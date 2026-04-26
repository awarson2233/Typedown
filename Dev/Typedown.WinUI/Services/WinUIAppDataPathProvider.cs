using System;
using System.IO;
using Typedown.Core.Interfaces;
using Windows.Storage;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIAppDataPathProvider : IAppDataPathProvider
    {
        private readonly string localFolderPath;

        public WinUIAppDataPathProvider()
        {
            localFolderPath = ResolveLocalFolderPath();
            Directory.CreateDirectory(localFolderPath);
        }

        public string GetLocalFolderPath()
        {
            return localFolderPath;
        }

        public string GetSettingsFilePath()
        {
            return Path.Combine(localFolderPath, "settings.json");
        }

        public string GetDatabaseFilePath()
        {
            return Path.Combine(localFolderPath, "typedown.db");
        }

        public string GetBackupFolderPath()
        {
            var path = Path.Combine(localFolderPath, "backup");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string ResolveLocalFolderPath()
        {
            try
            {
                var path = ApplicationData.Current.LocalFolder.Path;
                if (!string.IsNullOrWhiteSpace(path))
                {
                    return path;
                }
            }
            catch
            {
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Typedown",
                "WinUI");
        }
    }
}
