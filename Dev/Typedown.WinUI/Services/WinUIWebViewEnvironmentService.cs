using System;
using Microsoft.Web.WebView2.Core;
using Typedown.Core.Interfaces;
using Typedown.WinUI.Utilities;

namespace Typedown.WinUI.Services;

internal sealed class WinUIWebViewEnvironmentService
{
    private readonly object environmentLock = new();
    private Task<CoreWebView2Environment>? environmentTask;

    public WinUIWebViewEnvironmentService(IAppDataPathProvider appDataPathProvider)
    {
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
        using (StartupTrace.Phase("CoreWebView2Environment.CreateAsync"))
        {
            return await CoreWebView2Environment.CreateAsync();
        }
    }
}
