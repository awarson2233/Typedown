using Typedown.Core.Interfaces;
using Typedown.WinUI.Services;

namespace Typedown.WinUI
{
    internal static class Phase10ContractsProbe
    {
        public static string Describe(WinUIPlatformServices services)
        {
            var localFolder = services.AppDataPathProvider.GetLocalFolderPath();
            return $"Phase 10b contracts wired to WinUI services: {string.Join(", ", services.ServiceNames)}. Local data root: {localFolder}.";
        }
    }
}
