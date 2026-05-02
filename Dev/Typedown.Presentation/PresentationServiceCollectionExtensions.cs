using Microsoft.Extensions.DependencyInjection;
using Typedown.Presentation.Services;
using Typedown.Presentation.ViewModels;

namespace Typedown.Presentation
{
    public static class PresentationServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedownPresentation(this IServiceCollection services)
        {
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
