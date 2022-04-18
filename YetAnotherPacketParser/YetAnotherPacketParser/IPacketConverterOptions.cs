using System;

namespace YetAnotherPacketParser
{
    public interface IPacketConverterOptions
    {
        /// <summary>
        /// The name of the input packet. This is often the filename of the packet.
        /// </summary>
        public string StreamName { get; }

        /// <summary>
        /// The format of the output string after conversion, such as JSON or HTML.
        /// </summary>
        public OutputFormat OutputFormat { get; }

        [Obsolete("No longer used")]
        public int MaximumLineCountBeforeNextStage { get; }

        /// <summary>
        /// The maximum number of packets the converter is allowed to convert. If the converter is required to convert more packets than this
        /// value, the conversion will fail.
        /// </summary>
        public int MaximumPackets { get; }

        /// <summary>
        /// The largest size an input packet can be in bytes. If the converter sees a packet larger than this value,
        /// then the conversion fails.
        /// </summary>
        public int MaximumPacketSizeInBytes { get; }

        /// <summary>
        /// When <c>true</c>, don't include sanitized fields in the output string.
        /// </summary>
        public bool ModaqFormat { get; }

        /// <summary>
        /// When <c>true</c>, pretty prints the output.
        /// </summary>
        public bool PrettyPrint { get; }

        /// <summary>
        /// Callback for logs during the conversion process.
        /// </summary>
        public Action<LogLevel, string>? Log { get; }
    }
}
