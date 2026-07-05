using DCSSimpleLauncher.Data;
using DCSSimpleLauncher.Helper;
using Lua;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
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
        private static readonly JsonSerializerOptions ProfileJsonOptions = new() { WriteIndented = true };

        private Profile CurrentProfile = new();

        public bool IsVRModeOn {
            get => CurrentProfile.UseVR;
            set
            {
                CurrentProfile.UseVR = value;
                SaveCurrentProfile();
            }
        }

        public bool IsLauncherOn {
            get => CurrentProfile.UseLauncher;
            set
            {
                CurrentProfile.UseLauncher = value;
                SaveCurrentProfile();
            }
        }

        public Launcher()
        {
            this.InitializeComponent();

            var localSettings = ApplicationData.Current.LocalSettings;

            if (!localSettings.Values.ContainsKey(SettingsKeys.CURRENT_PROFILE))
            {
                localSettings.Values[SettingsKeys.CURRENT_PROFILE] = "Default.json";
            }

            var currentProfileFile = ResolveProfilePath();
            if (currentProfileFile != null)
            {
                try
                {
                    if (File.Exists(currentProfileFile))
                    {
                        var fileContent = File.ReadAllText(currentProfileFile);
                        var deserialized = JsonSerializer.Deserialize<Profile>(fileContent);
                        if (deserialized != null)
                        {
                            CurrentProfile = deserialized;
                        }
                    }
                    else
                    {
                        File.WriteAllText(currentProfileFile, JsonSerializer.Serialize(CurrentProfile, ProfileJsonOptions));
                    }
                }
                catch
                {
                    // HACK, log things
                }
            }
        }

        private static string? ResolveProfilePath()
        {
            var localSettings = ApplicationData.Current.LocalSettings;
            var savedGamesFolder = localSettings.Values[SettingsKeys.SAVEDGAMES_DCSLAUNCHER_FOLDER] as string;
            if (string.IsNullOrEmpty(savedGamesFolder))
            {
                return null;
            }

            var currentProfileName = localSettings.Values[SettingsKeys.CURRENT_PROFILE] as string ?? "Default.json";
            return Path.Combine(savedGamesFolder, currentProfileName);
        }

        private void SaveCurrentProfile()
        {
            var currentProfileFile = ResolveProfilePath();
            if (currentProfileFile is null)
            {
                return;
            }

            try
            {
                File.WriteAllText(currentProfileFile, JsonSerializer.Serialize(CurrentProfile, ProfileJsonOptions));
            }
            catch
            {
                // HACK, log things
            }
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
            string? savedGamesDCSFolderPath = ApplicationData.Current.LocalSettings.Values["SavedGamesDCSFolder"]?.ToString();
            string? dcsFolderPath = ApplicationData.Current.LocalSettings.Values["DCSFolder"]?.ToString();

            if (savedGamesDCSFolderPath is null or "" || dcsFolderPath is null or "")
            {
                var dialog = new ContentDialog
                {
                    Title = "DCS Saved Games folder/DCS Folder path not set",
                    Content = "DCS Saved Games and or DCS folder are not set in settings. Set folders in settings.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                return;
            }
            string dcsConfigFilePath = Path.Combine(savedGamesDCSFolderPath, "Config/Options.lua");
            string dcsConfigBackupFilePath = Path.Combine(savedGamesDCSFolderPath, "Config/Options_backup.lua");

            var state = LuaState.Create();
            await state.DoFileAsync(dcsConfigFilePath);
            
            if (state == null) {
                var dialog = new ContentDialog
                {
                    Title = "Unable to load config State",
                    Content = "May be folders in settings are set incorrectly.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();

                return;
            }

            var VROn = IsVRModeOn ? "true" : "false";
            var LauncherOn = IsLauncherOn ? "true" : "false";

            await state.DoStringAsync($"options.miscellaneous.launcher = {LauncherOn}");
            await state.DoStringAsync($"options.VR.enable = {VROn}");

            var options = state.Environment["options"].Read<LuaTable>();
            if (options is null) {
                var dialog = new ContentDialog
                {
                    Title = "Unable to read options",
                    Content = "Something may be wrong in options.lua",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            string output = "options = " + options.ToFormattedString();

            File.Copy(dcsConfigFilePath, dcsConfigBackupFilePath, true);

            // Write no launcher
            File.WriteAllText(dcsConfigFilePath, output);

            string dcsExePath = System.IO.Path.Combine(dcsFolderPath, "bin\\DCS.exe");
            Process.Start(new ProcessStartInfo
            {
                FileName = dcsExePath,
                UseShellExecute = true
            });
        }
    }
}
