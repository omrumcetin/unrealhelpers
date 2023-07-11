using Serilog;
using System.Diagnostics;
using System.IO;
using UnrealEngineInstaller.Core;

namespace UnrealEngineInstaller.Handlers
{
    class PlasticScmHandler
    {
        private const string CmPath = @"client\cm.exe";
        private string FullCmPath;
        //private const string PlasticScmWorkSpacePath = @"D:\UnrealWorkSpace\Oikos";
        private string _workspacePath;

        private PlasticScmSettings _settings;

        public PlasticScmHandler(PlasticScmSettings settings)
        {
            _settings = settings;

        }

        public bool CheckPrerequisites()
        {
            Log.Information($"Cheking Plastic SCM validation...");
            Software plasticSCM = LocalHandler.IsApplictionInstalled("Codice Software Plastic SCM");

            if (plasticSCM != null)
                Log.Information($"Plastic SCM installed in system version {plasticSCM.Version}");
            else
            {
                Log.Error($"Counld not find PlasticSCM, please install it first.");
                return false;
            }

            FullCmPath = Path.Combine(plasticSCM.InstallLocation, CmPath);

            Process process = new Process();
            process.StartInfo.FileName = FullCmPath;
            process.StartInfo.Arguments = "repository";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            //Output when not logged in: "The server spec is not correct: invalid."
            if (output.Contains("invalid."))
            {
                Log.Error($"Plastic SCM is not logged in. Please login manually as in instructions that gave by developers.");
                return false;
            }
            else
                Log.Information($"Plastic SCM is logged in.");

            bool hasRepo = false;
            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                //Log.Information($"line: {line}");
                if (line.Contains($"{_settings.RepoName}@{_settings.ConnectionString}"))
                {
                    hasRepo = true;
                    break;
                }
            }
            if (hasRepo)
                Log.Information($"Repo '{_settings.RepoName}' is found in the repository list.");
            else
            {
                Log.Error($"Repo '{_settings.RepoName}' is not found in the repository list.");
                return false;
            }

            Log.Information($"Plastic SCM is valid.");
            return true;
        }

        public void CreateWorkspace(string repoPath, string workspaceName)
        {
            if (string.IsNullOrEmpty(workspaceName))
            {
                workspaceName = _settings.RepoName;
            }
            _workspacePath = Path.Combine(repoPath, workspaceName);
            string fullRepoName = $"{_settings.RepoName}@{_settings.ConnectionString}";

            Process process = new Process();
            process.StartInfo.FileName = FullCmPath;
            process.StartInfo.Arguments =
                $"workspace create {workspaceName} {repoPath} --repository={fullRepoName}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Log.Information(output);
        }

        public void SwitchBranchAndUpdateWorkspace()
        {
            Process process = new Process();
            process.StartInfo.FileName = "cmd.exe";

            string argumant = $"/C D: && cd {_workspacePath} && cm status && cm switch {_settings.BranchName} && cm update";
            process.StartInfo.Arguments = argumant;

            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Log.Information(output);
        }

        public void SwitchUnrealVersionInWorkspace(string unrealRootPath)
        {
            Log.Information($"Switching unreal version in project...");

            string versionSelectorSuffixPath = @"Engine\Binaries\Win64\UnrealVersionSelector-Win64-Shipping.exe";
            string versionSelectorPath = Path.Combine(unrealRootPath, versionSelectorSuffixPath);

            string uprojectPath = FindUprojectFilePath();

            Process process = new Process();
            process.StartInfo.FileName = versionSelectorPath;
            process.StartInfo.Arguments = $"-switchversionsilent {uprojectPath} {unrealRootPath}";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            Log.Information(output);
        }

        private string FindUprojectFilePath()
        {
            var files = Directory.GetFiles(_workspacePath, "*.uproject", SearchOption.TopDirectoryOnly);
            return Path.GetFullPath(files[0]);
        }

        private void GetListPlasticScmWorkspaces()
        {
            // Create a new process to run the Plastic SCM CLI command
            Process process = new Process();
            process.StartInfo.FileName = FullCmPath;
            process.StartInfo.Arguments = $"workspace list";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;

            // Start the process and read the output
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            // Print the output to the console
            Log.Information(output);
        }
    }
}
