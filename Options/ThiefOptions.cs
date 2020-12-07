namespace Beholder.Options
{
    public class ThiefOptions
    {
        public int CheckInterval { get; set; } = 600;

        public int RefreshTakeCount { get; set; } = 1000;

        public int PreloadTakeCount { get; set; } = 200;
    }
}
