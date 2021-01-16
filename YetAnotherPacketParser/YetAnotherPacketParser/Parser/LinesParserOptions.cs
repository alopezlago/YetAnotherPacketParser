using System;

namespace YetAnotherPacketParser.Parser
{
    public class LinesParserOptions
    {
        public static readonly LinesParserOptions Default = new LinesParserOptions();

        private int maximumLineCountBeforeNextStage;

        public LinesParserOptions()
        {
            this.MaximumLineCountBeforeNextStage = 1;
        }

        /// <summary>
        /// The number of extra lines to search for the next stage/token (like ANSWER or a bonus part value)
        /// </summary>
        public int MaximumLineCountBeforeNextStage
        {
            get => this.maximumLineCountBeforeNextStage;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), Strings.ValueMustBeGreaterThanZero);
                }

                this.maximumLineCountBeforeNextStage = value;
            }
        }
    }
}
