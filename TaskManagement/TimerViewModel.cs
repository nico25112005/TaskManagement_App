using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TaskManagement
{
    /// <summary>
    /// UI-facing wrapper around FocusSession with INotifyPropertyChanged
    /// for the WPF bindings on the Home tab.
    /// </summary>
    public class TimerViewModel : INotifyPropertyChanged
    {
        private readonly FocusSession _session = new();
        private string _display = "00:00";
        private string _stateLabel = "Ready";
        private bool _isRunning;

        public TimerViewModel()
        {
            _session.Tick += (s, e) => Refresh();
        }

        public string Display
        {
            get => _display;
            set { _display = value; OnPropertyChanged(); }
        }

        public string StateLabel
        {
            get => _stateLabel;
            set { _stateLabel = value; OnPropertyChanged(); }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set { _isRunning = value; OnPropertyChanged(); }
        }

        public Task? ActiveTask => _session.Task;
        public FocusSession Session => _session;

        public void Start(Task task, TimeSpan duration)
        {
            _session.Start(task, duration);
            IsRunning = true;
            StateLabel = $"Focus: {task.Description}";
            Refresh();
        }

        public void Pause()
        {
            _session.Pause();
            IsRunning = false;
            StateLabel = "Paused";
        }

        public void Resume()
        {
            _session.Resume();
            IsRunning = true;
        }

        public void Stop()
        {
            _session.Stop();
            IsRunning = false;
            Display = "00:00";
            StateLabel = "Ready";
        }

        public void SkipBreak() => _session.SkipBreak();

        private void Refresh()
        {
            var remaining = _session.CurrentBlockRemaining;
            if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
            Display = remaining.ToString(@"mm\:ss");
            if (_session.IsOnBreak)
            {
                StateLabel = (_session.CompletedFocusBlocks % _session.LongBreakInterval == 0)
                    ? "Long break"
                    : "Short break";
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
