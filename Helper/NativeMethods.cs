#nullable enable
using System;
using System.Runtime.InteropServices;

namespace DCSSimpleLauncher.Helper
{
    internal static class NativeMethods
    {
        public const int SW_HIDE = 0;

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    }
}
