using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildHelper.Helpers
{
    internal class Modifier
    {
        private string EnginePath;
        private string ProjectPath;
        public Modifier(string EnginePath, string ProjectPath)
        {
            this.EnginePath = EnginePath;
            this.ProjectPath = ProjectPath;
        }

        public void ApplySettingsToEngineConfig(int MaxMemoryAllowance, int MaxConcurentShaderJobs, int MinReservedMemory)
        {
            if (EnginePath == null)
                return;

            var configPath = Path.Combine(EnginePath, "Config/BaseEditor.ini");
            string tempFile = Path.Combine(Path.GetDirectoryName(configPath), "BaseEditor-temp.ini");

            using (var sourceConfig = File.OpenText(configPath))
            {

                using (var tempFileStream = new StreamWriter(tempFile))
                {
                    string line;
                    bool bConfigSegmentFound = false;

                    while ((line = sourceConfig.ReadLine()) != null)
                    {
                        if (bConfigSegmentFound)
                        {
                            if (line.Contains("MaxMemoryAllowance"))
                            {
                                line = $"MaxMemoryAllowance={MaxMemoryAllowance}";
                            }
                            else if (line.Contains("MaxConcurrentShaderJobs"))
                            {
                                line = $"MaxConcurrentShaderJobs={MaxConcurentShaderJobs}";
                            }
                            else if (line.Contains("MinReservedMemory"))
                            {
                                line = $"MinReservedMemory={MinReservedMemory}";
                            }
                        }

                        if (!bConfigSegmentFound && line.Contains("[CookSettings]"))
                        {
                            bConfigSegmentFound = true;
                        }
                        tempFileStream.WriteLine(line);
                    }

                    // if config never found
                }
            }

            File.Replace(tempFile, configPath, null);
        }

        public void ApplySettingsToProjectConfig(int MaxMemoryAllowance, int MinMemoryBeforeGC, int MinReservedMemory)
        {
            if (ProjectPath == null)
                return;

            var configPath = Path.Combine(ProjectPath, "Config/DefaultEditor.ini");
            string tempFile = Path.Combine(Path.GetDirectoryName(configPath), "DefaultEditor-temp.ini");

            using (var sourceConfig = File.OpenText(configPath))
            {

                using (var tempFileStream = new StreamWriter(tempFile))
                {
                    string line;
                    bool bConfigSegmentFound = false;

                    while ((line = sourceConfig.ReadLine()) != null)
                    {
                        if (bConfigSegmentFound)
                        {
                            if (line.Contains("MaxMemoryAllowance"))
                            {
                                line = $"MaxMemoryAllowance={MaxMemoryAllowance}";
                            }
                            else if (line.Contains("MinMemoryBeforeGC"))
                            {
                                line = $"MinMemoryBeforeGC={MinMemoryBeforeGC}";
                            }
                            else if (line.Contains("MinReservedMemory"))
                            {
                                line = $"MinReservedMemory={MinReservedMemory}";
                            }
                        }

                        if (!bConfigSegmentFound && line.Contains("[CookSettings]"))
                        {
                            bConfigSegmentFound = true;
                        }
                        tempFileStream.WriteLine(line);
                    }
                }
            }

            File.Replace(tempFile, configPath, null);
        }

        public void ApplyShaderConfig(int CoreCount, int RamAmount)
        {
            if (EnginePath == null)
                return;

            var configPath = Path.Combine(EnginePath, "Config/BaseEngine.ini");
            string tempFile = Path.Combine(Path.GetDirectoryName(configPath), "BaseEngine-temp.ini");

            using (var sourceConfig = File.OpenText(configPath))
            {

                using (var tempFileStream = new StreamWriter(tempFile))
                {
                    string line;
                    bool bConfigSegmentFound = false;

                    while ((line = sourceConfig.ReadLine()) != null)
                    {
                        if (bConfigSegmentFound)
                        {
                            if (line.Contains("NumUnusedShaderCompilingThreads="))
                            {
                                var candidateCompilingThreads = (RamAmount / 2) + 1 ;
                                if (candidateCompilingThreads > CoreCount)
                                    candidateCompilingThreads = CoreCount - 2;
                                line = $"NumUnusedShaderCompilingThreads={candidateCompilingThreads}";
                            }
                        }

                        if (!bConfigSegmentFound && line.Contains("[DevOptions.Shaders]"))
                        {
                            bConfigSegmentFound = true;
                        }
                        tempFileStream.WriteLine(line);
                    }
                }
            }

            File.Replace(tempFile, configPath, null);
        }
    }
}
