using System;

namespace YetAnotherPacketParser
{
    public interface IPacketConverterOptions
    {
        public string StreamName { get; }

        public OutputFormat OutputFormat { get; }

        public int MaximumLineCountBeforeNextStage { get; }

        public int MaximumPackets { get; }

        public int MaximumPacketSizeInBytes { get; }

        public bool PrettyPrint { get; }

        public Action<LogLevel, string>? Log { get; }
    }
}
