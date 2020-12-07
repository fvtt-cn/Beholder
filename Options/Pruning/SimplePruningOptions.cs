namespace Beholder.Options.Pruning
{
    public class SimplePruningOptions
    {
        public int MergeRequirement { get; set; } = 20;

        public string PreloadCheckRoot { get; set; } = string.Empty;
    }
}
