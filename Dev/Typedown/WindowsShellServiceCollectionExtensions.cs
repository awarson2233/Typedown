using Microsoft.Extensions.DependencyInjection;
using Typedown.Controls;
using Typedown.Controls.FloatControls;
using Typedown.Core.Interfaces;
using Typedown.Interfaces;
using Typedown.Presentation.Interfaces;
using Typedown.Services;

namespace Typedown
{
    public static class WindowsShellServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedownWindowsShell(this IServiceCollection services)
        {
            services.AddScoped<IClipboard, Clipboard>();
            services.AddSingleton<IAppActivationService, AppActivationService>();
            services.AddScoped<IDialogService, DialogService>();
            services.AddScoped<IFloatViewService, FloatViewService>();
            services.AddScoped<IFileConverter, FileConverter>();
            services.AddScoped<IFileExport, FileExport>();
            services.AddScoped<IFilePickerService, FilePickerService>();
            services.AddScoped<IFileOperation, FileOperation>();
            services.AddScoped<IKeyboardAccelerator, KeyboardAccelerator>();
            services.AddScoped<IEditorCommandSink, EditorCommandSink>();
            services.AddScoped<IEditorSettingsNotifier, EditorSettingsNotifier>();
            services.AddScoped<UiDispatcher>();
            services.AddScoped<IUiDispatcher>(sp => sp.GetRequiredService<UiDispatcher>());
            services.AddScoped<IPowerShellService, PowerShellService>();
            services.AddScoped<ITableDialogService, TableDialogService>();
            services.AddScoped<WindowContext>();
            services.AddScoped<IWindowContext>(sp => sp.GetRequiredService<WindowContext>());
            services.AddScoped<IWindowService, WindowService>();
            services.AddSingleton<IAppDataPathProvider, AppDataPathProvider>();

            services.AddScoped<IMarkdownEditor, MarkdownEditor>();
            services.AddTransient<FrontMenu>();
            services.AddTransient<TableTools>();
            services.AddTransient<ImageSelector>();
            services.AddTransient<ImageToolbar>();
            services.AddTransient<Controls.FloatControls.ToolTip>();

            return services;
        }
    }
}
