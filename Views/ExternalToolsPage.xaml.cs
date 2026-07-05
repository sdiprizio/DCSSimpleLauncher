#nullable enable
using DCSSimpleLauncher.Data;
using DCSSimpleLauncher.Helper;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace DCSSimpleLauncher.Views
{
    public sealed partial class ExternalToolsPage : Page
    {
        private readonly ObservableCollection<CompanionApp> Tools;
        private readonly Dictionary<CompanionApp, Process> RunningProcesses = new();

        public ExternalToolsPage()
        {
            InitializeComponent();

            Tools = new ObservableCollection<CompanionApp>(ExternalToolsRepository.Load());
            ToolsListView.ItemsSource = Tools;

            DetectRunningToolsAsync();
        }

        private async void DetectRunningToolsAsync()
        {
            var pathToTool = Tools
                .Where(t => !string.Equals(Path.GetExtension(t.Path), ".ps1", StringComparison.OrdinalIgnoreCase))
                .GroupBy(t => t.Path, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            if (pathToTool.Count == 0) return;

            var matches = await Task.Run(() =>
            {
                var found = new List<(string Path, Process Process)>();
                foreach (var process in Process.GetProcesses())
                {
                    try
                    {
                        var path = process.MainModule?.FileName;
                        if (path != null && pathToTool.ContainsKey(path))
                        {
                            found.Add((path, process));
                        }
                    }
                    catch
                    {
                        // Access denied on system/elevated processes; skip.
                    }
                }
                return found;
            });

            foreach (var (path, process) in matches)
            {
                if (!pathToTool.TryGetValue(path, out var tool)) continue;
                if (RunningProcesses.ContainsKey(tool)) continue;

                process.EnableRaisingEvents = true;
                RunningProcesses[tool] = process;
                tool.IsRunning = true;

                process.Exited += (_, _) => DispatcherQueue.TryEnqueue(() =>
                {
                    RunningProcesses.Remove(tool);
                    tool.IsRunning = false;
                });
            }
        }

        private async void AddElement_Click(object sender, RoutedEventArgs e)
        {
            if (ExternalToolsRepository.ResolvePath() is null)
            {
                await ShowMessageAsync("DCS Launcher folder not set", "Set the DCS Launcher folder in Settings before adding external tools.");
                return;
            }

            await ShowEditDialogAsync(null);
        }

        private async void EditTool_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not CompanionApp tool) return;
            await ShowEditDialogAsync(tool);
        }

        private async void DeleteTool_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.DataContext is not CompanionApp tool) return;

            var dialog = new ContentDialog
            {
                Title = "Delete external tool",
                Content = $"Delete \"{tool.Name}\"? This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary) return;

            Tools.Remove(tool);
            ExternalToolsRepository.Save(Tools);
        }

        private async void LaunchTool_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            if (sender.DataContext is not CompanionApp tool) return;

            if (tool.IsRunning)
            {
                StopTool(tool);
                return;
            }

            try
            {
                var extension = Path.GetExtension(tool.Path).ToLowerInvariant();
                var startInfo = extension == ".ps1"
                    ? new ProcessStartInfo
                    {
                        FileName = "powershell.exe",
                        Arguments = $"-ExecutionPolicy Bypass -File \"{tool.Path}\" {tool.Args}",
                        UseShellExecute = true
                    }
                    : new ProcessStartInfo
                    {
                        FileName = tool.Path,
                        Arguments = tool.Args,
                        UseShellExecute = true
                    };

                startInfo.WorkingDirectory = !string.IsNullOrEmpty(tool.WorkingDirectory)
                    ? tool.WorkingDirectory
                    : Path.GetDirectoryName(tool.Path);

                if (tool.RunAsAdmin)
                {
                    startInfo.Verb = "runas";
                }

                var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.EnableRaisingEvents = true;
                    RunningProcesses[tool] = process;
                    tool.IsRunning = true;

                    process.Exited += (_, _) => DispatcherQueue.TryEnqueue(() =>
                    {
                        RunningProcesses.Remove(tool);
                        tool.IsRunning = false;
                    });

                    if (tool.Minimize)
                    {
                        _ = HideWindowAfterDelayAsync(process, tool.HideWindowDelaySeconds);
                    }
                }
            }
            catch (Exception ex)
            {
                await ShowMessageAsync("Unable to launch tool", ex.Message);
            }
        }

        private static async Task HideWindowAfterDelayAsync(Process process, double delaySeconds)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds));
                if (process.HasExited) return;

                process.Refresh();
                var handle = process.MainWindowHandle;
                if (handle != IntPtr.Zero)
                {
                    NativeMethods.ShowWindow(handle, NativeMethods.SW_HIDE);
                }
            }
            catch
            {
                // process may have exited or window unavailable
            }
        }

        private void StopTool(CompanionApp tool)
        {
            if (!RunningProcesses.TryGetValue(tool, out var process)) return;

            try
            {
                if (!process.HasExited)
                {
                    process.CloseMainWindow();
                }
            }
            catch
            {
                // process may have already exited
            }
        }

        private void KillTool_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as MenuFlyoutItem)?.Tag is not CompanionApp tool) return;
            if (!RunningProcesses.TryGetValue(tool, out var process)) return;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(true);
                }
            }
            catch
            {
                // process may have already exited
            }
        }

        private async Task ShowEditDialogAsync(CompanionApp? existing)
        {
            ContentDialog dialog = null!;

            var nameBox = new TextBox { Header = "Name", Text = existing?.Name ?? "" };

            var pathBox = new TextBox { Header = "Path", Text = existing?.Path ?? "" };
            var browseButton = new Button { Content = "Browse...", Margin = new Thickness(0, 4, 0, 0) };
            browseButton.Click += async (_, _) =>
            {
                var picker = new FileOpenPicker();
                picker.FileTypeFilter.Add(".exe");
                picker.FileTypeFilter.Add(".bat");
                picker.FileTypeFilter.Add(".ps1");

                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.Window);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

                var file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    pathBox.Text = file.Path;
                }
            };

            var pathButtonsRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 4, 0, 0) };
            pathButtonsRow.Children.Add(browseButton);

            var reopenForProcessPicker = false;

            if (existing is null)
            {
                var selectProcessButton = new Button { Content = "Select running process..." };
                selectProcessButton.Click += (_, _) =>
                {
                    reopenForProcessPicker = true;
                    dialog.Hide();
                };
                pathButtonsRow.Children.Add(selectProcessButton);
            }

            var pathPanel = new StackPanel();
            pathPanel.Children.Add(pathBox);
            pathPanel.Children.Add(pathButtonsRow);

            var argsBox = new TextBox { Header = "Default launch arguments", Text = existing?.Args ?? "" };
            var workingDirBox = new TextBox { Header = "Working directory (optional)", Text = existing?.WorkingDirectory ?? "" };
            var runAsAdminCheckBox = new CheckBox { Content = "Run as administrator", IsChecked = existing?.RunAsAdmin ?? false };

            var hideWindowCheckBox = new CheckBox { Content = "Hide window after launch", IsChecked = existing?.Minimize ?? false };
            var hideDelayBox = new NumberBox
            {
                Value = existing?.HideWindowDelaySeconds ?? 2.0,
                Minimum = 0,
                SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
                IsEnabled = hideWindowCheckBox.IsChecked ?? false,
                Width = 120
            };
            hideWindowCheckBox.Checked += (_, _) => hideDelayBox.IsEnabled = true;
            hideWindowCheckBox.Unchecked += (_, _) => hideDelayBox.IsEnabled = false;

            var hideDelayRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            hideDelayRow.Children.Add(new TextBlock { Text = "Delay before hiding (seconds)", VerticalAlignment = VerticalAlignment.Center });
            hideDelayRow.Children.Add(hideDelayBox);

            var panel = new StackPanel { Spacing = 8, Width = 600 };
            panel.Children.Add(nameBox);
            panel.Children.Add(pathPanel);
            panel.Children.Add(argsBox);
            panel.Children.Add(workingDirBox);
            panel.Children.Add(runAsAdminCheckBox);
            panel.Children.Add(hideWindowCheckBox);
            panel.Children.Add(hideDelayRow);

            dialog = new ContentDialog
            {
                Title = existing is null ? "Add External Tool" : "Edit External Tool",
                Content = panel,
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                XamlRoot = this.XamlRoot
            };

            ContentDialogResult result;
            while (true)
            {
                result = await dialog.ShowAsync();

                if (reopenForProcessPicker)
                {
                    reopenForProcessPicker = false;

                    var selected = await ShowProcessPickerAsync();
                    if (selected != null)
                    {
                        pathBox.Text = selected.Value.Path;
                        if (string.IsNullOrWhiteSpace(nameBox.Text))
                        {
                            nameBox.Text = selected.Value.Name;
                        }
                    }

                    continue;
                }

                break;
            }

            if (result != ContentDialogResult.Primary) return;

            if (string.IsNullOrWhiteSpace(nameBox.Text) || string.IsNullOrWhiteSpace(pathBox.Text)) return;

            var tool = new CompanionApp
            {
                Name = nameBox.Text,
                Path = pathBox.Text,
                Args = string.IsNullOrWhiteSpace(argsBox.Text) ? null : argsBox.Text,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirBox.Text) ? null : workingDirBox.Text,
                RunAsAdmin = runAsAdminCheckBox.IsChecked ?? false,
                Delay = existing?.Delay ?? 0.0,
                Minimize = hideWindowCheckBox.IsChecked ?? false,
                HideWindowDelaySeconds = hideDelayBox.Value
            };

            if (existing is null)
            {
                Tools.Add(tool);
            }
            else
            {
                var index = Tools.IndexOf(existing);
                if (index >= 0) Tools[index] = tool;
            }

            ExternalToolsRepository.Save(Tools);
        }

        private async Task<(string Name, string Path)?> ShowProcessPickerAsync()
        {
            var processes = new List<(string Name, string Path)>();
            foreach (var process in Process.GetProcesses())
            {
                try
                {
                    var path = process.MainModule?.FileName;
                    if (!string.IsNullOrEmpty(path))
                    {
                        processes.Add((process.ProcessName, path));
                    }
                }
                catch
                {
                    // Access denied on system/elevated processes; skip.
                }
            }

            var distinct = processes
                .GroupBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var listView = new ListView
            {
                ItemsSource = distinct.Select(p => $"{p.Name}  —  {p.Path}").ToList(),
                SelectionMode = ListViewSelectionMode.Single,
                MaxHeight = 400
            };

            var dialog = new ContentDialog
            {
                Title = "Select running process",
                Content = listView,
                PrimaryButtonText = "Select",
                CloseButtonText = "Cancel",
                IsPrimaryButtonEnabled = false,
                XamlRoot = this.XamlRoot
            };

            listView.SelectionChanged += (_, _) => dialog.IsPrimaryButtonEnabled = listView.SelectedIndex >= 0;

            var result = await dialog.ShowAsync();
            if (result != ContentDialogResult.Primary || listView.SelectedIndex < 0) return null;

            return distinct[listView.SelectedIndex];
        }

        private async Task ShowMessageAsync(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
