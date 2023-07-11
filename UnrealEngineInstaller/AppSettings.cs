namespace UnrealEngineInstaller
{
    class AppSettings
    {    
        public VisualStudioSettings VisualStudioSettings { get; set; }
        public UnrealEngineSettings UnrealEngineSettings { get; set; }
        public PlasticScmSettings PlasticScmSettings { get; set; }
    }

    class VisualStudioSettings
    {
        public string Version { get; set; }
    }

    class UnrealEngineSettings
    {
        public string CommitHash { get; set; }
    }

    class PlasticScmSettings
    {
        public string RepoName { get; set; }
        public string BranchName { get; set; }
        public string ConnectionString { get; set; }
    }

}
