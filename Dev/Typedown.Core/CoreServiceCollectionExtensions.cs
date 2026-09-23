using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Services;
using Typedown.Core.ViewModels;
using Typedown.Services;

namespace Typedown.Core
{
    public static class CoreServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedownCore(this IServiceCollection services)
        {
            services.AddScoped<IAtomicFileWriter, AtomicFileWriter>();
            services.AddScoped<AutoBackup>();
            services.AddScoped<EventCenter>();
            services.AddScoped<RemoteInvoke>();
            services.AddScoped<Transport>();
            services.AddSingleton<AccessHistory>();

            services.AddScoped<AppViewModel>();
            services.AddScoped<EditorViewModel>();
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
