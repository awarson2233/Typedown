using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.Core;
using Typedown.Core;
using Typedown.Core.Interfaces;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Services;

internal sealed class WinUIWebViewEnvironmentService
{
    private readonly object environmentLock = new();
    private readonly IAppDataPathProvider appDataPathProvider;
    private Task<CoreWebView2Environment>? environmentTask;

    public WinUIWebViewEnvironmentService(IAppDataPathProvider appDataPathProvider)
    {
        this.appDataPathProvider = appDataPathProvider;
    }

    public void StartPrewarm()
    {
        _ = GetEnvironmentAsync();
        StartupTrace.Mark("WebView2 environment prewarm scheduled");
    }

    public Task<CoreWebView2Environment> GetEnvironmentAsync()
    {
        lock (environmentLock)
        {
            environmentTask ??= CreateEnvironmentAsync();
            return environmentTask;
        }
    }

    private async Task<CoreWebView2Environment> CreateEnvironmentAsync()
    {
        StartupTrace.CoreWebView2EnvironmentCreateStart();
        try
        {
            // 命令行开关见 Config.WebView2Args；同一用户数据目录下的所有 WebView2 必须用同一组开关。
            var commandLineArgs = new List<string>(Config.WebView2Args);
#if DEBUG
            commandLineArgs.Add("--remote-debugging-port=9222");
#endif
            var options = new CoreWebView2EnvironmentOptions
            {
                AdditionalBrowserArguments = string.Join(" ", commandLineArgs)
            };

            // 用户数据目录固定在应用本地目录下，避免使用 WebView2 默认位置导致的路径不可控。
            var userDataFolder = Path.Combine(appDataPathProvider.GetLocalFolderPath(), Config.WebView2UserDataFolderName);
            Directory.CreateDirectory(userDataFolder);

            return await CoreWebView2Environment.CreateWithOptionsAsync(browserExecutableFolder: null, userDataFolder: userDataFolder, options: options);
        }
        finally
        {
            StartupTrace.CoreWebView2EnvironmentCreateStop();
        }
    }
}
