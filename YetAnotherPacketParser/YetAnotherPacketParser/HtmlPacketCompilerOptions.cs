﻿using System;

namespace YetAnotherPacketParser
{
    public class HtmlPacketCompilerOptions : IPacketConverterOptions
    {
        public HtmlPacketCompilerOptions()
        {
            this.StreamName = string.Empty;
            this.MaximumPackets = 1000;
            this.MaximumPacketSizeInBytes = 1 * 1024 * 1024; // 1 MB
        }

        public string StreamName { get; set; }

        public OutputFormat OutputFormat => OutputFormat.Html;

        [Obsolete("No longer used")]
        public int MaximumLineCountBeforeNextStage { get; set; }

        public int MaximumPackets { get; set; }

        public int MaximumPacketSizeInBytes { get; set; }

        public bool PrettyPrint => throw new NotSupportedException();

        public Action<LogLevel, string>? Log { get; set; }
    }
}
