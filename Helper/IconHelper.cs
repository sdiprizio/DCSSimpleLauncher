#nullable enable
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DCSSimpleLauncher.Helper
{
    internal static class IconHelper
    {
        private static readonly Dictionary<string, BitmapImage?> Cache = new();
        private static readonly string CacheDir = Path.Combine(Path.GetTempPath(), "DCSSimpleLauncherIconCache");

        public static BitmapImage? GetIcon(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            if (Cache.TryGetValue(path, out var cached)) return cached;

            BitmapImage? image = null;
            try
            {
                Directory.CreateDirectory(CacheDir);
                var hash = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(path)));
                var cacheFile = Path.Combine(CacheDir, $"{hash}.png");

                if (!File.Exists(cacheFile))
                {
                    using var icon = Icon.ExtractAssociatedIcon(path);
                    if (icon is null)
                    {
                        Cache[path] = null;
                        return null;
                    }

                    using var bitmap = icon.ToBitmap();
                    bitmap.Save(cacheFile, ImageFormat.Png);
                }

                image = new BitmapImage(new Uri(cacheFile));
            }
            catch
            {
                image = null;
            }

            Cache[path] = image;
            return image;
        }
    }
}
