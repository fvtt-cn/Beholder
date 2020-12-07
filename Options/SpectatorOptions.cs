using System;

namespace Beholder.Options
{
    public class SpectatorOptions
    {
        /// <summary>
        ///     In KBytes.
        /// </summary>
        public int BufferSize { get; set; } = 256;

        public string Directory { get; set; } = ".";

        public string TrimStart { get; set; } = string.Empty;

        public string[] ExcludeExtensions { get; set; } = Array.Empty<string>();

        public string[] ExcludePaths { get; set; } = Array.Empty<string>();

        public bool PreloadEnabled { get; set; }

        public bool OnCreated { get; set; }

        public bool OnChanged { get; set; }

        public bool OnDeleted { get; set; }

        public bool OnRenamed { get; set; }
    }
}
