using System;
using Beholder.Options.Pruning;
using Microsoft.Extensions.DependencyInjection;

namespace Beholder.Services.Pruning
{
    public static class PruningServiceExtensions
    {
        public static IServiceCollection AddPruning(this IServiceCollection services,
            Action<PruningOptions> setupAction)
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

            var options = new PruningOptions();
            setupAction.Invoke(options);

            if (options.Method.Equals(nameof(SimplePruning), StringComparison.OrdinalIgnoreCase))
            {
                services.AddSingleton(options.SimplePruning);
                services.AddTransient<IPruning, SimplePruning>();
            }

            return services;
        }
    }
}
