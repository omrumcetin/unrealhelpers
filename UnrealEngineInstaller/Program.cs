﻿using System;
using System.Globalization;
using System.IO;
using Microsoft.Extensions.Configuration;
using Serilog;
using UnrealEngineInstaller.Handlers;

namespace UnrealEngineInstaller
{
    enum something
    {
        High,
        Low,
        Great
    };
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

            var bForceEnabled = false;
            var bPlasticEnabled = true;
            var bFixDeps = false;
            var bFixSolver = false;
            var bBuildEngineOnly = false;
            var bUpgradeEngine = false;
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    if (string.Equals(arg, "-force", StringComparison.InvariantCultureIgnoreCase)) bForceEnabled = true;
                    if (string.Equals(arg, "-noplastic", StringComparison.InvariantCultureIgnoreCase)) bPlasticEnabled = false;
                    if (string.Equals(arg, "-fixdeps", StringComparison.InvariantCultureIgnoreCase)) bFixDeps = true;
                    if (string.Equals(arg, "-fixsolver", StringComparison.InvariantCultureIgnoreCase)) bFixSolver = true;
                    if (string.Equals(arg, "-buildengineonly", StringComparison.InvariantCultureIgnoreCase)) bBuildEngineOnly = true;
                    if (string.Equals(arg, "-upgrade", StringComparison.InvariantCultureIgnoreCase)) bUpgradeEngine = true;
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

                var gitToken = "";

                if (bBuildEngineOnly)
                {
                    string unrealEnginePath = GetUserInput(@"Please Enter a path to build the unreal engine: (Default: C:\Program Files\UnrealEngine)");
                    if (bUpgradeEngine)
                    {
                        gitToken = GetUserInput("Please Enter your Git PAT Token to fetch updates:");
                        unrealEngineHandler.CloneGitUnrealEngine(gitToken, unrealEnginePath);
                    }
                    unrealEngineHandler.SetUnrealRootPath(unrealEnginePath);
                    unrealEngineHandler.RunSetupBat();
                    unrealEngineHandler.RunGenerateProjectFilesBat();
                    var msBuildPathForFix = visualStudioHandler.GetMsBuildPath();
                    unrealEngineHandler.BuildUnrealEngine(msBuildPathForFix, bFixSolver);
                    return;
                }

                //Unreal engine
                gitToken = GetUserInput("Please Enter your Git PAT Token:");
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
                    Log.Information("*********READ HERE CAREFULLY*******");
                    Log.Information(@"!Path is base path of your plastic repos! (Ex : C:\Repos)");
                    Log.Information(@"!!Workspace name is the workspace name can be different from repo name. (Ex : WkspaceOfMita)");
                    Log.Information("!!!Repo name is something that you clone to your space, it should be available repo in the system. (Ex : MITA)");
                    Log.Information("********BASE ON ABOVE OUTPUT YOUR PATH WILL LOOK LIKE THIS************");
                    Log.Information(@"C:\Repos\WkspaceOfMita    <-------- MITA Repo will be in it!");
                    Log.Information("**************************************");
                    Log.Warning("If you read carefully, press any key to continue...");
                    Console.ReadKey();
                    
                    var repoPath = GetUserInput(@"Please Enter a path to create Plastic SCM workspace: (Default: C:\Repos)");
                    if (string.IsNullOrEmpty(repoPath))
                    {
                        repoPath = @"C:\Repos";
                    }

                    bool validRepo = false;
                    string plasticRepoName = null;
                    while (!validRepo)
                    {
                        plasticRepoName = GetUserInput(@"Enter the repo name");
                        validRepo = plasticScmHandler.CheckRepo(plasticRepoName);
                    }
                    var plasticWorkspaceName = GetUserInput(@"Please Enter the workspace name. (Default : repo name will be used.)");
                    plasticScmHandler.CreateWorkspace(repoPath, plasticWorkspaceName, plasticRepoName);
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
            Console.WriteLine("         2023 Omrum Cetin all rights reserved.            ");
            Console.WriteLine("                                         \n\n");
        }

    }
}

