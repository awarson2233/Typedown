using Microsoft.Extensions.DependencyInjection;
using Typedown.UI.ViewModels;

namespace Typedown.UI.Composition;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTypedownUI(this IServiceCollection services)
    {
        services.AddTransient<Phase14ShellViewModel>();
        services.AddTransient<MainPageViewModel>();
        return services;
    }
}
