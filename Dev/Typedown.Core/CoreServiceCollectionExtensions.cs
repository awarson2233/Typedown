using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Editor;
using Typedown.Core.Services;
using Typedown.Core.ViewModels;

namespace Typedown.Core
{
    public static class CoreServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedownCore(this IServiceCollection services)
        {
            services.AddScoped<IAtomicFileWriter, AtomicFileWriter>();
            services.AddScoped<AutoBackup>();
            services.AddSingleton<AccessHistory>();

            services.AddScoped<AppViewModel>();
            services.AddScoped<EditorViewModel>();
            services.AddScoped<IEditorHostCallbacks>(sp => sp.GetRequiredService<EditorViewModel>());
            services.AddScoped<FileViewModel>();
            services.AddScoped<FloatViewModel>();
            services.AddScoped<FormatViewModel>();
            services.AddScoped<ParagraphViewModel>();
            services.AddScoped<SettingsViewModel>();
            services.AddScoped<UIViewModel>();

            services.AddScoped<ImageAction>();
            services.AddScoped<ImageUpload>();

            return services;
        }
    }
}
