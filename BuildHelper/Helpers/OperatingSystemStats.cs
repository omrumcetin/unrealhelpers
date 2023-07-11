using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;

namespace BuildHelper.Helpers
{
    internal static class OperatingSystemStats
    {
        private static ManagementObject RAMStats;
        private static ManagementObject CPUStats;
        public static void Refresh()
        {
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();

            //long availableTotalMemoryBytes = 0;
            //long totalCore = 0;

            foreach (ManagementObject result in results)
            {
                RAMStats = result;
#if DEBUG
                using (StreamWriter sw = new StreamWriter("OperatingSystem.txt"))
                {
                    foreach (var res in result.Properties)
                    {
                        sw.WriteLine($"{res.Name}: {res.Value}");
                    }
                }
#endif
            }

            wql = new ObjectQuery("SELECT * FROM Win32_processor");
            searcher = new ManagementObjectSearcher(wql);
            results = searcher.Get();

            foreach (ManagementObject result in results)
            {
                CPUStats = result;
#if DEBUG
                using (StreamWriter sw = new StreamWriter("ProcessorStats.txt"))
                {
                    foreach (var res in result.Properties)
                    {
                        sw.WriteLine($"{res.Name}: {res.Value}");
                    }
                }
#endif
            }
        }

        public static long GetAvailableFreeRAM()
        {
            Refresh();
            return Convert.ToInt64(RAMStats.Properties["FreePhysicalMemory"].Value);
        }

        public static int GetAvailableTotalPhysicalRAM()
        {
            Refresh();
            return Convert.ToInt32(RAMStats.Properties["TotalVisibleMemorySize"].Value);
        }
        public static int GetAvailableTotalVRAM()
        {
            Refresh();
            return Convert.ToInt32(RAMStats.Properties["TotalVirtualMemorySize"].Value);
        }

        public static int GetAvailableCores()
        {
            Refresh();
            return Convert.ToInt32(CPUStats.Properties["NumberOfLogicalProcessors"].Value);
        }
    }
}
