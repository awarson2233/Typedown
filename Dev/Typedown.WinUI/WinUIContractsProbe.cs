using Typedown.Core.Interfaces;
using Typedown.WinUI.Services;

namespace Typedown.WinUI
{
    internal static class WinUIContractsProbe
    {
        public static string Describe(WinUIPlatformServices services)
        {
            var localFolder = services.AppDataPathProvider.GetLocalFolderPath();
            return $"Typedown contracts wired to WinUI services: {string.Join(", ", services.ServiceNames)}. Local data root: {localFolder}. Activation remains a stub over the contract surface only.";
        }
    }
}
