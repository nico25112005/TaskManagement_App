using System.Collections.Generic;

namespace TaskManagement
{
    public class Week
    {
        public float PlanedHours { get; set; }
        public List<Task> Tasks { get; set; }

        public Week()
        {
            PlanedHours = 0;
            Tasks = new List<Task>();
        }
    }
}
