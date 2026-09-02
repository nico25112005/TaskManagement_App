using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Text;

namespace TaskManagement
{
    public static class TaskSorter
    {
        public static void Distributor()
        {
            Trace.WriteLine("Starte Aufgabenverteilung...");

            var sortedTasks = Tasks.tasks.Values
                .OrderByDescending(task => task.CalculateWeighting(Tasks.tasks))
                .ToList();

            PriorityQueue<int, float> availableHoursPerDay = InitializeAvailableHours();

            foreach (var task in sortedTasks)
            {
                Trace.WriteLine($"Verarbeite Aufgabe: {task.Description}, Stunden: {task.Hours}, Gewichtung: {task.CalculateWeighting(Tasks.tasks):F2}");

                if (!AssignTaskEvenly(task, availableHoursPerDay))
                {
                    Trace.WriteLine($"Warnung: Aufgabe \"{task.Description}\" konnte nicht vollständig zugewiesen werden.");
                    if (!Tasks.notDistributableTasks.ContainsKey(task.Description))
                        Tasks.notDistributableTasks.Add(task.Description, task);
                }
            }

            Trace.WriteLine("Aufgabenverteilung abgeschlossen.");
            PrintWeekPlan();
        }

        private static PriorityQueue<int, float> InitializeAvailableHours()
        {
            var priorityQueue = new PriorityQueue<int, float>();

            for (int day = 0; day < Settings.maxPlanableDays; day++)
            {
                priorityQueue.Enqueue(day, Settings.maxHoursPerDay);
            }

            return priorityQueue;
        }

        private static bool AssignTaskEvenly(Task task, PriorityQueue<int, float> availableHoursPerDay)
        {
            float remainingHours = task.Hours;
            bool assigned = false;

            while (remainingHours > 0 && availableHoursPerDay.Count > 0)
            {
                int day = availableHoursPerDay.Dequeue(out float availableHours);

                if (availableHours > 0)
                {
                    float hoursToAssign = Math.Min(remainingHours, availableHours);
                    Task partialTask = task.CloneWithHours(hoursToAssign);

                    if (!Tasks.nWeek.ContainsKey(day))
                        Tasks.nWeek[day] = new Week();

                    Tasks.nWeek[day].Tasks.Add(partialTask);
                    Tasks.nWeek[day].PlanedHours += hoursToAssign;

                    Trace.WriteLine($"Aufgabe \"{task.Description}\" wurde {hoursToAssign:F2} Stunden zu Tag {day} zugewiesen.");

                    remainingHours -= hoursToAssign;
                    assigned = true;

                    float newAvailableHours = availableHours - hoursToAssign;
                    if (newAvailableHours > 0)
                        availableHoursPerDay.Enqueue(day, newAvailableHours);
                }
            }

            return remainingHours == 0;
        }

        public static void PrintWeekPlan()
        {
            StringBuilder weekPlan = new();
            weekPlan.AppendLine("=== Wochenplan ===");

            foreach (var day in Tasks.nWeek.OrderBy(week => week.Key))
            {
                weekPlan.AppendLine($"Tag {day.Key}: Geplante Stunden: {day.Value.PlanedHours:F2}");
                foreach (var task in day.Value.Tasks)
                {
                    weekPlan.AppendLine($"   - {task.Description} ({task.Hours:F2} Stunden)");
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
