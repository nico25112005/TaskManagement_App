using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace TaskManagement
{
    /// <summary>
    /// View-model for the Home tab's To-do panel. Splits tasks into two buckets:
    /// "Today" (planned today OR deadline today) and "Upcoming" (everything else).
    /// Designed for INotifyPropertyChanged so the UI refreshes when Calculate() runs.
    /// </summary>
    public class HomeTodoViewModel : INotifyPropertyChanged
    {
        private List<TaskViewModel> _today = new();
        private List<TaskViewModel> _upcoming = new();
        private List<DoneTaskViewModel> _done = new();
        private float _todayPlannedHours;
        private float _todayAvailableHours;

        public List<TaskViewModel> Today
        {
            get => _today;
            set { _today = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasToday)); OnPropertyChanged(nameof(TodayCount)); }
        }

        public List<TaskViewModel> Upcoming
        {
            get => _upcoming;
            set { _upcoming = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasUpcoming)); OnPropertyChanged(nameof(UpcomingCount)); }
        }

        public List<DoneTaskViewModel> Done
        {
            get => _done;
            set { _done = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasDone)); OnPropertyChanged(nameof(DoneCount)); }
        }

        public bool HasDone => _done.Count > 0;
        public int DoneCount => _done.Count;

        public float TodayPlannedHours
        {
            get => _todayPlannedHours;
            set { _todayPlannedHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(TodayLoadLabel)); }
        }

        public float TodayAvailableHours
        {
            get => _todayAvailableHours;
            set { _todayAvailableHours = value; OnPropertyChanged(); OnPropertyChanged(nameof(TodayLoadLabel)); OnPropertyChanged(nameof(IsOverloaded)); }
        }

        public bool HasToday => _today.Count > 0;
        public bool HasUpcoming => _upcoming.Count > 0;
        public int TodayCount => _today.Count;
        public int UpcomingCount => _upcoming.Count;

        /// <summary>
        /// "5.5h / 3.5h" overload indicator string. Shows red when planned > available.
        /// </summary>
        public string TodayLoadLabel
        {
            get
            {
                var planned = _todayPlannedHours;
                var available = _todayAvailableHours;
                if (available <= 0) return $"{planned:F1}h / no slot";
                return $"{planned:F1}h / {available:F1}h";
            }
        }

        public bool IsOverloaded => _todayAvailableHours > 0 && _todayPlannedHours > _todayAvailableHours;

        public void Refresh()
        {
            var today = DateTime.Today;
            var todayScheduled = Tasks.nWeek.Values
                .Where(w => w.Date.Date == today)
                .SelectMany(w => w.Tasks)
                .ToList();

            // "Today" = planned for today, OR deadline today, OR overdue (deadline in past).
            // We union with the original Task objects by Description so the user sees
            // one entry per logical task, not per split chunk.
            var descriptionsToday = todayScheduled.Select(t => StripPartLabel(t.Description)).ToHashSet();

            Today = todayScheduled
                .Concat(Tasks.tasks.Values.Where(t =>
                    t.Delivery.Date == today ||
                    (t.Delivery.Date < today && t.Hours > 0)))
                .DistinctBy(t => StripPartLabel(t.Description))
                .OrderByDescending(t => t.Importance)
                .Select(t => new TaskViewModel { Description = t.Description, Hours = t.Hours })
                .ToList();

            TodayPlannedHours = todayScheduled.Sum(t => t.Hours);
            TodayAvailableHours = CalendarEvents.GetAvailableHoursForDay(today);

            // "Upcoming" = everything not in Today, ordered by deadline.
            var todayDescriptions = new HashSet<string>(Today.Select(t => t.Description));
            Upcoming = Tasks.tasks.Values
                .Where(t => !todayDescriptions.Contains(StripPartLabel(t.Description)))
                .OrderBy(t => t.Delivery)
                .Select(t => new TaskViewModel { Description = t.Description, Hours = t.Hours })
                .ToList();

            // "Recently done" = the most recently completed tasks. Capped at 10
            // to avoid flooding the Home tab if the user has a long history.
            Done = Tasks.done?.Values
                .Where(t => t.DoneAt.HasValue)
                .OrderByDescending(t => t.DoneAt)
                .Take(10)
                .Select(t => new DoneTaskViewModel
                {
                    Id = t.DoneAt.HasValue ? (Tasks.done?.FirstOrDefault(kv => kv.Value == t).Key ?? "") : "",
                    Description = t.Description,
                    DoneAtLabel = t.DoneAt.HasValue ? t.DoneAt.Value.ToString("dd.MM. HH:mm") : ""
                })
                .ToList() ?? new();
        }

        /// <summary>
        /// Strips the "[part 2/4]" suffix that TaskSorter adds to split chunks,
        /// so each logical task shows up once in the Home view.
        /// </summary>
        private static string StripPartLabel(string description)
        {
            var idx = description.IndexOf(" [part ", StringComparison.Ordinal);
            return idx >= 0 ? description.Substring(0, idx) : description;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    /// <summary>
    /// View-model for a completed task shown in the Home tab's "Recently done" list.
    /// Carries the task id so the Undo button can move it back to active.
    /// </summary>
    public class DoneTaskViewModel
    {
        public string Id { get; set; } = "";
        public string Description { get; set; } = "";
        public string DoneAtLabel { get; set; } = "";
    }
}
