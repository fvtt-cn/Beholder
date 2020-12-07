using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Models.PathTree;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;

namespace Beholder.Services.PathTree
{
    public class PathTree : IPathTree
    {
        private readonly ILogger logger;

        private readonly AsyncManualResetEvent mre;
        private readonly ConcurrentDictionary<string, PathNode> preloadPaths;
        private readonly ConcurrentDictionary<string, PathNode> refreshPaths;

        public PathTree(ILogger<PathTree> logger)
        {
            this.logger = logger;

            mre = new AsyncManualResetEvent(true);
            refreshPaths = new ConcurrentDictionary<string, PathNode>();
            preloadPaths = new ConcurrentDictionary<string, PathNode>();
        }

        public Task BlockAsync(CancellationToken stoppingToken = default)
        {
            mre.Reset();
            return Task.CompletedTask;
        }

        public Task ReleaseAsync(CancellationToken stoppingToken = default)
        {
            mre.Set();
            return Task.CompletedTask;
        }

        public Task WaitAsync(CancellationToken stoppingToken = default)
        {
            return mre.WaitAsync(stoppingToken);
        }

        public Task AddPathAsync(bool isRefresh,
            string fullPath,
            bool isDir,
            bool forceUpdate = false,
            bool justCreated = false)
        {
            var paths = isRefresh ? refreshPaths : preloadPaths;

            fullPath = fullPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var splitPaths = fullPath.Split(Path.AltDirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
            var parent = string.Empty;

            // Skip filename, handle parent paths.
            foreach (var splitPath in splitPaths.SkipLast(1))
            {
                var sPath = Path.Combine(parent, splitPath)
                    .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                var sNode = new PathNode(sPath, parent, true);
                paths.TryAdd(sPath, sNode);
                parent = sPath;
            }

            // Original file path.
            var infoNode = new PathNode(fullPath, parent, isDir, forceUpdate)
            {
                JustCreated = justCreated
            };

            paths.AddOrUpdate(fullPath, infoNode, (_, oldNode) =>
            {
                oldNode.WillForceUpdate = oldNode.WillForceUpdate || forceUpdate;
                return oldNode;
            });

            logger.LogTrace("Tree added node for: {path}", fullPath);

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(bool isRefresh, string fullPath)
        {
            return Task.FromResult(Exists(isRefresh, fullPath));
        }

        public Task<Node<PathNode>?> GetPathAsync(bool isRefresh, int takeCount = 1000)
        {
            // Check count.
            if (takeCount < 1)
            {
                logger.LogWarning("Tree taken invalid {count} items", takeCount);
                return Task.FromResult<Node<PathNode>?>(null);
            }

            var paths = isRefresh ? refreshPaths : preloadPaths;

            // Add root.
            if (!paths.TryAdd(string.Empty, new PathNode(string.Empty, null, true)))
            {
                return Task.FromResult<Node<PathNode>?>(null);
            }

            // Tree-ize.
            var rootNodes = Node<PathNode>.CreateTree(paths.Values.Take(takeCount), p => p.Path, p => p.ParentPath);
            var rootNode = rootNodes.SingleOrDefault();

            logger.LogInformation("Tree built with nodes for: {count}", rootNode?.All.Count() ?? 0);

            return Task.FromResult(rootNode);
        }

        public Task ClearPathAsync(bool isRefresh, IEnumerable<string> paths)
        {
            var tree = isRefresh ? refreshPaths : preloadPaths;
            foreach (var path in paths)
            {
                tree.TryRemove(path, out _);
            }

            logger.LogInformation("Tree pre-built list cleared at: {time}", DateTimeOffset.Now);

            return Task.CompletedTask;
        }

        private bool Exists(bool isRefresh, string fullPath)
        {
            var paths = isRefresh ? refreshPaths : preloadPaths;
            fullPath = fullPath.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return paths.ContainsKey(fullPath);
        }
    }
}
