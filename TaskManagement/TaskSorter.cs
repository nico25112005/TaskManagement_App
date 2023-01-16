using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TaskManagement
{
    public static class Data
    {

        public static Dictionary<string, Task> tasks = new();

        public static Dictionary<string, Task> notDistributableTasks = new();

        //public static float[,] week = new float[Settings.maxPlannableDays, Settings.tasksPerDay];

        public static Dictionary<int, Week> nWeek = new();

        static Data()
        {
            tasks = new Dictionary<string, Task>();
            nWeek = new Dictionary<int, Week>();
        }

        public static void WriteDataToJson<T>(string filename, T Data)
        {
            filename += ".json";
            string path = Path.Combine(Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""), filename);

            string json = JsonConvert.SerializeObject(Data, Formatting.Indented);

            if (File.Exists(path) != true)
            {
                File.Create(path).Close();
            }

            using StreamWriter writer = new(path);
            writer.WriteLine(json);
        }
        public static void ReadDataOfJson<T>(string filename, out T output)
        {
            filename += ".json";
            string path = Path.Combine(Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""), filename);
            string json;

            if (File.Exists(path) != true)
            {
                File.Create(path).Close();
            }

            using (StreamReader reader = new(path))
            {
                json = reader.ReadToEnd();
            }

            output = JsonConvert.DeserializeObject<T>(json);
        }

        public static void GenerateTasks(byte amountOfTasks, sbyte lowerDaysTillDelivery, sbyte upperDaysTillDelivery, byte lowerHours, byte upperHours, bool hoursInMinutes)
        {
            Random rand = new();
            for (int i = 0; i < amountOfTasks; i++)
            {
                tasks.Add(Task.GenerateID(), new Task((float)rand.Next(lowerHours, upperHours), i.ToString(), DateTime.Now.AddDays(rand.Next(lowerDaysTillDelivery, upperDaysTillDelivery)), (byte)rand.Next(1, 3), hoursInMinutes));
            }
        }

    }
    public static class Settings
    {
        public static float maxHoursPerDay = 3;
        public static byte maxPlanableDays = 10;

        public static void Print()
        {
            Console.WriteLine("MaxHoursPerDay: {0}, Max Plannable Days: {1}", maxHoursPerDay, maxPlanableDays);
        }
    }


    public class Task
    {

        //public int id { get;}
        private float _allocationWeighting;
        public float AllocationWeighting
        {
            get
            {
                _allocationWeighting = (float)(600 / (0.9f + Math.Pow(Importance, 1.087312731f) / 10f + Math.Exp(0.69f * GetDaysTillDelivery()) * (600f / 576 - 1)) + Hours * 35);
                return _allocationWeighting;
            }
            set
            {
                _allocationWeighting = value;
            }
        }

        private sbyte DaysTillDelivery;
        public float Hours { get; set; } // ungefäre Arbeitszeit um fertig zu werden 
        public string Description { get; set; } // Kurze beschreibung was zu tun ist
        public DateTime Delivery { get; set; } // Abgabedatum 
        public byte Importance { get; set; } // 1-3 -> 1: sehr wichtig, 2: mittelmässig, 3: nicht so wichtig

        //Verbessesrungswürdig, nur zum konvertieren der .json file wegen dem Datum
        public Task()
        {

        }
        public Task(float _time, string _description, DateTime _delivery, byte _importance, bool _hoursInMinutes)
        {
            if (_time > 0)
            {
                if (_hoursInMinutes == true)
                {
                    this.Hours = _time / 60f;
                }
                else
                {
                    this.Hours = _time;
                }
            }
            else
            {
                Console.WriteLine("Hours can't be a negative value");
            }

            if (_importance >= 1 && _importance <= 3)
            {
                this.Importance = _importance;
            }
            else
            {
                Console.WriteLine(Importance);
                Console.WriteLine("Imp. out of range");
            }

            Description = _description;
            Delivery = _delivery;
            DaysTillDelivery = (sbyte)((Delivery - DateTime.Now).Days + 1);
            AllocationWeighting = (float)(600 / (0.9f + Math.Pow(Importance, 1.087312731f) / 10f + Math.Exp(0.69f * GetDaysTillDelivery()) * (600f / 576 - 1)) + Hours * 35);

        }

        public static List<int> usedIds = new();
        public static string GenerateID()
        {
            Random random = new();
            int newId;

            do
            {
                newId = random.Next(ushort.MaxValue);

            } while (usedIds.Contains(newId) == true);
            usedIds.Add(newId);

            return newId.ToString();
        }

        public sbyte GetDaysTillDelivery()
        {
            DaysTillDelivery = (sbyte)((Delivery - DateTime.Now).Days + 1);
            return DaysTillDelivery;
        }

        public void Print(string _id)
        {
            Console.WriteLine("Id: {4}, Hours: {0}, description: {1}, delivery: {2}, importancy: {3}, allocationWeighting: {5}, Days Till Delivery: {6}\n", Hours, Description, Delivery.ToShortDateString(), Importance, _id, AllocationWeighting, GetDaysTillDelivery());
        }
    }

    public class Week
    {
        public float PlanedHours { get; set; }
        //public string? Id { get; set; }
        public List<Task> Tasks { get; set; }

        public Week()
        {
            PlanedHours = 0;
            Tasks = new List<Task>();
        }
    }
    public static class TaskSorter
    {
        public static void Distributor()
        {

            var tasks = from task in Data.tasks
                        orderby task.Value.AllocationWeighting descending
                        select task.Key;

            bool distributed;

            //zuteilung von den importancy sorted Tasks in den Week Array
            foreach (var task in tasks)
            {
                Console.WriteLine("test, {0}", task);
                distributed = false;

                for (byte i = 0; i < Settings.maxPlanableDays && distributed == false && Data.tasks[task].GetDaysTillDelivery() >= i; i++) // week Tage
                {
                    if (Data.tasks[task].Importance == 1)// überprüfung ob importancy 1 ist
                    {
                        if (Data.nWeek.ContainsKey(i) == false)
                        {
                            Data.nWeek.Add(i, new Week());
                        }

                        if (Data.tasks[task].Hours + Data.nWeek[i].PlanedHours <= Settings.maxHoursPerDay)// überprüfung ob die Stunden sich an diesem Tag noch ausgehen
                        {

                            Data.nWeek[i].Tasks.Add(Data.tasks[task]);
                            Data.nWeek[i].PlanedHours += Data.tasks[task].Hours;

                            Console.WriteLine("1 distributed");
                            distributed = true;
                        }
                    }
                    else if (Data.tasks[task].Importance == 2)// überprüfung ob importancy 2 ist
                    {
                        if (Data.nWeek.ContainsKey(i) == false)
                        {
                            Data.nWeek.Add(i, new Week());
                        }

                        if (Data.tasks[task].Hours + Data.nWeek[i].PlanedHours <= Settings.maxHoursPerDay * 0.77 - 0.1)// überprüfung ob die Sunden sich an diesem Tag noch ausgehen
                        {

                            Data.nWeek[i].Tasks.Add(Data.tasks[task]);
                            Data.nWeek[i].PlanedHours += Data.tasks[task].Hours;

                            Console.WriteLine("2 distributed");
                            distributed = true;
                        }
                    }
                    else if (Data.tasks[task].Importance == 3)// überprüfung ob importancy 3 ist
                    {
                        if (Data.nWeek.ContainsKey(i) == false)
                        {
                            Data.nWeek.Add(i, new Week());
                        }

                        if (Data.tasks[task].Hours + Data.nWeek[i].PlanedHours <= Settings.maxHoursPerDay * 0.64 - 0.1)// überprüfung ob die Sunden sich an diesem Tag noch ausgehen
                        {

                            Data.nWeek[i].Tasks.Add(Data.tasks[task]);
                            Data.nWeek[i].PlanedHours += Data.tasks[task].Hours;

                            Console.WriteLine("3 distributed");
                            distributed = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("importancy out of range!");
                    }

                    if (Data.tasks[task].GetDaysTillDelivery() == i && distributed != true)
                    {
                        Console.WriteLine("day: {1}, id: {0} ist nicht rechtzeitig möglich fertigzustellen.", task, i);
                        if (Data.notDistributableTasks.ContainsKey(task) == false) Data.notDistributableTasks.Add(task, Data.tasks[task]);
                    }
                }
            }
        }

        public static void SplitingTasks()
        {

        }

        public static float AvarageHoursPerDay()
        {
            float averageHorsPerDay = 0;
            float leftHoursInThisWeek = 0;
            var hours = from hour in Data.tasks
                        where hour.Value.GetDaysTillDelivery() <= 5 - (int)DateTime.Now.DayOfWeek
                        select hour.Value.Hours;

            foreach (var hour in hours)
            {
                leftHoursInThisWeek += hour;
            }

            averageHorsPerDay = leftHoursInThisWeek / (5 - (int)DateTime.Now.DayOfWeek);
            Console.WriteLine("Dayofweek: {0}, ghours: {1}", (5 - (int)DateTime.Now.DayOfWeek), leftHoursInThisWeek);
            Console.ReadKey();
            return averageHorsPerDay;
        }
    }
}
