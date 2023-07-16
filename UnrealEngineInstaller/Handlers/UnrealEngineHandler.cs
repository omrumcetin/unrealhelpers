using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace UnrealEngineInstaller.Handlers
{
    class UnrealEngineHandler
    {
        private string _unrealEngineRootPath;

        private UnrealEngineSettings _settings;

        public UnrealEngineHandler(UnrealEngineSettings settings)
        {
            _settings = settings;
        }

        public bool CheckPrerequisites()
        {
            //TODO: check if commit hash exist
            return true;
        }

        public void CloneGitUnrealEngine(string gitToken, string clonePath)
        {
            _unrealEngineRootPath = clonePath;

            if (Directory.Exists(_unrealEngineRootPath) == false)
                Directory.CreateDirectory(_unrealEngineRootPath);

            string repoName = "UnrealEngine";
            string gitOwner = "EpicGames";
            string cloneUrl = $"https://{gitToken}@github.com/{gitOwner}/{repoName}.git";

            Log.Information($"Cloning Unreal Engine source...\n");

            Process process = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = $"clone {cloneUrl} {clonePath}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            Log.Information(output);

            // Change directory to the cloned repository
            Environment.CurrentDirectory = clonePath;

            // Pull all changes and commits
            Process.Start("git", "pull --all").WaitForExit();

            // Get everything
            Process.Start("git", "fetch").WaitForExit();

            Process processCheckout = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "git",
                    Arguments = $"checkout {_settings.CommitHash}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true
                }
            };
            processCheckout.Start();
            output = processCheckout.StandardOutput.ReadToEnd();
            processCheckout.WaitForExit();
            Log.Information(output);
        }
        public void CopyCommitGitDepths()
        {
            string sourcePath = $@"{AppDomain.CurrentDomain.BaseDirectory}\Commit.gitdeps.xml";
            string destinationPath = $@"{_unrealEngineRootPath}\Engine\Build\Commit.gitdeps.xml";
            File.Copy(sourcePath, destinationPath, true);
        }

        public void CopyCommitGitDepths(string SourcePath)
        {
            string sourcePath = $@"Commit.gitdeps.xml";
            //string destinationPath = $@"{_unrealEngineRootPath}\Engine\Build\Commit.gitdeps.xml";
            string destinationPath = $@"{SourcePath}\Engine\Build\Commit.gitdeps.xml";
            File.Copy(sourcePath, destinationPath, true);
        }

        public void RunSetupBat()
        {
            RunFileAndWait(_unrealEngineRootPath, "Setup.bat");
        }

        public void RunGenerateProjectFilesBat()
        {
            RunFileAndWait(_unrealEngineRootPath, "GenerateProjectFiles.bat");
        }

        public void BuildUnrealEngine(string msbuildPath, bool bFixSolver)
        {
            string solutionPath = FindUnrealSlnPath();
            string workingDirectory = Path.GetDirectoryName(solutionPath);

            if (bFixSolver)
                FixSolverIssue(workingDirectory);

            Process rebuildProcess = new Process()
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = msbuildPath,
                    Arguments = $@"{solutionPath} -t:Engine\UE5 /t:Build /p:Configuration=""Development Editor"";platform=Win64",
                    WorkingDirectory = workingDirectory
                }
            };
            rebuildProcess.Start();
            rebuildProcess.WaitForExit();

            if (rebuildProcess.ExitCode == 0)
                Log.Information("Unreal Engine built successfully!");
            else
                Log.Information("Unreal Engine build failed!");
        }

        private void FixSolverIssue(string BasePath)
        {
            string filePath = Path.Combine(BasePath, @"Engine\Plugins\Experimental\ChaosUserDataPT\Source\ChaosUserDataPT\Public\ChaosUserDataPT.h");
            string text = File.ReadAllText(filePath);
            text = text.Replace("FPhysicsSolverBase* Solver", "FPhysicsSolverBase* TheSolver");
            File.WriteAllText(filePath, text);
        }


        private static void RunFileAndWait(string directory, string fileName)
        {
            string extention = Path.GetExtension(fileName);
            string[] files = Directory.GetFiles(directory, $"*{extention}", SearchOption.AllDirectories);

            foreach (string file in files)
            {
                if (Path.GetFileName(file) == fileName)
                {
                    Log.Information($"Running {fileName}...");

                    Process process = Process.Start(file);
                    process.WaitForExit();
                    Log.Information($"{fileName} process is finished.");

                    break;
                }
            }
        }

        private string FindUnrealSlnPath()
        {
            string[] files;
            files = Directory.GetFiles(_unrealEngineRootPath, "*.sln", SearchOption.TopDirectoryOnly);

            if (files == null || files.Length == 0)
            {
                string[] subDirs = Directory.GetDirectories(_unrealEngineRootPath, "*", SearchOption.AllDirectories);

                foreach (string subdir in subDirs)
                {
                    files = Directory.GetFiles(subdir, "*.sln", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).Contains("UE"))
                        {
                            Log.Information($"Unreal sln File Path: {Path.GetFullPath(file)}");
                            return Path.GetFullPath(file);
                        }
                    }
                }
            }
            else
            {
                foreach (string file in files)
                {
                    if (Path.GetFileName(file).Contains("UE"))
                    {
                        Log.Information($"Unreal sln File Path: {Path.GetFullPath(file)}");
                        return Path.GetFullPath(file);
                    }
                }
            }

            throw new DirectoryNotFoundException("EU<version>.sln file not found!");
        }

        private string FindUnrealRootPath()
        {
            string[] files;
            files = Directory.GetFiles(_unrealEngineRootPath, "*.sln", SearchOption.TopDirectoryOnly);

            if (files == null || files.Length == 0)
            {
                string[] subDirs = Directory.GetDirectories(_unrealEngineRootPath, "*", SearchOption.AllDirectories);

                foreach (string subdir in subDirs)
                {
                    files = Directory.GetFiles(subdir, "*.sln", SearchOption.TopDirectoryOnly);
                    foreach (string file in files)
                    {
                        if (Path.GetFileName(file).Contains("UE"))
                            return subdir;
                    }
                }
            }
            else
            {
                foreach (string file in files)
                    if (Path.GetFileName(file).Contains("UE"))
                        return _unrealEngineRootPath;
            }

            throw new DirectoryNotFoundException("EU<version>.sln file not found!");
        }

    }
}
