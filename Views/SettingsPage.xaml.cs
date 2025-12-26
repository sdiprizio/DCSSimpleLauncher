using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Windows.Storage;
using Windows.Storage.Pickers;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DCSSimpleLauncher.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        this.InitializeComponent();

        DCSFolderTextBlock.Text = ApplicationData.Current.LocalSettings.Values["DCSFolder"]?.ToString() ?? "DCS folder not selected.";
        SavedGamesDCSFolderTextBlock.Text = ApplicationData.Current.LocalSettings.Values["SavedGamesDCSFolder"]?.ToString() ?? "DCS folder not selected.";
        DCSLauncherFolderTextBlock.Text = ApplicationData.Current.LocalSettings.Values["DCSLauncherFolder"]?.ToString() ?? "DCS Launcher folder not selected.";
    }

    private async void PickDCSFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker
        FolderPicker openPicker = new FolderPicker
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder
        };
        openPicker.FileTypeFilter.Add("*");

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Window;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);


        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            // StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            ApplicationData.Current.LocalSettings.Values["DCSFolder"] = folder.Path;
            DCSFolderTextBlock.Text = folder.Path;
        }

    }

    private async void SavedGamesPickDCSFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker
        FolderPicker openPicker = new FolderPicker();
        openPicker.FileTypeFilter.Add("*");

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Window;

        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);

        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);


        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            // StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            ApplicationData.Current.LocalSettings.Values["SavedGamesDCSFolder"] = folder.Path;
            SavedGamesDCSFolderTextBlock.Text = folder.Path;
        }
    }

    private async void PicckDCSLauncherFolderButton_Click(object sender, RoutedEventArgs e)
    {
        // Create a folder picker
        FolderPicker openPicker = new FolderPicker();
        openPicker.FileTypeFilter.Add("*");

        // See the sample code below for how to make the window accessible from the App class.
        var window = App.Window;
        // Retrieve the window handle (HWND) of the current WinUI 3 window.
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        // Initialize the file picker with the window handle (HWND).
        WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

        // Open the picker for the user to pick a folder
        StorageFolder folder = await openPicker.PickSingleFolderAsync();
        if (folder != null)
        {
            // StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
            ApplicationData.Current.LocalSettings.Values["DCSLauncherFolder"] = folder.Path;
            DCSLauncherFolderTextBlock.Text = folder.Path;
        }
    }
}
