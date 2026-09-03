using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace TaskManagement
{
    public static class TaskSorter
    {
        /// <summary>
        /// Distributes all tasks across the planning horizon (Settings.maxPlanableDays),
        /// respecting CalendarEvents (fixed appointments, free time, work hours).
        /// Tasks larger than a single day's free slot are SPLIT across multiple days.
        /// </summary>
        public static void Distributor()
        {
            Trace.WriteLine("Starte Aufgabenverteilung (split-aware, calendar-aware)...");

            var sortedTasks = Tasks.tasks.Values
                .OrderByDescending(task => task.CalculateWeighting(Tasks.tasks))
                .ToList();

            // Build a per-day list of remaining free minutes from CalendarEvents.
            // Index = day-offset from today. Value = remaining minutes in that day.
            var freeMinutesPerDay = BuildDayAvailabilityMap();

            Tasks.nWeek.Clear();
            Tasks.notDistributableTasks.Clear();

            foreach (var task in sortedTasks)
            {
                Trace.WriteLine($"Verarbeite Aufgabe: {task.Description}, Stunden: {task.Hours}, Gewichtung: {task.CalculateWeighting(Tasks.tasks):F2}");

                if (!TryAssignWithSplit(task, freeMinutesPerDay))
                {
                    Trace.WriteLine($"Warnung: Aufgabe \"{task.Description}\" konnte nicht verteilt werden (freie Kapazität überschritten).");
                    if (!Tasks.notDistributableTasks.ContainsKey(task.Description))
                        Tasks.notDistributableTasks.Add(task.Description, task);
                }
            }

            Trace.WriteLine("Aufgabenverteilung abgeschlossen.");
            PrintWeekPlan();
        }

        /// <summary>
        /// Returns a map: day-offset -> free minutes available on that day.
        /// Combines CalendarEvents (work hours minus fixed blocks / free time / sleep)
        /// with Settings.maxHoursPerDay as a hard cap (so even if user defines
        /// 16h of work hours, the scheduler won't overload them).
        /// </summary>
        private static Dictionary<int, int> BuildDayAvailabilityMap()
        {
            var map = new Dictionary<int, int>();
            int hardCapMinutes = (int)(Settings.maxHoursPerDay * 60f);

            for (int day = 0; day < Settings.maxPlanableDays; day++)
            {
                var date = DateTime.Today.AddDays(day);
                float calendarHours = CalendarEvents.GetAvailableHoursForDay(date);
                float cappedHours = Math.Min(calendarHours, Settings.maxHoursPerDay);
                map[day] = (int)(cappedHours * 60f);
            }
            return map;
        }

        /// <summary>
        /// Splits a task across multiple days if needed. Returns true if fully assigned.
        /// Algorithm: first-fit decreasing with split. Walks day by day, taking the
        /// min(task-remaining, day-remaining) per day, recording each chunk as its
        /// own Task with the same description + a "[part N/M]" suffix for clarity.
        /// </summary>
        private static bool TryAssignWithSplit(Task task, Dictionary<int, int> freeMinutesPerDay)
        {
            int remainingMinutes = (int)(task.Hours * 60f);
            if (remainingMinutes <= 0) return true; // 0h task is trivially "placed"

            int chunksPlanned = 0;
            int totalChunksEstimate = EstimateChunkCount(task.Hours, freeMinutesPerDay);
            int chunkIndex = 1;

            foreach (var dayKvp in freeMinutesPerDay.OrderBy(k => k.Key).ToList())
            {
                if (remainingMinutes <= 0) break;
                if (dayKvp.Value <= 0) continue;

                int minutesToPlace = Math.Min(remainingMinutes, dayKvp.Value);
                float hoursToPlace = minutesToPlace / 60f;

                // Subtract from the day's available pool
                freeMinutesPerDay[dayKvp.Key] = dayKvp.Value - minutesToPlace;

                // Register the chunk in the Week for that day
                var date = DateTime.Today.AddDays(dayKvp.Key);
                if (!Tasks.nWeek.ContainsKey(dayKvp.Key))
                    Tasks.nWeek[dayKvp.Key] = new Week(date);

                var chunkTask = new Task
                {
                    Description = totalChunksEstimate > 1
                        ? $"{task.Description} [part {chunkIndex}/{totalChunksEstimate}]"
                        : task.Description,
                    Delivery = task.Delivery,
                    Importance = task.Importance,
                    Hours = hoursToPlace
                };
                Tasks.nWeek[dayKvp.Key].Tasks.Add(chunkTask);
                Tasks.nWeek[dayKvp.Key].PlanedHours += hoursToPlace;

                Trace.WriteLine($"  + Tag {dayKvp.Key} ({date:dd.MM.}): {hoursToPlace:F2}h von \"{task.Description}\"");
                remainingMinutes -= minutesToPlace;
                chunksPlanned++;
                chunkIndex++;
            }

            return remainingMinutes <= 0;
        }

        /// <summary>
        /// Quick upper bound on how many chunks a task will need.
        /// Used for human-readable labels like "[part 2/4]".
        /// </summary>
        private static int EstimateChunkCount(float taskHours, Dictionary<int, int> freeMinutesPerDay)
        {
            int totalFree = freeMinutesPerDay.Values.Where(m => m > 0).Sum();
            int needed = (int)(taskHours * 60f);
            if (totalFree <= 0 || needed <= 0) return 1;
            int totalCap = freeMinutesPerDay.Count(m => m.Value > 0) * 60 * (int)Settings.maxHoursPerDay;
            // Use the day's max capacity for a pessimistic estimate
            return Math.Max(1, (int)Math.Ceiling(needed / (double)Math.Max(1, freeMinutesPerDay.Values.DefaultIfEmpty(1).Max())));
        }

        public static void PrintWeekPlan()
        {
            StringBuilder weekPlan = new();
            weekPlan.AppendLine("=== Wochenplan ===");

            foreach (var day in Tasks.nWeek.OrderBy(week => week.Key))
            {
                weekPlan.AppendLine($"Tag {day.Key} ({day.Value.Date:dd.MM.yyyy}): Geplante Stunden: {day.Value.PlanedHours:F2}");
                foreach (var task in day.Value.Tasks)
                {
                    weekPlan.AppendLine($"   - {task.Description} ({task.Hours:F2} Stunden)");
                }
            }

            if (Tasks.notDistributableTasks.Count > 0)
            {
                weekPlan.AppendLine();
                weekPlan.AppendLine($"!!! {Tasks.notDistributableTasks.Count} Tasks konnten NICHT verteilt werden:");
                foreach (var t in Tasks.notDistributableTasks.Values)
                {
                    weekPlan.AppendLine($"   - {t.Description} ({t.Hours:F2}h, Deadline {t.Delivery:dd.MM.})");
                }
            }

            Trace.WriteLine(weekPlan.ToString());
        }
    }

    public class PriorityQueue<TKey, TValue> where TValue : IComparable<TValue>
    {
        private readonly SortedDictionary<TValue, Queue<TKey>> _dict = new();
        public int Count { get; private set; } = 0;

        public void Enqueue(TKey key, TValue value)
        {
            if (!_dict.ContainsKey(value))
            {
                _dict[value] = new Queue<TKey>();
            }
            _dict[value].Enqueue(key);
            Count++;
        }

        public TKey Dequeue(out TValue priority)
        {
            if (_dict.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            var (minPriority, queue) = _dict.First();
            priority = minPriority;
            var item = queue.Dequeue();
            if (queue.Count == 0)
            {
                _dict.Remove(minPriority);
            }

            Count--;
            return item;
        }
    }

    public static class TaskExtensions
    {
        public static Task CloneWithHours(this Task original, float hours)
        {
            return new Task
            {
                Description = original.Description,
                Delivery = original.Delivery,
                Importance = original.Importance,
                Hours = hours
            };
        }
    }
}
