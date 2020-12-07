using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Models.PathTree;
using Beholder.Options;
using Beholder.Services.Handler;
using Beholder.Services.PathTree;
using Beholder.Services.Pruning;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beholder
{
    public class ThiefWorker : BackgroundService
    {
        private readonly ILogger logger;

        private readonly ThiefOptions options;
        private readonly IPathTree pathTree;
        private readonly IServiceProvider serviceProvider;

        public ThiefWorker(ThiefOptions options,
            IServiceProvider provider,
            IPathTree pathTree,
            ILogger<ThiefWorker> logger)
        {
            this.options = options;

            serviceProvider = provider;
            this.pathTree = pathTree;

            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Running at: {time}", DateTimeOffset.Now);

                Node<PathNode>? refreshTree = null;
                Node<PathNode>? preloadTree = null;

                try
                {
                    refreshTree = await CheckTreeAsync(true, stoppingToken);
                    preloadTree = await CheckTreeAsync(false, stoppingToken);
                }
                catch (OperationCanceledException oce)
                {
                    logger.LogError(oce, "Unable to fetch trees");
                }

                using var scope = serviceProvider.CreateScope();
                var pruning = scope.ServiceProvider.GetRequiredService<IPruning>();
                var handler = scope.ServiceProvider.GetRequiredService<IHandler>();

                try
                {
                    await ProcessAsync(pruning, handler, refreshTree, preloadTree, stoppingToken);
                }
                catch (ApplicationException aEx)
                {
                    logger.LogError(aEx, "Application Exception threw at: {time}", DateTimeOffset.Now);
                }

                await DisposeServices(handler, pruning);

                // Wait interval.
                await Task.Delay(options.CheckInterval * 1000, stoppingToken);
            }
        }

        private async Task ProcessAsync(IPruning pruning,
            IHandler handler,
            Node<PathNode>? refreshTree,
            Node<PathNode>? preloadTree,
            CancellationToken stoppingToken = default)
        {
            try
            {
                pruning.PrunePathTree(refreshTree ?? new Node<PathNode>(new PathNode("", null)),
                    preloadTree ?? new Node<PathNode>(new PathNode("", null)));

                if (refreshTree is not null &&
                    await handler.RefreshAsync(refreshTree.All.Select(x => x.Value), stoppingToken))
                {
                    logger.LogInformation("Refreshing trees completed at: {time}", DateTimeOffset.Now);
                }

                if (preloadTree is not null &&
                    await handler.PreloadAsync(preloadTree.All.Select(x => x.Value), stoppingToken))
                {
                    logger.LogInformation("Preloading trees completed at: {time}", DateTimeOffset.Now);
                }
            }
            catch (OperationCanceledException oce)
            {
                logger.LogError(oce, "Processing trees operation canceled at: {time}", DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"Processing trees failed at: {DateTimeOffset.Now}", ex);
            }
        }

        private async Task DisposeServices(params object[] services)
        {
            foreach (var service in services)
            {
                switch (service)
                {
                    case IAsyncDisposable asyncDisposable:
                        await asyncDisposable.DisposeAsync();
                        break;
                    case IDisposable disposable:
                        disposable.Dispose();
                        break;

                    default:
                        logger.LogTrace("Service not implemented any disposable method: {name}",
                            service.GetType().FullName);
                        break;
                }
            }
        }

        private async Task<Node<PathNode>?> CheckTreeAsync(bool isRefresh, CancellationToken stoppingToken = default)
        {
            // Block adding.
            await pathTree.BlockAsync(stoppingToken);
            logger.LogTrace("Blocked Path Tree at: {time}", DateTimeOffset.Now);

            // Get tree and clear.
            var count = isRefresh ? options.RefreshTakeCount : options.PreloadTakeCount;
            var tree = await pathTree.GetPathAsync(isRefresh, count);
            logger.LogInformation("Took Path Tree items: {count}", tree?.All.Count() ?? 0);

            if (tree is not null)
            {
                // Clear paths.
                await pathTree.ClearPathAsync(isRefresh, tree.All.Select(n => n.Value.Path));
                logger.LogInformation("Cleared Path Tree items that was taken");
            }

            // Release the lock.
            await pathTree.ReleaseAsync(stoppingToken);
            logger.LogTrace("Released Path Tree at: {time}", DateTimeOffset.Now);

            return tree;
        }
    }
}
