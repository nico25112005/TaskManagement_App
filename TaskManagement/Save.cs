using System.Collections.Generic;

namespace TaskManagement
{
    public class Save
    {
        private Dictionary<string, Task> task { get; set; }
        private Dictionary<int, Week> nWeek { get; set; }
        private byte maxPlanableDays { get; set; }
        private float maxHoursPerDay { get; set; }

        public void applyValues()
        {
            Tasks.tasks = this.task;
            Tasks.nWeek = this.nWeek;
            Settings.maxPlanableDays = this.maxPlanableDays;
            Settings.maxHoursPerDay = this.maxHoursPerDay;
        }

        public void setValues()
        {
            this.task = Tasks.tasks;
            this.nWeek = Tasks.nWeek;
            this.maxPlanableDays = Settings.maxPlanableDays;
            this.maxHoursPerDay = Settings.maxHoursPerDay;
        }
    }
}
