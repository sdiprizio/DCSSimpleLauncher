#nullable enable
namespace DCSSimpleLauncher.Helper
{
    internal static class ToolStateHelper
    {
        public static string GetLabel(bool isRunning) => isRunning ? "Stop" : "Launch";
    }
}
