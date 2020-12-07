using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Options;
using Beholder.Services.PathTree;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Beholder
{
    public class SpectatorWorker : BackgroundService
    {
        private readonly ILogger logger;

        private readonly SpectatorOptions options;
        private readonly IPathTree pathTree;

        public SpectatorWorker(SpectatorOptions options, IPathTree pathTree, ILogger<SpectatorWorker> logger)
        {
            this.options = options;

            this.pathTree = pathTree;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var watcher = new FileSystemWatcher(options.Directory)
            {
                InternalBufferSize = options.BufferSize * 1024,
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.Size | NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            // Preload.
            if (options.OnCreated)
            {
                watcher.Created += async (_, args) => await OnCreatedAsync(args, stoppingToken);
            }

            // Refresh and Preload.
            if (options.OnChanged)
            {
                watcher.Changed += async (_, args) => await OnChangedAsync(args, stoppingToken);
            }

            // Refresh.
            if (options.OnDeleted)
            {
                watcher.Deleted += async (_, args) => await OnDeletedAsync(args, stoppingToken);
            }

            // Refresh the old path and Preload the new path.
            if (options.OnRenamed)
            {
                watcher.Renamed += async (_, args) => await OnRenamedAsync(args, stoppingToken);
            }

            // Start watching.
            watcher.EnableRaisingEvents = true;

            while (!stoppingToken.IsCancellationRequested)
            {
                // Log per hour to show spectator is working.
                logger.LogInformation("Running at: {time}", DateTimeOffset.Now);
                await Task.Delay(3600 * 1000, stoppingToken);
            }
        }

        private async Task OnCreatedAsync(FileSystemEventArgs args, CancellationToken stoppingToken)
        {
            if (options.ExcludeExtensions.Contains(Path.GetExtension(args.FullPath),
                    StringComparer.OrdinalIgnoreCase) ||
                options.ExcludePaths.Any(x => args.FullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await pathTree.WaitAsync(stoppingToken);

            var fullPath = args.FullPath;
            var isDir = Directory.Exists(fullPath);
            fullPath = TrimDirectoryRoot(fullPath);

            logger.LogInformation("File system info created at: {path}", fullPath);

            // Only refresh files.
            if (!isDir && options.PreloadEnabled)
            {
                await pathTree.AddPathAsync(false, fullPath, isDir, false, !await pathTree.ExistsAsync(true, fullPath));
            }
        }

        private async Task OnChangedAsync(FileSystemEventArgs args, CancellationToken stoppingToken)
        {
            if (options.ExcludeExtensions.Contains(Path.GetExtension(args.FullPath),
                    StringComparer.OrdinalIgnoreCase) ||
                options.ExcludePaths.Any(x => args.FullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await pathTree.WaitAsync(stoppingToken);

            var fullPath = args.FullPath;
            var isDir = Directory.Exists(fullPath);
            fullPath = TrimDirectoryRoot(fullPath);

            logger.LogInformation("File system info changed at: {path}", fullPath);

            await pathTree.AddPathAsync(true, fullPath, isDir, isDir);

            if (!isDir && options.PreloadEnabled)
            {
                await pathTree.AddPathAsync(false, fullPath, isDir);
            }
        }

        private async Task OnDeletedAsync(FileSystemEventArgs args, CancellationToken stoppingToken)
        {
            if (options.ExcludeExtensions.Contains(Path.GetExtension(args.FullPath),
                    StringComparer.OrdinalIgnoreCase) ||
                options.ExcludePaths.Any(x => args.FullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await pathTree.WaitAsync(stoppingToken);

            var fullPath = args.FullPath;
            var isDir = Directory.Exists(fullPath);
            fullPath = TrimDirectoryRoot(fullPath);

            logger.LogInformation("File system info deleted at: {path}", fullPath);

            await pathTree.AddPathAsync(true, fullPath, isDir, isDir);
        }

        private async Task OnRenamedAsync(RenamedEventArgs args, CancellationToken stoppingToken)
        {
            if (options.ExcludeExtensions.Contains(Path.GetExtension(args.FullPath),
                    StringComparer.OrdinalIgnoreCase) ||
                options.ExcludePaths.Any(x => args.FullPath.StartsWith(x, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await pathTree.WaitAsync(stoppingToken);

            var oldFullPath = TrimDirectoryRoot(args.OldFullPath);
            var newFullPath = args.FullPath;
            var isDir = Directory.Exists(oldFullPath);
            newFullPath = TrimDirectoryRoot(newFullPath);

            logger.LogInformation("File system info renamed: from {oldPath} to {newPath}", oldFullPath, newFullPath);

            await pathTree.AddPathAsync(true, oldFullPath, isDir, isDir);

            if (!isDir && options.PreloadEnabled)
            {
                await pathTree.AddPathAsync(false, newFullPath, isDir);
            }
        }

        private string TrimDirectoryRoot(string dir)
        {
            return dir.StartsWith(options.TrimStart) && dir.Length > options.TrimStart.Length
                ? dir.Substring(options.TrimStart.Length)
                : dir;
        }
    }
}
