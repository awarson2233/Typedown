using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Typedown.Core.Utilities
{
    public static class CommandLine
    {
        [return: MaybeNull]
        public static string GetOpenFilePath(string[] commandLineArgs)
        {
            return commandLineArgs?.Where(FileTypeHelper.IsMarkdownFile).FirstOrDefault();
        }
    }
}
