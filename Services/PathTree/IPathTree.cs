using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Models.PathTree;

namespace Beholder.Services.PathTree
{
    /// <summary>
    ///     Singleton.
    /// </summary>
    public interface IPathTree
    {
        Task BlockAsync(CancellationToken stoppingToken = default);
        Task ReleaseAsync(CancellationToken stoppingToken = default);
        Task WaitAsync(CancellationToken stoppingToken = default);

        Task AddPathAsync(bool isRefresh,
            string fullPath,
            bool isDir,
            bool forceUpdate = false,
            bool justCreated = false);

        Task<bool> ExistsAsync(bool isRefresh, string fullPath);
        Task<Node<PathNode>?> GetPathAsync(bool isRefresh, int takeCount = 1000);
        Task ClearPathAsync(bool isRefresh, IEnumerable<string> paths);
    }
}
