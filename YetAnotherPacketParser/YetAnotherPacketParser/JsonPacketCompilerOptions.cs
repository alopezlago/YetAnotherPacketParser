using System;

namespace YetAnotherPacketParser
{
    public class JsonPacketCompilerOptions : IPacketConverterOptions
    {
        public JsonPacketCompilerOptions()
        {
            this.StreamName = string.Empty;
            this.MaximumPackets = 1000;
            this.MaximumPacketSizeInBytes = 1 * 1024 * 1024; // 1 MB
            this.PrettyPrint = true;
        }

        public string StreamName { get; set; }

        public OutputFormat OutputFormat => OutputFormat.Json;

        [Obsolete("No longer used")]
        public int MaximumLineCountBeforeNextStage { get; set; }

        public int MaximumPackets { get; set; }

        public int MaximumPacketSizeInBytes { get; set; }

        public bool PrettyPrint { get; set; }

        public Action<LogLevel, string>? Log { get; set; }
    }
}
