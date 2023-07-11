using BuildHelper.Core;
using BuildHelper.Helpers;
using System;
using System.IO;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;

namespace BuildHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            OperatingSystemStats.Refresh();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TextBlockTotalCPUCores.Text = OperatingSystemStats.GetAvailableCores().ToString();
            TextBlockTotalRAM.Text = $"{(OperatingSystemStats.GetAvailableTotalPhysicalRAM() / 1024).ToString()} MB";
            TextBlockTotalVRAM.Text = $"{(OperatingSystemStats.GetAvailableTotalVRAM() / 1024).ToString()} MB";
        }

        private void ButtonEnginePathBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog openFolderDialog = new FolderBrowserDialog())
            {
                DialogResult result = openFolderDialog.ShowDialog();

                if (!string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                {
                    TextBoxEnginePath.Text = openFolderDialog.SelectedPath;
                }
            }
        }

        private void ButtonProjectPathBrowse_Click(object sender, RoutedEventArgs e)
        {
            using (FolderBrowserDialog openFolderDialog = new FolderBrowserDialog())
            {
                DialogResult result = openFolderDialog.ShowDialog();

                if (!string.IsNullOrWhiteSpace(openFolderDialog.SelectedPath))
                {
                    TextBoxProjectPath.Text = openFolderDialog.SelectedPath;
                }
            }
        }

        private async void ButtonGetRecommended_Click(object sender, RoutedEventArgs e)
        {
            RunnerProgressBar.Value = 0;

            var progress = new Progress<int>(percent =>
            {
                RunnerProgressBar.Value = percent;
            });

            long minAllocatedRAM = 0;
            int observationMinute = 1;

            RunnerProgressBar.Visibility = Visibility.Visible;
            await Task.Run(() => Runner.ObserveSystem(observationMinute, out minAllocatedRAM, progress));
            RunnerProgressBar.Visibility = Visibility.Hidden;

            PopulateValuesToUI(minAllocatedRAM);
        }

        private void PopulateValuesToUI(long MinAllocatedRAM)
        {
            var availableFreeMemoryMbtes = MinAllocatedRAM / 1024 ;

            TextBoxMaxMemoryAllowance.Text = (Math.Round(availableFreeMemoryMbtes * 0.6)).ToString();
            TextBoxMinReservedMemory.Text = (Math.Round(availableFreeMemoryMbtes * 0.1)).ToString();
            TextBoxMinMemoryBeforeGC.Text = (Math.Round(availableFreeMemoryMbtes * 0.15)).ToString();
            TextBoxMaxConcurentSJobs.Text = (Math.Round(availableFreeMemoryMbtes * 0.2)).ToString();
        }

        private void ButtonApplyConfiguration_Click(object sender, RoutedEventArgs e)
        {
            var enginePath = TextBoxEnginePath.Text;
            var projectPath = TextBoxProjectPath.Text;
            
            if (String.IsNullOrEmpty(enginePath) || String.IsNullOrEmpty(projectPath))
            {
                string messageBoxText = "Please locate engine and project paths.";
                string caption = "Path Error";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Error;

                System.Windows.MessageBox.Show(messageBoxText, caption, button, icon, MessageBoxResult.Yes);
                return;
            }

            int maxMemoryAllowance = Convert.ToInt32(TextBoxMaxMemoryAllowance.Text);
            int minReservedMemory = Convert.ToInt32(TextBoxMinReservedMemory.Text); ;
            int maxConcurentShaderJobs = Convert.ToInt32(TextBoxMaxConcurentSJobs.Text);
            int minMemoryBeforeGC = Convert.ToInt32(TextBoxMinMemoryBeforeGC.Text);

            Modifier modifier = new Modifier(enginePath, projectPath);
            modifier.ApplySettingsToEngineConfig(maxMemoryAllowance, maxConcurentShaderJobs, minReservedMemory);
            modifier.ApplySettingsToProjectConfig(maxMemoryAllowance, minMemoryBeforeGC, minReservedMemory);

            int availableRAM = Convert.ToInt32(TextBoxMaxMemoryAllowance.Text) / 1024;
            int totalCores = Convert.ToInt32(TextBlockTotalCPUCores.Text);

            modifier.ApplyShaderConfig(totalCores, availableRAM);
        }
    }
}
