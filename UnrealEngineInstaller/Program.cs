using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnrealEngineInstaller.Handlers;

namespace UnrealEngineInstaller
{
    class Program
    {
        private static VisualStudioHandler visualStudioHandler;
        private static UnrealEngineHandler unrealEngineHandler;
        private static PlasticScmHandler plasticScmHandler;

        static void Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                .CreateLogger();

            Signature();
            Log.Warning("You need to install git and plastic scm first in order to proceed.");
            Log.Warning("Download Git : https://git-scm.com/download/win");
            Log.Warning("Download Plastic SCM : https://www.plasticscm.com/download");
            Log.Warning("Please login to the Plastic Server before proceeding...");
            Log.Warning("If everything ready, press any key to continue...");
            Console.ReadKey();

            bool bForceEnabled = false;
            bool bPlasticEnabled = true;
            bool bFixDeps = false;
            bool bFixSolver = false;
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (arg == "-force") bForceEnabled = true;
                    if (arg == "-noplastic") bPlasticEnabled = false;
                    if (arg == "-fixdeps") bFixDeps = true;
                    if (arg == "-fixsolver") bFixSolver = true;
                }
            }

            try
            {
                Initialize();

                CheckPrerequisites(bForceEnabled, bPlasticEnabled);

                //Visual Studio
                bool hasVisualStudio = visualStudioHandler.CheckIsInstalled();
                if (hasVisualStudio)
                    visualStudioHandler.UpdateVisualStudio();
                else
                    visualStudioHandler.InstallVisualStudio();

                //Unreal engine
                var gitToken = GetUserInput("Please Enter your Git PAT Token:");
                string unrealEngineRootPath = GetUserInput(@"Please Enter a path to download unreal engine: (Default: C:\Program Files\UnrealEngine)");
                if (string.IsNullOrEmpty(unrealEngineRootPath))
                {
                    unrealEngineRootPath = @"C:\Program Files\UnrealEngine";
                }

                unrealEngineHandler.CloneGitUnrealEngine(gitToken, unrealEngineRootPath);
                if (bFixDeps) unrealEngineHandler.CopyCommitGitDepths();
                unrealEngineHandler.RunSetupBat();
                unrealEngineHandler.RunGenerateProjectFilesBat();
                var msBuildPath = visualStudioHandler.GetMsBuildPath();
                unrealEngineHandler.BuildUnrealEngine(msBuildPath, bFixSolver);

                //Plastic SCM
                if (bPlasticEnabled)
                {
                    var repoPath = GetUserInput(@"Please Enter a path to create Plastic SCM workspace: (Default: C:\Repos)");
                    if (string.IsNullOrEmpty(repoPath))
                    {
                        repoPath = @"C:\Repos";
                    }
                    var plasticWorkspaceName = GetUserInput(@"Please Enter the workspace name. (Default : repo name will be used.)");
                    plasticScmHandler.CreateWorkspace(repoPath, plasticWorkspaceName);
                    plasticScmHandler.SwitchBranchAndUpdateWorkspace();
                    plasticScmHandler.SwitchUnrealVersionInWorkspace(unrealEngineRootPath);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Exception: {ex.Message}");
            }
        }

        static void Initialize()
        {
            Log.Information("Initializing...");
            var appSettings = BuildAppSettings();

            visualStudioHandler = new VisualStudioHandler(appSettings.VisualStudioSettings);
            unrealEngineHandler = new UnrealEngineHandler(appSettings.UnrealEngineSettings);
            plasticScmHandler = new PlasticScmHandler(appSettings.PlasticScmSettings);
        }

        static AppSettings BuildAppSettings()
        {
            Log.Information("Building AppSettings...");
            var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appSettings.json")
                    .Build();

            var appSettings = new AppSettings();
            configuration.GetSection("AppSettings").Bind(appSettings);

            string reportMessage = "\n********* AppSettings *********\n";
            reportMessage += $"VisualStudio Version: {appSettings.VisualStudioSettings.Version}\n";
            reportMessage += $"Unreal CommitHash: {appSettings.UnrealEngineSettings.CommitHash}\n";
            reportMessage += $"Plastic Scm Connected Server: {appSettings.PlasticScmSettings.ConnectionString}\n";
            reportMessage += $"Plastic Scm Repo Name: {appSettings.PlasticScmSettings.RepoName}\n";
            reportMessage += $"Plastic Scm Branch Name: {appSettings.PlasticScmSettings.BranchName}\n";
            reportMessage += "**********************************";
            Log.Information(reportMessage);

            return appSettings;
        }

        static void CheckPrerequisites(bool IsForceEnabled, bool IsPlasticEnabled)
        {
            if (IsForceEnabled)
            {
                Log.Information("Force enabled on prerequisities check! Next steps are born to throw errors! Please be sure you completed the prerequisities...");
            }
            bool isValidVisualStudio = visualStudioHandler.CheckPrerequisites();
            bool isValidUnrealEngine = unrealEngineHandler.CheckPrerequisites();

            bool isValidPlasticScm = true;
            if (IsPlasticEnabled)
            {
                isValidPlasticScm = plasticScmHandler.CheckPrerequisites();
            }
            bool isValidOthers = LocalHandler.CheckOtherPrerequisites();

            bool result = (isValidVisualStudio && isValidUnrealEngine && isValidPlasticScm && isValidOthers) || IsForceEnabled;
            if (result)
                Log.Information("Prerequisites validation succeeded!");
            else
            {
                Log.Error("Prerequisites validation failed!");
                Environment.Exit(0);
            }
        }

        private static string GetUserInput(string message)
        {
            Log.Information($"\n***** {message} *****");
            string input = Console.ReadLine();
            Log.Information($"You entered: {input}\n");

            return input;
        }

        private static void Signature()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("\n\n");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⠀⠀⠀⣀⣤⡶⠋⠀⠀⠀⠀⠀⠀⠀⠀⠀⠀⣀⠀⠀         ");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⢀⣠⣾⣿⣿⠁⠀⠀⠀⠀⠀⠀⠀⣀⣴⣾⡟⠁⠀⠀         ");
            Console.WriteLine("           ⠀⠀⠀⠀⣰⣿⣿⣿⣿⣿⣦⡄⠀⠀⠈⠳⣶⣾⣿⣿⡏⠀⠀⠀⠀         ");
            Console.WriteLine("           ⠀⠀⢀⣾⣿⡿⠿⣿⣿⣿⣿⡇⠀⠀⠀⠀⣿⣿⣿⣿⡇⠀⠀⠀⠀         ");
            Console.WriteLine("           ⠀⢠⣿⡿⠋⠀⠀⢸⣿⣿⣿⡇⠀⠀⠀⠀⣿⣿⣿⣿⡇⠀⠀⠀⠀         ");
            Console.WriteLine("           ⠀⣿⠟⠀⠀⠀⠀⢸⣿⣿⣿⡇⠀⠀⠀⠀⣿⣿⣿⣿⡇⠀⠀⠀⠀         ");
            Console.WriteLine("           ⢰⠋⠀⠀⠀⠀⠀⢸⣿⣿⣿⡇⠀⠀⠀⠀⣿⣿⣿⣿⡇⠀⠀⠀⠀         ");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⠀⠀⢸⣿⣿⣿⡇⠀⠀⠀⠀⣿⣿⣿⣿⡇⠀⢀⣠⠔         ");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⠀⠀⣸⣿⣿⣿⣷⣤⣤⣴⣾⣿⣿⣿⣿⣿⣿⡿⠋⠀         ");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⠀⠙⢿⣿⣿⣿⣿⣿⣿⣿⡟⠻⣿⣿⣿⡿⠟⠁⠀⠀         ");
            Console.WriteLine("           ⠀⠀⠀⠀⠀⠀⠀⠀⠉⠛⠿⣿⣿⣿⠋⠀⠀⠈⠻⠋⠀⠀⠀⠀⠀         \n");
            Console.WriteLine("              UNREAL ENGINE DEPLOYER         ");
            Console.WriteLine("       Magic Media & Entertainment Group Ltd.");
            Console.WriteLine("               All rights reserved           \n\n");
        }

    }
}

