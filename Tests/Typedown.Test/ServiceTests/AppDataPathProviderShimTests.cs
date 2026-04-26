using Microsoft.VisualStudio.TestTools.UnitTesting;
using Typedown.Core;
using Typedown.Core.Interfaces;

namespace Typedown.Test.ServiceTests
{
    [TestClass]
    [DoNotParallelize]
    public class AppDataPathProviderShimTests
    {
        private IAppDataPathProvider originalProvider;

        [TestInitialize]
        public void SetUp()
        {
            originalProvider = Config.GetAppDataPathProvider();
        }

        [TestCleanup]
        public void TearDown()
        {
            Config.SetAppDataPathProvider(originalProvider);
        }

        [TestMethod]
        public void ConfigPathApis_UseInjectedPathProvider()
        {
            var provider = new FakeAppDataPathProvider(
                @"D:\typedown-test",
                @"D:\typedown-test\Settings.json",
                @"D:\typedown-test\Storage.db",
                @"D:\typedown-test\Backup");
            Config.SetAppDataPathProvider(provider);

            Assert.AreEqual(provider.GetLocalFolderPath(), Config.GetLocalFolderPath());
            Assert.AreEqual(provider.GetSettingsFilePath(), Config.GetSettingsFilePath());
            Assert.AreEqual(provider.GetDatabaseFilePath(), Config.GetDatabaseFilePath());
            Assert.AreEqual(provider.GetBackupFolderPath(), Config.GetBackupFolderPath());
        }

        private sealed class FakeAppDataPathProvider : IAppDataPathProvider
        {
            private readonly string localFolderPath;
            private readonly string settingsFilePath;
            private readonly string databaseFilePath;
            private readonly string backupFolderPath;

            public FakeAppDataPathProvider(string localFolderPath, string settingsFilePath, string databaseFilePath, string backupFolderPath)
            {
                this.localFolderPath = localFolderPath;
                this.settingsFilePath = settingsFilePath;
                this.databaseFilePath = databaseFilePath;
                this.backupFolderPath = backupFolderPath;
            }

            public string GetLocalFolderPath() => localFolderPath;

            public string GetSettingsFilePath() => settingsFilePath;

            public string GetDatabaseFilePath() => databaseFilePath;

            public string GetBackupFolderPath() => backupFolderPath;
        }
    }
}
