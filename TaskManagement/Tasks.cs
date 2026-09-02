using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;

namespace TaskManagement
{
    internal class Tasks
    {
        public static Dictionary<string, Task> tasks = new();
        public static Dictionary<string, Task> notDistributableTasks = new();
        public static Dictionary<int, Week> nWeek = new();

        static Tasks()
        {
            tasks = new Dictionary<string, Task>();
            nWeek = new Dictionary<int, Week>();
        }

        public static void WriteDataToJson<T>(string filename, T data)
        {
            filename += ".json";
            string path = Path.Combine(Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""), filename);
            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            using StreamWriter writer = new(path);
            writer.WriteLine(json);
        }

        public static void ReadDataFromJson<T>(string filename, out T output)
        {
            filename += ".json";
            string path = Path.Combine(Environment.CurrentDirectory.Replace("bin\\Debug\\net6.0-windows", ""), filename);

            if (!File.Exists(path))
            {
                File.Create(path).Close();
            }

            string json = File.ReadAllText(path);
            output = JsonConvert.DeserializeObject<T>(json);
        }

        public static void GenerateTasks(byte amountOfTasks, sbyte lowerDaysTillDelivery, sbyte upperDaysTillDelivery, byte lowerHours, byte upperHours, bool hoursInMinutes)
        {
            Random rand = new();
            for (int i = 0; i < amountOfTasks; i++)
            {
                var taskId = Task.GenerateId();
                var task = new Task(
                    (float)rand.Next(lowerHours, upperHours),
                    $"Task {i + 1}",
                    DateTime.Now.AddDays(rand.Next(lowerDaysTillDelivery, upperDaysTillDelivery)),
                    (byte)rand.Next(1, 3),
                    hoursInMinutes);

                tasks[taskId] = task;
            }
        }
    }
}
