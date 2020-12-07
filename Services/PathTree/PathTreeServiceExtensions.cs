using System;
using Microsoft.Extensions.DependencyInjection;

namespace Beholder.Services.PathTree
{
    public static class PathTreeServiceExtensions
    {
        public static IServiceCollection AddPathTree(this IServiceCollection services)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOptions();

            services.AddSingleton<IPathTree, PathTree>();

            return services;
        }
    }
}
