using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using Typedown.Core.Interfaces;
using Windows.ApplicationModel;
using Windows.Storage;

namespace System.Runtime.CompilerServices
{
    public static class IsExternalInit { }
}

namespace Typedown.Core
{
    public static class Config
    {
        private static readonly object appDataPathProviderLock = new();

        private static IAppDataPathProvider appDataPathProvider = new LegacyAppDataPathProvider();

        public static bool IsMicaSupported { get; } = Environment.OSVersion.Version.Build >= 22000;

        public static IReadOnlyList<string> WebView2Args { get; } = new List<string>()
        {
            "--disable-web-security",
            "--allow-file-access-from-files",
            "--flag-switches-begin",
            "--enable-features=msOverlayScrollbarWinStyle",
            "--flag-switches-end"
        };

        public static JsonSerializerSettings EditorJsonSerializerSettings = new()
        {
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy(true, true)
            },
            MaxDepth = 256
        };

        public static string GetLocalFolderPath()
        {
            return GetAppDataPathProvider().GetLocalFolderPath();
        }

        public static string GetSettingsFilePath()
        {
            return GetAppDataPathProvider().GetSettingsFilePath();
        }

        public static string GetDatabaseFilePath()
        {
            return GetAppDataPathProvider().GetDatabaseFilePath();
        }

        public static string GetBackupFolderPath()
        {
            return GetAppDataPathProvider().GetBackupFolderPath();
        }

        public static void SetAppDataPathProvider(IAppDataPathProvider provider)
        {
            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            lock (appDataPathProviderLock)
                appDataPathProvider = provider;
        }

        public static IAppDataPathProvider GetAppDataPathProvider()
        {
            lock (appDataPathProviderLock)
                return appDataPathProvider;
        }

        public static string AppName => "Typedown";

        public static bool IsPackaged { get; private set; }

        static Config()
        {
            try
            {
                IsPackaged = Package.Current != null;
            }
            catch
            {
                IsPackaged = false;
            }
        }

        private sealed class LegacyAppDataPathProvider : IAppDataPathProvider
        {
            public string GetLocalFolderPath()
            {
                try
                {
                    return ApplicationData.Current.LocalFolder.Path;
                }
                catch (Exception)
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppName);
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
}
