using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Beholder.Models.PathTree;

namespace Beholder.Services.Handler
{
    public interface IHandler
    {
        Task<bool> RefreshAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default);

        Task<bool> PreloadAsync(IEnumerable<PathNode> paths, CancellationToken stoppingToken = default);
    }
}
