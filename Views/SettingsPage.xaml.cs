using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using SimpleDCSLauncher.Helper;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DCSSimpleLauncher.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("DCSFolder"))
            {
                DCSFolderTextBlock.Text = ApplicationData.Current.LocalSettings.Values["DCSFolder"].ToString();
            }
            else
            {
                DCSFolderTextBlock.Text = "DCS folder not selected.";
            }
        }

        private async void PickDCSFolderButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear previous returned file name, if it exists, between iterations of this scenario
            DCSFolderTextBlock.Text = "";

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
            }
            else
            {
                DCSFolderTextBlock.Text = "Operation cancelled.";
            }

        }
    }
}
