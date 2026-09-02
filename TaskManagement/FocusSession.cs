using System;
using System.Diagnostics;

namespace TaskManagement
{
    /// <summary>
    /// A focus session: working on a task for a planned duration, with adaptive
    /// breaks in between. Designed to be UI-agnostic – the WPF layer just binds
    /// to the public properties and reads IsRunning / IsPaused for button states.
    ///
    /// Adaptive break logic (v1):
    ///   - After every focus block: short break (5 min by default)
    ///   - After every 3rd consecutive focus block: long break (15 min)
    ///   - User can override the next break duration via SkipBreak()
    /// </summary>
    public class FocusSession
    {
        public Task? Task { get; private set; }
        public TimeSpan FocusDuration { get; private set; }
        public TimeSpan ShortBreak { get; set; } = TimeSpan.FromMinutes(5);
        public TimeSpan LongBreak { get; set; } = TimeSpan.FromMinutes(15);
        public int LongBreakInterval { get; set; } = 3;

        public DateTime StartedAt { get; private set; }
        public TimeSpan Elapsed { get; private set; }
        public bool IsRunning { get; private set; }
        public bool IsOnBreak { get; private set; }
        public int CompletedFocusBlocks { get; private set; }
        public TimeSpan CurrentBlockRemaining { get; private set; }

        private readonly Stopwatch _stopwatch = new();

        public event EventHandler? Tick;
        public event EventHandler? FocusBlockCompleted;
        public event EventHandler? BreakCompleted;

        public void Start(Task task, TimeSpan focusDuration)
        {
            Task = task;
            FocusDuration = focusDuration;
            CurrentBlockRemaining = focusDuration;
            StartedAt = DateTime.Now;
            Elapsed = TimeSpan.Zero;
            CompletedFocusBlocks = 0;
            IsOnBreak = false;
            _stopwatch.Restart();
            IsRunning = true;

            // 1-second tick for UI updates
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (s, e) => OnTick();
            timer.Start();
        }

        public void Pause()
        {
            if (!IsRunning) return;
            _stopwatch.Stop();
            IsRunning = false;
        }

        public void Resume()
        {
            if (IsRunning) return;
            _stopwatch.Start();
            IsRunning = true;
        }

        public void Stop()
        {
            _stopwatch.Reset();
            IsRunning = false;
            IsOnBreak = false;
            Task = null;
        }

        public void SkipBreak()
        {
            if (!IsOnBreak) return;
            IsOnBreak = false;
            CurrentBlockRemaining = FocusDuration;
        }

        private void OnTick()
        {
            Elapsed = _stopwatch.Elapsed;

            if (!IsOnBreak)
            {
                CurrentBlockRemaining = FocusDuration - Elapsed;
                if (CurrentBlockRemaining <= TimeSpan.Zero)
                {
                    CompleteFocusBlock();
                }
            }
            else
            {
                var breakDuration = (CompletedFocusBlocks % LongBreakInterval == 0)
                    ? LongBreak
                    : ShortBreak;
                CurrentBlockRemaining = breakDuration - (Elapsed - FocusDuration);
                if (CurrentBlockRemaining <= TimeSpan.Zero)
                {
                    CompleteBreak();
                }
            }

            Tick?.Invoke(this, EventArgs.Empty);
        }

        private void CompleteFocusBlock()
        {
            // Book the time back to the task
            if (Task != null)
            {
                float hoursWorked = (float)FocusDuration.TotalHours;
                Task.Hours = Math.Max(0, Task.Hours - hoursWorked);
                Trace.WriteLine($"Focus-Block beendet: {hoursWorked:F2}h auf '{Task.Description}' gebucht. Verbleibend: {Task.Hours:F2}h");
            }

            CompletedFocusBlocks++;
            IsOnBreak = true;
            FocusBlockCompleted?.Invoke(this, EventArgs.Empty);
        }

        private void CompleteBreak()
        {
            IsOnBreak = false;
            _stopwatch.Restart(); // reset for next focus block
            BreakCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
