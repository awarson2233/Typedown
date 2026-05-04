using System;
using System.IO;
using System.Runtime.InteropServices;
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
            if (HasPackageIdentity())
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
            }

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Typedown",
                "WinUI");
        }

        private static bool HasPackageIdentity()
        {
            var length = 0;
            return GetCurrentPackageFullName(ref length, IntPtr.Zero) == ERROR_INSUFFICIENT_BUFFER;
        }

        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, IntPtr packageFullName);

        private const int ERROR_INSUFFICIENT_BUFFER = 122;
    }
}
