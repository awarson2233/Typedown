using System;
using System.Collections.Generic;
using System.IO;
using Typedown.Core.Interfaces;

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

        // WebView2 启动参数：编辑器页面走 file:// 加载，本地图片读取依赖 allow-file-access-from-files，
        // 滚动条样式依赖 msOverlayScrollbarWinStyle 特性开关。
        public static IReadOnlyList<string> WebView2Args { get; } = new List<string>()
        {
            "--disable-web-security",
            "--allow-file-access-from-files",
            "--flag-switches-begin",
            "--enable-features=msOverlayScrollbarWinStyle",
            "--flag-switches-end"
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

        // Migration compatibility shim: keeps existing static call sites working until
        // path consumers are fully moved to DI-bound IAppDataPathProvider access.
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

        public static bool IsPackaged { get; set; }

        public static string GetAppVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
            return string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision) + (IsPackaged ? "" : " (Unpackaged)");
        }

        private sealed class LegacyAppDataPathProvider : IAppDataPathProvider
        {
            public string GetLocalFolderPath()
            {
                try
                {
                    var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), AppName);
                    if (!Directory.Exists(path))
                        Directory.CreateDirectory(path);
                    return path;
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
