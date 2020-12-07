using Beholder.Models.PathTree;

namespace Beholder.Services.Pruning
{
    public interface IPruning
    {
        void PrunePathTree(Node<PathNode> refresh, Node<PathNode> preload);
    }
}
