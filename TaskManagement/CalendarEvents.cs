using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TaskManagement
{
    /// <summary>
    /// Static container for CalendarEvents. Same pattern as Tasks: static state,
    /// persisted to JSON. Singleton-style for simplicity. In a real product this
    /// would be injected, but the current codebase uses static state everywhere,
    /// so we stay consistent.
    /// </summary>
    internal static class CalendarEvents
    {
        public static List<CalendarEvent> events = new();

        public static void WriteDataToJson()
        {
            string path = Path.Combine(
                Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""),
                "calendar_events.json"
            );
            string json = JsonConvert.SerializeObject(events, Formatting.Indented);
            if (!File.Exists(path)) File.Create(path).Close();
            using StreamWriter writer = new(path);
            writer.WriteLine(json);
        }

        public static void ReadDataFromJson()
        {
            string path = Path.Combine(
                Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""),
                "calendar_events.json"
            );
            if (!File.Exists(path)) { File.Create(path).Close(); return; }
            string json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) { events = new(); return; }
            events = JsonConvert.DeserializeObject<List<CalendarEvent>>(json) ?? new();
        }

        /// <summary>
        /// Returns all hard-block events for a given day (FixedAppointment, FreeTime, Sleep).
        /// These are the slots where the scheduler cannot place any tasks.
        /// </summary>
        public static List<CalendarEvent> GetHardBlocksForDay(DateTime day)
        {
            return events.Where(e =>
                (e.Type == CalendarEventType.FixedAppointment ||
                 e.Type == CalendarEventType.FreeTime ||
                 e.Type == CalendarEventType.Sleep) &&
                e.IsOnDay(day)
            ).ToList();
        }

        /// <summary>
        /// Returns the total work hours available on a given day:
        /// (WorkHours blocks) minus (HardBlocks that overlap with WorkHours).
        /// If no WorkHours are defined, returns 0 (so user must set them).
        /// </summary>
        public static float GetAvailableHoursForDay(DateTime day)
        {
            var workBlocks = events.Where(e => e.Type == CalendarEventType.WorkHours && e.IsOnDay(day)).ToList();
            if (workBlocks.Count == 0) return 0f;

            var hardBlocks = GetHardBlocksForDay(day);

            float totalMinutes = 0f;
            foreach (var work in workBlocks)
            {
                // Subtract overlapping hard-block minutes
                float workMinutes = (float)work.Duration.TotalMinutes;
                foreach (var hard in hardBlocks)
                {
                    var overlapStart = work.Start > hard.Start ? work.Start : hard.Start;
                    var overlapEnd = work.End < hard.End ? work.End : hard.End;
                    if (overlapStart < overlapEnd)
                    {
                        workMinutes -= (float)(overlapEnd - overlapStart).TotalMinutes;
                    }
                }
                totalMinutes += Math.Max(0, workMinutes);
            }
            return totalMinutes / 60f;
        }

        /// <summary>
        /// Returns the list of available time slots (start, end) on a given day
        /// after subtracting all hard blocks from the work blocks.
        /// Used by the SplitTasks algorithm to know WHERE in the day to place blocks.
        /// </summary>
        public static List<(DateTime Start, DateTime End)> GetAvailableSlotsForDay(DateTime day)
        {
            var workBlocks = events.Where(e => e.Type == CalendarEventType.WorkHours && e.IsOnDay(day)).ToList();
            var hardBlocks = GetHardBlocksForDay(day);

            var slots = new List<(DateTime, DateTime)>();
            foreach (var work in workBlocks)
            {
                var current = work.Start;
                var hardInThisBlock = hardBlocks
                    .Where(h => h.Start < work.End && h.End > work.Start)
                    .OrderBy(h => h.Start)
                    .ToList();

                foreach (var hard in hardInThisBlock)
                {
                    if (hard.Start > current)
                    {
                        slots.Add((current, hard.Start));
                    }
                    current = hard.End > current ? hard.End : current;
                }
                if (current < work.End)
                {
                    slots.Add((current, work.End));
                }
            }
            // Filter out slots shorter than 30 minutes – not worth scheduling
            return slots.Where(s => (s.End - s.Start).TotalMinutes >= 30).ToList();
        }
    }
}
