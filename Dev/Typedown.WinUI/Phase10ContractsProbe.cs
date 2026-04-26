using Typedown.Core.Interfaces;

namespace Typedown.WinUI
{
    internal static class Phase10ContractsProbe
    {
        public static string Describe()
        {
            return $"Phase 10a contracts reachable: {nameof(IAppDataPathProvider)}, {nameof(IDialogService)}.";
        }
    }
}
