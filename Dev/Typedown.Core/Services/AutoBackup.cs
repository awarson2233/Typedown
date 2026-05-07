using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
using Typedown.Core.Utilities;

namespace Typedown.Core.Services
{
    public class AutoBackup
    {
        private readonly string backupPath = Config.GetBackupFolderPath();

        [return: MaybeNull]
        public string GetBackupFilePath(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                return null;

            if (!Directory.Exists(backupPath))
                Directory.CreateDirectory(backupPath);

            var pathHash = Common.SimpleHash2(sourcePath);
            var pathFilename = Path.GetFileName(sourcePath);
            return Path.Combine(backupPath, $"{pathHash}_{pathFilename}");
        }

        public async Task<bool> Backup(string path, string markdown)
        {
            try
            {
                var backupFilePath = GetBackupFilePath(path);
                if (backupFilePath == null)
                    return false;

                await File.WriteAllTextAsync(backupFilePath, markdown);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> GetBackup(string path)
        {
            try
            {
                var backupFilePath = GetBackupFilePath(path);
                if (backupFilePath == null || !File.Exists(backupFilePath))
                    return null;

                return await File.ReadAllTextAsync(backupFilePath);
            }
            catch
            {
                return null;
            }
        }

        public void DeleteBackup(string path)
        {
            try
            {
                var backupFilePath = GetBackupFilePath(path);
                if (backupFilePath != null)
                    File.Delete(backupFilePath);
            }
            catch
            {
                // Ignore
            }
        }
    }
}
