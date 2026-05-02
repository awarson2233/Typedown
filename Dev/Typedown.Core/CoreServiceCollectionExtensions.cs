using Microsoft.Extensions.DependencyInjection;
using Typedown.Core.Services;
using Typedown.Services;

namespace Typedown.Core
{
    public static class CoreServiceCollectionExtensions
    {
        public static IServiceCollection AddTypedownCore(this IServiceCollection services)
        {
            services.AddScoped<AutoBackup>();
            services.AddScoped<EventCenter>();
            services.AddScoped<RemoteInvoke>();
            services.AddScoped<Transport>();
            services.AddSingleton<AccessHistory>();

            return services;
        }
    }
}
