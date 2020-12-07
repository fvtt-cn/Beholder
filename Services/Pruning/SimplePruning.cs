using System.IO;
using System.Linq;
using Beholder.Models.PathTree;
using Beholder.Options.Pruning;
using Microsoft.Extensions.Logging;

namespace Beholder.Services.Pruning
{
    public class SimplePruning : IPruning
    {
        private readonly ILogger logger;
        private readonly SimplePruningOptions options;

        public SimplePruning(SimplePruningOptions options, ILogger<SimplePruning> logger)
        {
            this.options = options;

            this.logger = logger;
        }

        public void PrunePathTree(Node<PathNode> refresh, Node<PathNode> preload)
        {
            var refreshList = refresh.SelfAndDescendants.ToList();

            // Remove files that unable to preload.
            if (!string.IsNullOrEmpty(options.PreloadCheckRoot))
            {
                foreach (var node in preload.SelfAndDescendants.Where(node => !node.Value.IsDirectory).ToList())
                {
                    // C...C => Preload.
                    // C...D => Neither.
                    // D...D => Refresh.
                    // D...C => Both.
                    if (node.Value.JustCreated)
                    {
                        var refreshNode = refreshList.FirstOrDefault(x => x.Value.Path == node.Value.Path);
                        if (refreshNode is not null)
                        {
                            logger.LogInformation("Prune just created ready-to-refresh file at: {path}",
                                node.Value.Path);
                            refreshNode.Disconnect();
                        }
                    }

                    if (!File.Exists(Path.Combine(options.PreloadCheckRoot, node.Value.Path)))
                    {
                        logger.LogInformation("Prune ready-to-preload file at: {path}", node.Value.Path);
                        node.Disconnect();
                    }
                }
            }

            foreach (var node in refreshList)
            {
                // If Children > MergeRequirement, update the folder instead.
                node.Value.WillForceUpdate = node.Value.WillForceUpdate ||
                                             node.Value.IsDirectory &&
                                             node.Children.Count() >= options.MergeRequirement;

                // The folder will refresh, disconnect children (GC Stressing).
                if (node.Value.WillForceUpdate && node.Value.IsDirectory)
                {
                    logger.LogInformation("Prune folder children at: {path}", node.Value.Path);

                    foreach (var nodeChild in node.Children.ToList())
                    {
                        nodeChild.Disconnect();
                    }
                }
            }
        }
    }
}
