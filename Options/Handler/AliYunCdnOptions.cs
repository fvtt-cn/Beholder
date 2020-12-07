namespace Beholder.Options.Handler
{
    public class AliYunCdnOptions
    {
        public string Prefix { get; set; } = string.Empty;

        public string RegionId { get; set; } = "cn-hangzhou";

        public string AccessKeyId { get; set; } = string.Empty;

        public string Secret { get; set; } = string.Empty;

        public int MaxRetry { get; set; } = 5;

        public int TotalTimeout { get; set; } = 30;
    }
}
