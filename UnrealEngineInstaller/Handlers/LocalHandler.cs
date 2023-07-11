using Microsoft.Win32;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnrealEngineInstaller.Core;

namespace UnrealEngineInstaller.Handlers
{
    static class LocalHandler
    {
        public static Software IsApplictionInstalled(string p_name)
        {
            string displayName;
            RegistryKey key;

            // search in: CurrentUser
            key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName").ToString();
                if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return PopulateSoftware(subkey);
                }
            }

            // search in: LocalMachine_32
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;
                if (string.IsNullOrEmpty(displayName))
                {
                    continue;
                }
                if (p_name.StartsWith(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return PopulateSoftware(subkey);
                }
            }

            // search in: LocalMachine_64
            key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall");
            foreach (String keyName in key.GetSubKeyNames())
            {
                RegistryKey subkey = key.OpenSubKey(keyName);
                displayName = subkey.GetValue("DisplayName") as string;
                if (p_name.Equals(displayName, StringComparison.OrdinalIgnoreCase) == true)
                {
                    return PopulateSoftware(subkey);
                }
            }
            return null;
        }

        private static Software PopulateSoftware(RegistryKey subkey)
        {

            Software software = new Software();

            software.DisplayName = subkey.GetValue("DisplayName").ToString();
            software.InstallLocation = subkey.GetValue("InstallLocation").ToString();
            software.Version = subkey.GetValue("DisplayVersion").ToString();

            return software;
        }

        public static bool CheckOtherPrerequisites()
        {
            if (IsApplictionInstalled("Git") == null)
            {
                Log.Error("You need to install Git in order to proceed.");
                return false;
            }
            return true;
        }
    }
}
