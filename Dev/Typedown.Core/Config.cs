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

        // WebView2 启动参数：编辑器页面以 https 虚拟主机加载，不再放开跨域与 file:// 访问；
        // 滚动条样式依赖 msOverlayScrollbarWinStyle 特性开关。
        public static IReadOnlyList<string> WebView2Args { get; } = new List<string>()
        {
            "--flag-switches-begin",
            "--enable-features=msOverlayScrollbarWinStyle",
            "--flag-switches-end"
        };

        /// <summary>
        /// WebView2 用户数据目录（应用本地数据目录下）。同一目录下的所有 WebView2 共用一个浏览器进程，启动参数必须一致；
        /// 新引擎的参数与旧 Muya 构建不同，两种构建并存时共用目录会让后启动的一方建不出环境，所以分开存放（C6 退役旧构建后可以合回）。
        /// </summary>
        public const string WebView2UserDataFolderName = "WebView2Next";

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
