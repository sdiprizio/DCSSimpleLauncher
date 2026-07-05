#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using DCSSimpleLauncher.Data;
using Windows.Storage;

namespace DCSSimpleLauncher.Helper
{
    internal static class ExternalToolsRepository
    {
        private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

        public static string? ResolvePath()
        {
            var folder = ApplicationData.Current.LocalSettings.Values[SettingsKeys.SAVEDGAMES_DCSLAUNCHER_FOLDER] as string;
            return string.IsNullOrEmpty(folder) ? null : Path.Combine(folder, SettingsKeys.EXTERNAL_TOOLS_FILENAME);
        }

        public static List<CompanionApp> Load()
        {
            var path = ResolvePath();
            if (path is null || !File.Exists(path)) return new List<CompanionApp>();
            try
            {
                var content = File.ReadAllText(path);
                return JsonSerializer.Deserialize<List<CompanionApp>>(content) ?? new List<CompanionApp>();
            }
            catch
            {
                return new List<CompanionApp>();
            }
        }

        public static bool Save(IEnumerable<CompanionApp> tools)
        {
            var path = ResolvePath();
            if (path is null) return false;
            File.WriteAllText(path, JsonSerializer.Serialize(tools, Options));
            return true;
        }
    }
}
