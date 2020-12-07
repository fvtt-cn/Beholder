using System.Threading.Tasks;
using Beholder.Options;
using Beholder.Services.Handler;
using Beholder.Services.PathTree;
using Beholder.Services.Pruning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Beholder
{
    public class Program
    {
        public static Task Main(string[] args)
        {
            return CreateHostBuilder(args).Build().RunAsync();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Worker options.
                    ConfigureOptionsAsSingleton<SpectatorOptions>("Spectator", hostContext, services);
                    ConfigureOptionsAsSingleton<ThiefOptions>("Thief", hostContext, services);

                    // Workers.
                    services.AddHostedService<SpectatorWorker>();
                    services.AddHostedService<ThiefWorker>();

                    // Services.
                    services.AddPathTree();
                    services.AddPruning(o => hostContext.Configuration.GetSection("Pruning").Bind(o));
                    services.AddHandler(o => hostContext.Configuration.GetSection("Handler").Bind(o));
                });
        }

        private static void ConfigureOptionsAsSingleton<T>(string section,
            HostBuilderContext context,
            IServiceCollection services)
            where T : class
        {
            var options = context.Configuration.GetSection(section).Get<T>();
            services.AddSingleton(options);
        }
    }
}
