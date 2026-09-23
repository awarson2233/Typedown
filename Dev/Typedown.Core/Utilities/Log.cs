using System;
using System.Threading.Tasks;

namespace Typedown.Core.Utilities
{
    public static class Log
    {
        public static Task Report(string type, string content)
        {
            return Task.Run(() => RemoteService.ReportAsync(new ErrorReport(
                Config.GetAppVersion(),
                Environment.OSVersion.VersionString,
                type,
                content)));
        }
    }
}
