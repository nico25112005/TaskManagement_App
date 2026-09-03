using System;
using System.IO;
using Newtonsoft.Json;

namespace TaskManagement
{
    /// <summary>
    /// User-configurable settings. Persisted to settings.json next to the exe.
    /// Static fields for compatibility with the existing codebase, but mutations
    /// should go through the Save() method to persist.
    /// </summary>
    public static class Settings
    {
        public static float maxHoursPerDay = 3.5f;
        public static byte maxPlanableDays = 10;

        // Work-day boundaries for the planner. Used by TaskSorter to know when
        // tasks can actually be placed (outside these hours, only fixed/free/sleep).
        public static int workStartHour = 9;
        public static int workEndHour = 18;

        // UI preferences
        public static bool darkMode = false;

        // Timer defaults
        public static int focusBlockMinutes = 50;
        public static int shortBreakMinutes = 5;
        public static int longBreakMinutes = 15;
        public static int blocksBeforeLongBreak = 3;

        private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

        public static void Print()
        {
            System.Diagnostics.Trace.WriteLine(
                $"Max Hours/Day: {maxHoursPerDay}, Max Planable Days: {maxPlanableDays}, " +
                $"Work: {workStartHour:D2}:00–{workEndHour:D2}:00, " +
                $"Focus: {focusBlockMinutes}/{shortBreakMinutes}/{longBreakMinutes}min, " +
                $"Dark: {darkMode}");
        }

        public static SettingsData ToData() => new()
        {
            maxHoursPerDay = maxHoursPerDay,
            maxPlanableDays = maxPlanableDays,
            workStartHour = workStartHour,
            workEndHour = workEndHour,
            darkMode = darkMode,
            focusBlockMinutes = focusBlockMinutes,
            shortBreakMinutes = shortBreakMinutes,
            longBreakMinutes = longBreakMinutes,
            blocksBeforeLongBreak = blocksBeforeLongBreak
        };

        public static void LoadFromData(SettingsData data)
        {
            maxHoursPerDay = data.maxHoursPerDay;
            maxPlanableDays = data.maxPlanableDays;
            workStartHour = data.workStartHour;
            workEndHour = data.workEndHour;
            darkMode = data.darkMode;
            focusBlockMinutes = data.focusBlockMinutes;
            shortBreakMinutes = data.shortBreakMinutes;
            longBreakMinutes = data.longBreakMinutes;
            blocksBeforeLongBreak = data.blocksBeforeLongBreak;
        }

        public static void Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(ToData(), Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Settings.Save failed: {ex.Message}");
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(SettingsPath)) { Save(); return; }
                var json = File.ReadAllText(SettingsPath);
                var data = JsonConvert.DeserializeObject<SettingsData>(json);
                if (data != null) LoadFromData(data);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Settings.Load failed: {ex.Message}");
            }
        }
    }

    public class SettingsData
    {
        public float maxHoursPerDay { get; set; } = 3.5f;
        public byte maxPlanableDays { get; set; } = 10;
        public int workStartHour { get; set; } = 9;
        public int workEndHour { get; set; } = 18;
        public bool darkMode { get; set; } = false;
        public int focusBlockMinutes { get; set; } = 50;
        public int shortBreakMinutes { get; set; } = 5;
        public int longBreakMinutes { get; set; } = 15;
        public int blocksBeforeLongBreak { get; set; } = 3;
    }
}
