using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;

namespace UnrealEngineInstaller.Handlers
{
    class VisualStudioHandler
    {
        private const string VsWhereExeFilePath = @"C:\Program Files (x86)\Microsoft Visual Studio\Installer\vswhere.exe";
        private const string UrlInstaller2019 = "https://aka.ms/vs/16/release/vs_community.exe";
        private const string UrlInstaller2022 = "https://aka.ms/vs/17/release/vs_community.exe";

        private readonly static string[] VisualStudio2022WorkloadIds = new string[]
        {
            "Microsoft.VisualStudio.Workload.CoreEditor",//Core editor
            "Microsoft.VisualStudio.Workload.NetWeb", //ASP.NET and Web Development
            "Microsoft.VisualStudio.Workload.Universal", //Universal Windows Platform Development
            "Microsoft.VisualStudio.Workload.NativeDesktop", //Desktop Development with C++
            "Microsoft.VisualStudio.Workload.NativeGame", //Game Development with C++
            "Microsoft.VisualStudio.Workload.ManagedDesktop", //.NET Desktop Development 2022
            // Individual components
            "Microsoft.NetCore.Component.SDK",
            "Microsoft.NetCore.Component.DevelopmentTools",
            "Microsoft.NetCore.Component.Runtime.7.0",
            "Microsoft.VisualStudio.Component.VC.Tools.x86.x64",
            "Microsoft.VisualStudio.Component.VC.Tools.ARM64",
            "Microsoft.VisualStudio.Component.VC.14.34.17.4.ARM64",
            "Microsoft.VisualStudio.Component.VC.14.34.17.4.x86.x64",
            "Component.Unreal.Ide",
            "Component.Unreal",
            "Microsoft.VisualStudio.Component.Windows10SDK.20348",
            "Microsoft.VisualStudio.Component.Windows11SDK.22000",
            "Microsoft.VisualStudio.Component.Windows11SDK.22621",
            "Microsoft.Net.Component.4.6.2.TargetingPack"
        };

        private readonly static string[] VisualStudio2019WorkloadIds = new string[]
        {
                "Microsoft.VisualStudio.Workload.Universal", //Universal Windows Platform Development
                "Microsoft.VisualStudio.Workload.NativeDesktop", //Desktop Development with C++
                "Microsoft.VisualStudio.Workload.NativeGame", //Game Development with C++
                "Microsoft.VisualStudio.Workload.NetDesktop", //.NET Desktop Development 2019
                "Microsoft.NetCore.Component.SDK",
                "Microsoft.VisualStudio.Component.VC.Tools.x86.x64"
        };

        private VisualStudioSettings _settings;
        private string installedPath;
        private string VisualStudioUpdaterFileName;
        private VisualStudioVersion targetVersion;

        public VisualStudioHandler(VisualStudioSettings settings)
        {
            _settings = settings;

            if (settings.Version == "2019")
                targetVersion = VisualStudioVersion.v2019;
            else if (settings.Version == "2022")
                targetVersion = VisualStudioVersion.v2022;
            else
                targetVersion = VisualStudioVersion.NotDefined;
        }

        public bool CheckPrerequisites()
        {
            if (targetVersion == VisualStudioVersion.NotDefined)
            {
                Log.Information($"Not Defined Visual Studio Version {_settings.Version}. Defnined versions are: 2019, 2022");
                return false;
            }
            return true;
        }

        public bool CheckIsInstalled()
        {
            installedPath = string.Empty;

            if (CheckExistVsWhereExe() == false)
            {
                Log.Information($"Visual Studio is not installed.");
                return false;
            }

            installedPath = FindVisualStudioInstalledPath();

            if (string.IsNullOrEmpty(installedPath))
            {
                Log.Information($"Visual Studio {_settings.Version} is not installed.");
                return false;
            }
            else
            {
                Log.Information($"Visual Studio {_settings.Version} is already installed.");
                return true;
            }
        }

        public void InstallVisualStudio()
        {
            string urlInstaller = GetUrlInstaller();
            string installerFileName = "vs_community.exe";

            Log.Information($"Downloading Visual Studio {_settings.Version} installer...");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(urlInstaller, installerFileName);
            }
            Log.Information("Download completed.");

            Log.Information("Installing Visual Studio...");
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = installerFileName;

            string vsInstallerArgs = " --norestart --passive --includeRecommended";
            var workloadIDs = GetVisualStudioWorkloads();
            foreach (string workLoad in workloadIDs)
            {
                vsInstallerArgs += " --add " + workLoad;
            }
            vsInstallerArgs += " --wait";

            psi.Arguments = vsInstallerArgs;
            psi.UseShellExecute = true;

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                Log.Information("Installation completed.");
                File.Delete(installerFileName);
                Log.Information("Installer file deleted.");
            }
        }

        public void UpdateVisualStudio()
        {
            DownloadUpdater();

            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = VisualStudioUpdaterFileName;

            string vsInstallerArgs = " --update --norestart --passive  --includeRecommended";

            var workloadIds = GetVisualStudioWorkloads();
            foreach (string id in workloadIds)
                vsInstallerArgs += " --add " + id;
            vsInstallerArgs += " --wait";

            psi.Arguments = vsInstallerArgs;

            using (Process process = Process.Start(psi))
            {
                process.WaitForExit();
                Log.Information($"Visual Studio {_settings.Version} Updated!");

                File.Delete(VisualStudioUpdaterFileName);
                Log.Information($"Updater file deleted.");
            }

        }

        public string GetMsBuildPath()
        {
            string MsBuildPathSuffixPath = @"MSBuild\Current\Bin\MSBuild.exe";
            var MsBuildPath = Path.Combine(installedPath, MsBuildPathSuffixPath);
            Log.Information($"MsBuild Path: {MsBuildPath}");
            return MsBuildPath;
        }

        private string GetUrlInstaller()
        {
            switch (targetVersion)
            {
                case VisualStudioVersion.v2022:
                    return UrlInstaller2022;

                case VisualStudioVersion.v2019:
                    return UrlInstaller2019;

                default:
                    return UrlInstaller2022;
            }
        }

        private string[] GetVisualStudioWorkloads()
        {
            switch (targetVersion)
            {
                case VisualStudioVersion.v2022:
                    return VisualStudio2022WorkloadIds;

                case VisualStudioVersion.v2019:
                    return VisualStudio2019WorkloadIds;

                default:
                    return VisualStudio2022WorkloadIds;
            }
        }

        private bool CheckExistVsWhereExe()
        {
            if (File.Exists(VsWhereExeFilePath))
                return true;
            else
            {
                Log.Information($"vswhere.exe not found in {VsWhereExeFilePath}.");
                return false;
            }
        }

        private string FindVisualStudioInstalledPath()
        {
            string result = string.Empty;

            Process vswhere = new Process();
            vswhere.StartInfo.FileName = VsWhereExeFilePath;
            vswhere.StartInfo.Arguments = "-all -property installationPath";
            vswhere.StartInfo.UseShellExecute = false;
            vswhere.StartInfo.RedirectStandardOutput = true;
            vswhere.Start();

            while (!vswhere.StandardOutput.EndOfStream)
            {
                string line = vswhere.StandardOutput.ReadLine();
                var version = GetVisualStudioVersionByInstallPath(line);
                if (version == _settings.Version)
                {
                    Log.Information($"VisualStudio {_settings.Version} Path: {line}");
                    result = line;
                }
            }

            return result;
        }

        private void DownloadUpdater()
        {

            var splitPath = installedPath.Split('\\');
            var vsEdition = splitPath[splitPath.Length - 1].ToLower();

            var urlVersion = "16";
            if (targetVersion == VisualStudioVersion.v2019)
                urlVersion = "16";
            else if (targetVersion == VisualStudioVersion.v2022)
                urlVersion = "17";
            else
                throw new ArgumentException("Not Defined Visual Studio Version");

            string urlUpdater = $"https://aka.ms/vs/{urlVersion}/release/vs_{vsEdition}.exe";
            VisualStudioUpdaterFileName = $"vs_{vsEdition}.exe";


            Log.Information($"Downloading Visual Studio Updater (version:{_settings.Version} | Edition:{vsEdition}) ...");
            using (WebClient client = new WebClient())
            {
                client.DownloadFile(urlUpdater, VisualStudioUpdaterFileName);
            }
            Log.Information("Download completed.");
        }

        private string GetVisualStudioVersionByInstallPath(string installPath)
        {
            var splitPath = installPath.Split("\\");
            var vsVersion = splitPath[splitPath.Length - 2];

            return vsVersion;
        }

        public enum VisualStudioVersion
        {
            v2022,
            v2019,
            NotDefined
        }

    }
}
