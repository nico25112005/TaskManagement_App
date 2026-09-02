using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskManagement
{
    public class Task
    {
        private float _allocationWeighting;
        private static readonly HashSet<int> usedIds = new();

        public float AllocationWeighting
        {
            get => CalculateWeighting(Tasks.tasks);
        }

        public float Hours { get; set; }
        public string Description { get; set; }
        public DateTime Delivery { get; set; }
        public byte Importance { get; set; }
        public List<string> DependentTasks { get; set; } = new();

        private const float BaseWeight = 1000f;
        private const float TimeDecayFactor = 0.05f;

        public Task() { }

        public Task(float time, string description, DateTime delivery, byte importance, bool hoursInMinutes)
        {
            Hours = hoursInMinutes ? time / 60f : time;
            if (importance < 1 || importance > 3)
                throw new ArgumentOutOfRangeException(nameof(importance), "Importance must be between 1 and 3.");

            Description = description;
            Delivery = delivery;
            Importance = importance;
        }

        public float CalculateWeighting(Dictionary<string, Task> tasks)
        {
            int daysTillDelivery = Math.Max((Delivery - DateTime.Now).Days, 0);
            float importanceFactor = 300 / (float)Importance;
            float hoursFactor = 10 * Hours;

            float dependencyFactor = CalculateDependencyFactor(tasks);
            float remainingSlotFactor = CalculateRemainingSlotFactor();

            return BaseWeight / (1 + daysTillDelivery * TimeDecayFactor) + importanceFactor + hoursFactor + dependencyFactor + remainingSlotFactor;
        }

        private float CalculateDependencyFactor(Dictionary<string, Task> tasks)
        {
            float dependencyWeight = 0;

            foreach (var taskId in DependentTasks)
            {
                if (tasks.TryGetValue(taskId, out Task dependentTask))
                {
                    int dependentDays = Math.Max((dependentTask.Delivery - DateTime.Now).Days, 0);
                    dependencyWeight += 50 / (1 + dependentDays); // Höhere Gewichtung für baldige Abhängigkeiten
                }
            }

            return dependencyWeight;
        }

        private float CalculateRemainingSlotFactor()
        {
            // Beispiel: Restliche Stundenverfügbarkeit beeinflusst Gewichtung
            float remainingHours = Settings.maxHoursPerDay - (Tasks.nWeek.Values.Sum(week => week.PlanedHours));
            return remainingHours > 0 ? 50 / remainingHours : 100; // Je knapper die Zeit, desto höher die Gewichtung
        }

        public static string GenerateId()
        {
            Random random = new();
            int newId;

            do
            {
                newId = random.Next(ushort.MaxValue);
            } while (!usedIds.Add(newId));

            return newId.ToString();
        }

        public sbyte GetDaysTillDelivery()
        {
            return (sbyte)((Delivery - DateTime.Now).Days + 1);
        }
    }
}
