using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Models.PathTree;
using Beholder.Options.Handler;
using Cysharp.Text;
using Microsoft.Extensions.Logging;

namespace Beholder.Services.Handler
{
    public class ConsoleLogHandler : IHandler
    {
        private readonly ILogger logger;
        private readonly ConsoleLogOptions options;

        public ConsoleLogHandler(ConsoleLogOptions options, ILogger<ConsoleLogHandler> logger)
        {
            this.options = options;

            this.logger = logger;
        }

        public Task<bool> RefreshAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default)
        {
            var pathList = paths.ToList();

            foreach (var fileNode in pathList.Where(p => !p.IsDirectory))
            {
                logger.LogInformation(ZString.Concat("Refresh file: ", options.Prefix, fileNode.Path));
            }

            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Refresh task canceled due to stoppingToken");
                return Task.FromCanceled<bool>(stoppingToken);
            }

            foreach (var dirNode in pathList.Where(p => p.IsDirectory && p.WillForceUpdate))
            {
                logger.LogInformation(ZString.Concat("Refresh directory: ", options.Prefix, dirNode.Path));
            }

            return Task.FromResult(true);
        }

        public Task<bool> PreloadAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default)
        {
            var pathList = paths.ToList();

            foreach (var fileNode in pathList.Where(p => !p.IsDirectory))
            {
                logger.LogInformation(ZString.Concat("Preload file: ", options.Prefix, fileNode.Path));
            }

            return Task.FromResult(true);
        }
    }
}
