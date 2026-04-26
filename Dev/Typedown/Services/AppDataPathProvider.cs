using System;
using System.IO;
using Typedown.Core;
using Typedown.Core.Interfaces;
using Windows.Storage;

namespace Typedown.Services
{
    public class AppDataPathProvider : IAppDataPathProvider
    {
        public string GetLocalFolderPath()
        {
            try
            {
                return ApplicationData.Current.LocalFolder.Path;
            }
            catch (Exception)
            {
                var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Config.AppName);
                if (!Directory.Exists(path))
                    Directory.CreateDirectory(path);
                return path;
            }
        }

        public string GetSettingsFilePath()
        {
            return Path.Combine(GetLocalFolderPath(), "Settings.json");
        }

        public string GetDatabaseFilePath()
        {
            return Path.Combine(GetLocalFolderPath(), "Storage.db");
        }

        public string GetBackupFolderPath()
        {
            return Path.Combine(GetLocalFolderPath(), "Backup");
        }
    }
}
