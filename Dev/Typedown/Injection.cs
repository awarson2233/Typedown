using Microsoft.Extensions.DependencyInjection;
using Typedown.Core;
using Typedown.Core.Interfaces;
using Typedown.Presentation;

namespace Typedown
{
    public static class Injection
    {
        public static ServiceProvider ServiceProvider { get; }

        static Injection()
        {
            if (ServiceProvider == null)
            {
                var builder = new ServiceCollection();
                builder.AddTypedownCore();
                builder.AddTypedownPresentation();
                builder.AddTypedownWindowsShell();
                ServiceProvider = builder.BuildServiceProvider();
                Config.SetAppDataPathProvider(ServiceProvider.GetRequiredService<IAppDataPathProvider>());
            }
        }
    }
}
