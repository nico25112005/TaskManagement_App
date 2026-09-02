namespace TaskManagement
{
    public static class Settings
    {
        public static float maxHoursPerDay = 3.5f;
        public static byte maxPlanableDays = 10;

        public static void Print()
        {
            System.Diagnostics.Trace.WriteLine($"Max Hours Per Day: {maxHoursPerDay}, Max Planable Days: {maxPlanableDays}");
        }
    }
}
