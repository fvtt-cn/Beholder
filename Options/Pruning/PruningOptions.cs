namespace Beholder.Options.Pruning
{
    public class PruningOptions
    {
        public string Method { get; set; } = nameof(SimplePruning);

        public SimplePruningOptions SimplePruning { get; set; } = new SimplePruningOptions();
    }
}
