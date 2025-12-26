using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DCSSimpleLauncher
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        //private AppWindow m_AppWindow;

        public MainWindow()
        {
            this.InitializeComponent();

            //AppWindow.Changed += AppWindowChanged;

            //AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            //AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            //AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // 2 lines below are for WinUI 3 but WinUI 3 does not support controls in title bar yet
            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            ContentFrame.Navigate(typeof(Views.Launcher), null, new SuppressNavigationTransitionInfo());
            //AppTitleBar.Loaded += AppTitleBarLoaded;
            //AppTitleBar.SizeChanged += AppTitleBarSizeChanged;
        }

        private void MainNavigation_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.IsSettingsInvoked == true)
            {
                ContentFrame.Navigate(typeof(Views.SettingsPage), null, args.RecommendedNavigationTransitionInfo);
            }
            else if (args.InvokedItemContainer != null && (args.InvokedItemContainer.Tag != null))
            {
                switch (args.InvokedItemContainer.Tag.ToString())
                {
                    case "Launcher":
                        ContentFrame.Navigate(typeof(Views.Launcher), null, args.RecommendedNavigationTransitionInfo);
                        break;
                }
            }
        }

        private void NavigationPaneButton_Click(object sender, RoutedEventArgs e)
        {
            MainNavigation.IsPaneOpen = !MainNavigation.IsPaneOpen;
        }

        private void NavigationViewControl_DisplayModeChanged(NavigationView sender, NavigationViewDisplayModeChangedEventArgs args)
        {
            AppTitleBar.Margin = new Thickness()
            {
                Left = sender.CompactPaneLength * (sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 2 : 1),
                Top = AppTitleBar.Margin.Top,
                Right = AppTitleBar.Margin.Right,
                Bottom = AppTitleBar.Margin.Bottom
            };
            Console.WriteLine(AppTitleBar.Margin);

            ContentFrame.Margin = new Thickness()
            {
                Left = ContentFrame.Margin.Left,
                Top = sender.DisplayMode == NavigationViewDisplayMode.Minimal ? 48 : 10,
                Right = ContentFrame.Margin.Right,
                Bottom = ContentFrame.Margin.Bottom
            };
        }


        //private void AppTitleBarLoaded(object sender, RoutedEventArgs e)
        //{
        //    if (AppWindowTitleBar.IsCustomizationSupported())
        //    {
        //        SetDragRegionForCustomTitleBar();
        //    }
        //}

        //private void AppTitleBarSizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    if (AppWindowTitleBar.IsCustomizationSupported()
        //        && AppWindow.TitleBar.ExtendsContentIntoTitleBar)
        //    {
        //        // Update drag region if the size of the title bar changes.
        //        SetDragRegionForCustomTitleBar();
        //    }
        //}


        [DllImport("Shcore.dll", SetLastError = true)]
        internal static extern int GetDpiForMonitor(IntPtr hmonitor, Monitor_DPI_Type dpiType, out uint dpiX, out uint dpiY);

        internal enum Monitor_DPI_Type : int
        {
            MDT_Effective_DPI = 0,
            MDT_Angular_DPI = 1,
            MDT_Raw_DPI = 2,
            MDT_Default = MDT_Effective_DPI
        }

        private double GetScaleAdjustment()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
            DisplayArea displayArea = DisplayArea.GetFromWindowId(wndId, DisplayAreaFallback.Primary);
            IntPtr hMonitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);

            // Get DPI.
            int result = GetDpiForMonitor(hMonitor, Monitor_DPI_Type.MDT_Default, out uint dpiX, out uint _);
            if (result != 0)
            {
                throw new Exception("Could not get DPI for monitor.");
            }

            uint scaleFactorPercent = (uint)(((long)dpiX * 100 + (96 >> 1)) / 96);
            return scaleFactorPercent / 100.0;
        }


        //private void AppWindowChanged(AppWindow sender, AppWindowChangedEventArgs args)
        //{
        //    if (args.DidPresenterChange
        //        && AppWindowTitleBar.IsCustomizationSupported())
        //    {
        //        switch (sender.Presenter.Kind)
        //        {
        //            case AppWindowPresenterKind.CompactOverlay:
        //                // Compact overlay - hide custom title bar
        //                // and use the default system title bar instead.
        //                AppTitleBar.Visibility = Visibility.Collapsed;
        //                sender.TitleBar.ResetToDefault();
        //                break;

        //            case AppWindowPresenterKind.FullScreen:
        //                // Full screen - hide the custom title bar
        //                // and the default system title bar.
        //                AppTitleBar.Visibility = Visibility.Collapsed;
        //                sender.TitleBar.ExtendsContentIntoTitleBar = true;
        //                break;

        //            case AppWindowPresenterKind.Overlapped:
        //                // Normal - hide the system title bar
        //                // and use the custom title bar instead.
        //                AppTitleBar.Visibility = Visibility.Visible;
        //                sender.TitleBar.ExtendsContentIntoTitleBar = true;
        //                // SetDragRegionForCustomTitleBar();
        //                break;

        //            default:
        //                // Use the default system title bar.
        //                sender.TitleBar.ResetToDefault();
        //                break;
        //        }
        //    }
        //}
    }
}
