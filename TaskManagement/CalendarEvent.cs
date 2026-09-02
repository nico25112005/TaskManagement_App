using System;

namespace TaskManagement
{
    /// <summary>
    /// Represents a fixed time slot that cannot be moved by the scheduler.
    /// Used for: appointments, classes, recurring work blocks, sleep, free time.
    /// </summary>
    public class CalendarEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N").Substring(0, 8);
        public string Title { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        /// <summary>
        /// What kind of slot this is. The scheduler treats these differently:
        /// - FixedAppointment: hard block, no tasks can be placed here
        /// - WorkHours: scheduler can place tasks here
        /// - FreeTime: hard block, no tasks (sport, family time)
        /// - Sleep: like FixedAppointment but typically whole-day
        /// </summary>
        public CalendarEventType Type { get; set; } = CalendarEventType.FixedAppointment;

        /// <summary>
        /// Recurring pattern. null = single event. Otherwise the event repeats.
        /// Format: "WEEKLY:Mon,Wed,Fri" or "DAILY" or "WEEKDAYS" or "WEEKENDS"
        /// </summary>
        public string? Recurrence { get; set; }

        public TimeSpan Duration => End - Start;

        public bool ConflictsWith(CalendarEvent other)
        {
            // Two events conflict if their time ranges overlap
            return Start < other.End && other.Start < End;
        }

        public bool IsOnDay(DateTime day)
        {
            return Start.Date <= day.Date && day.Date <= End.Date;
        }
    }

    public enum CalendarEventType
    {
        FixedAppointment = 0,  // cannot be moved (Uni, Arzt)
        WorkHours = 1,          // available for tasks
        FreeTime = 2,           // hard block (Sport, Familie)
        Sleep = 3               // hard block, typically whole-day
    }
}
