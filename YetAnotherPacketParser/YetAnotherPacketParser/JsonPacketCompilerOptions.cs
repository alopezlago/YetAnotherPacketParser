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
            this.ModaqFormat = false;
            this.PrettyPrint = true;
        }

        public string StreamName { get; set; }

        public OutputFormat OutputFormat => OutputFormat.Json;

        [Obsolete("No longer used")]
        public int MaximumLineCountBeforeNextStage { get; set; }

        public int MaximumPackets { get; set; }

        public int MaximumPacketSizeInBytes { get; set; }

        // Only emit the fields MODAQ uses, e.g. remove all the *_sanitized fields
        public bool ModaqFormat { get; set; }

        public bool PrettyPrint { get; set; }

        public Action<LogLevel, string>? Log { get; set; }
    }
}
