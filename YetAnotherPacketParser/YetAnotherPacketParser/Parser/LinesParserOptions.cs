﻿using System;

namespace YetAnotherPacketParser.Parser
{
    public class LinesParserOptions
    {
        public static readonly LinesParserOptions Default = new LinesParserOptions();

        public LinesParserOptions()
        {
        }

        /// <summary>
        /// The number of extra lines to search for the next stage/token (like ANSWER or a bonus part value)
        /// </summary>
        [Obsolete("This parameter is no longer used")]
        public int MaximumLineCountBeforeNextStage
        {
            get;
            set;
        }
    }
}
