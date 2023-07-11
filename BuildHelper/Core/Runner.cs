using BuildHelper.Helpers;
using System;
using System.Threading.Tasks;

namespace BuildHelper.Core
{
    internal static class Runner
    {
        public static void ObserveSystem(int DurationInMinutes, out long OptimizedRamValue, IProgress<int> progress)
        {
            OptimizedRamValue = Int64.MaxValue;

            int counter = 0;
            int percentage = 0;

            var startingTimestamp = DateTime.Now;
            var endTimestamp = startingTimestamp.AddMinutes(DurationInMinutes);
            var nextRunTimestamp = startingTimestamp.AddSeconds(1);

            while (true)
            {
                if (endTimestamp < DateTime.Now)
                    break;

                if (nextRunTimestamp < DateTime.Now)
                {
                    var availableFreeMemory = OperatingSystemStats.GetAvailableFreeRAM();

                    if (availableFreeMemory <= OptimizedRamValue)
                    {
                        OptimizedRamValue = availableFreeMemory;
                    }
                    percentage = Convert.ToInt32(((float)counter++ / ((float)DurationInMinutes * 60)) * 100);
                    progress.Report(percentage);

                    nextRunTimestamp = nextRunTimestamp.AddSeconds(1);
                }
            }
        }
    }
}
