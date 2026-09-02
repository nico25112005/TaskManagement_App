using System.Collections.Generic;

namespace TaskManagement
{
    public class Week
    {
        public float PlanedHours { get; set; }
        public List<Task> Tasks { get; set; }

        // Tracks the date this Week entry represents (added in split-tasks refactor).
        // Old code used just integer day-indices from "today", which broke across
        // midnight or when the user added tasks late at night.
        public System.DateTime Date { get; set; }

        public Week()
        {
            PlanedHours = 0;
            Tasks = new List<Task>();
        }

        public Week(System.DateTime date) : this()
        {
            Date = date;
        }
    }
}
