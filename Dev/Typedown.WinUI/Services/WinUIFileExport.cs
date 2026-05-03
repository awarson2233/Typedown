using System.Collections.ObjectModel;
using Typedown.Core.Enums;
using Typedown.Core.Models;
using Typedown.Presentation.Interfaces;

namespace Typedown.WinUI.Services
{
    internal sealed class WinUIFileExport : IFileExport
    {
        public ObservableCollection<ExportConfig> ExportConfigs { get; } = new();

        public Task<ExportConfig> AddExportConfig(string? name = null, ExportType type = 0)
        {
            throw new NotSupportedException("WinUI file export configuration is not wired in this migration slice.");
        }

        public Task<ExportConfig> GetExportConfig(int id)
        {
            throw new NotSupportedException("WinUI file export configuration lookup is not wired in this migration slice.");
        }

        public Task Print(string basePath, string html, string? documentName = null)
        {
            throw new NotSupportedException("WinUI print/export is not wired in this migration slice.");
        }

        public Task RemoveExportConfig(int id)
        {
            throw new NotSupportedException("WinUI file export configuration removal is not wired in this migration slice.");
        }

        public Task<bool> SaveExportConfig(ExportConfig config)
        {
            throw new NotSupportedException("WinUI file export configuration persistence is not wired in this migration slice.");
        }

        public Task UpdateExportConfigs()
        {
            return Task.CompletedTask;
        }
    }
}
