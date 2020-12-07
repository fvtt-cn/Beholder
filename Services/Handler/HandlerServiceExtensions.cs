using System;
using Beholder.Options.Handler;
using Microsoft.Extensions.DependencyInjection;

namespace Beholder.Services.Handler
{
    public static class HandlerServiceExtensions
    {
        public static IServiceCollection AddHandler(this IServiceCollection services,
            Action<HandlerOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();

            var options = new HandlerOptions();
            setupAction.Invoke(options);

            if (options.Method.Equals(nameof(ConsoleLogHandler).Replace("Handler", string.Empty),
                StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton(options.ConsoleLog);
                services.AddSingleton<IHandler, ConsoleLogHandler>();
            }
            else if (options.Method.Equals(nameof(AliYunCdnHandler).Replace("Handler", string.Empty),
                StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton(options.AliYunCdn);
                services.AddScoped<IHandler, AliYunCdnHandler>();
            }

            return services;
        }
    }
}
