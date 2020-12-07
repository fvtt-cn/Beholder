using System;
using Beholder.Services.Handler;

namespace Beholder.Options.Handler
{
    public class HandlerOptions
    {
        public string Method { get; set; } =
            nameof(ConsoleLogHandler).Replace("Handler", string.Empty, StringComparison.OrdinalIgnoreCase);

        public ConsoleLogOptions ConsoleLog { get; set; } = new ConsoleLogOptions();

        public AliYunCdnOptions AliYunCdn { get; set; } = new AliYunCdnOptions();
    }
}
