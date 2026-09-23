using System;
using System.IO;
using System.Threading.Tasks;

namespace Typedown.Core.Services
{
    public interface IAtomicFileWriter
    {
        Task WriteAllTextAsync(string path, string content);
        Task WriteAllBytesAsync(string path, byte[] content);
        Task<string> WriteTemporaryAsync(string destinationPath, string content);
        void Commit(string temporaryPath, string destinationPath);
        void Discard(string? temporaryPath);
    }

    public sealed class AtomicFileWriter : IAtomicFileWriter
    {
        public Task WriteAllTextAsync(string path, string content) =>
            File.WriteAllTextAsync(path, content);

        public Task WriteAllBytesAsync(string path, byte[] content) =>
            File.WriteAllBytesAsync(path, content);

        public async Task<string> WriteTemporaryAsync(string destinationPath, string content)
        {
            var directory = Path.GetDirectoryName(destinationPath) ?? AppContext.BaseDirectory;
            Directory.CreateDirectory(directory);
            var temporaryPath = Path.Combine(directory, $".{Path.GetFileName(destinationPath)}.{Guid.NewGuid():N}.tmp");
            await File.WriteAllTextAsync(temporaryPath, content);
            return temporaryPath;
        }

        public void Commit(string temporaryPath, string destinationPath) =>
            File.Move(temporaryPath, destinationPath, true);

        public void Discard(string? temporaryPath)
        {
            if (!string.IsNullOrEmpty(temporaryPath) && File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
