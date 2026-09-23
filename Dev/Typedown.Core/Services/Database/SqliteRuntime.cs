using System;
using System.IO;

namespace Typedown.Core.Services
{
    /// <summary>
    /// One-time native SQLite setup, run lazily on the first database access instead of on the UI thread at launch.
    /// </summary>
    internal static class SqliteRuntime
    {
        private static readonly object initializeLock = new();

        private static bool initialized;

        public static void EnsureInitialized()
        {
            if (initialized)
                return;

            lock (initializeLock)
            {
                if (initialized)
                    return;

                // Must happen before the first SqliteConnection is created: Microsoft.Data.Sqlite probes
                // ApplicationData.Current in its static constructor, which throws in unpackaged apps.
                try
                {
                    var tmp = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        Config.AppName,
                        "temp");
                    Directory.CreateDirectory(tmp);
                    Environment.SetEnvironmentVariable("SQLITE_TMPDIR", tmp);
                }
                catch
                {
                    // SQLite falls back to its default temp directory.
                }

                SQLitePCL.Batteries_V2.Init();
                initialized = true;
            }
        }
    }
}
