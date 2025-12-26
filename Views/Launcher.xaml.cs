using DCSSimpleLauncher.Helper;
using Lua;
using Lua.Standard;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DCSSimpleLauncher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Launcher : Page
    {
        public Launcher()
        {
            this.InitializeComponent();
        }

        private void ForceStopDCS_Click(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("DCS");
            foreach (var process in processes)
            {
                process.Kill();
            }
        }

        private async void LaunchDCS_Click(object sender, RoutedEventArgs e)
        {
            var state = await LoadConfigState();

            await state.DoStringAsync("options.miscellaneous.launcher = false");

            var options = state.Environment["options"].Read<LuaTable>();
            var output = "options = " + options.ToFormattedString();

            await BackupDCSConfig();

            // Write no launcher
            (string dcsConfigFilePath, _) = await GetConfigFilePaths();
            File.WriteAllText(dcsConfigFilePath, output);

            string DCSFolderPath = await GetDCSFolderPath();
            string dcsExePath = System.IO.Path.Combine(DCSFolderPath, "bin\\DCS.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = dcsExePath,
                UseShellExecute = true
            });
        }

        private async void LaunchDCSWithLauncher_Click(object sender, RoutedEventArgs e)
        {
            var state = await LoadConfigState();

            await state.DoStringAsync("options.miscellaneous.launcher = true");

            var options = state.Environment["options"].Read<LuaTable>();
            var output = "options = " + options.ToFormattedString();

            await BackupDCSConfig();

            // Write with launcher
            (string dcsConfigFilePath, _) = await GetConfigFilePaths();
            File.WriteAllText(dcsConfigFilePath, output);

            string DCSFolderPath = await GetDCSFolderPath();
            string dcsExePath = System.IO.Path.Combine(DCSFolderPath, "bin\\DCS.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = dcsExePath,
                UseShellExecute = true
            });
        }

        private async Task<LuaState> LoadConfigState()
        {
            (string dcsConfigFilePath, _) = await GetConfigFilePaths();
            var state = LuaState.Create();
            await state.DoFileAsync(dcsConfigFilePath);

            return state;
        }

        private async Task BackupDCSConfig()
        {
            (string dcsConfigFilePath, string dcsConfigBackupFilePath) = await GetConfigFilePaths();

            File.Copy(dcsConfigFilePath, dcsConfigBackupFilePath, true);
        }

        private async Task<(string dcsConfigFilePath, string dcsConfigBackupFilePath)> GetConfigFilePaths()
        {
            string SavedGamesDCSFolderPath = ApplicationData.Current.LocalSettings.Values["SavedGamesDCSFolder"]?.ToString();

            if (SavedGamesDCSFolderPath is null or "")
            {
                var dialog = new ContentDialog
                {
                    Title = "DCS Saved Games folder not set",
                    Content = "DCS Saved Games is not set in settings. Set folders in settings.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };

                await dialog.ShowAsync();
            }

            string dcsConfigFilePath = Path.Combine(SavedGamesDCSFolderPath, "Config/Options.lua");
            string dcsConfigBackupFilePath = Path.Combine(SavedGamesDCSFolderPath, "Config/Options_backup.lua");

            return (dcsConfigFilePath, dcsConfigBackupFilePath);
        }

        private async Task<string> GetDCSFolderPath()
        {
            string dcsFolderPath = ApplicationData.Current.LocalSettings.Values["DCSFolder"]?.ToString();
            if (dcsFolderPath is null or "")
            {
                var dialog = new ContentDialog
                {
                    Title = "DCS Folder not set",
                    Content = "DCS folder is not set in settings. Set folder in settings.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
            return dcsFolderPath;
        }
    }
}
