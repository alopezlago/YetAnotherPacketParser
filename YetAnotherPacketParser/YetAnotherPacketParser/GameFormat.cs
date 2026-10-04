using System;
using System.Collections.Generic;
using System.Linq;

namespace YetAnotherPacketParser
{
    /// <summary>
    /// The rules of the game a packet is written for, such as how many tossups are in regulation and what powers are
    /// worth. This is written to yapp2 packets as the top-level "gameFormat" object, so a reader can set up the game
    /// from the packet instead of having the moderator customize it each time.
    /// </summary>
    /// <remarks>
    /// Every field is optional. A field left as <c>null</c> isn't written, and a reader falls back to its own setting
    /// for it. The field names match MODAQ's game format, so a reader like MODAQ can apply the format directly.
    /// </remarks>
    public class GameFormat
    {
        /// <summary>
        /// The name of the format, such as "ACF" or "PACE".
        /// </summary>
        public string? DisplayName { get; set; }

        /// <summary>
        /// The number of tossups in regulation, not counting tiebreakers.
        /// </summary>
        public int? RegulationTossupCount { get; set; }

        /// <summary>
        /// The fewest tossups read in overtime before a tied game can end.
        /// </summary>
        public int? MinimumOvertimeQuestionCount { get; set; }

        /// <summary>
        /// Whether a team that answers a tossup in overtime also gets a bonus.
        /// </summary>
        public bool? OvertimeIncludesBonuses { get; set; }

        /// <summary>
        /// The power markers in the tossups and what a buzz before each one is worth, highest first. An empty list
        /// means the format has no powers.
        /// </summary>
        public IReadOnlyList<PowerMarker>? Powers { get; set; }

        /// <summary>
        /// The points for an incorrect interrupt, such as -5. Zero means the format has no negs.
        /// </summary>
        public int? NegValue { get; set; }

        /// <summary>
        /// Whether the other team can answer the bonus parts the controlling team misses.
        /// </summary>
        public bool? BonusesBounceBack { get; set; }

        /// <summary>
        /// The number of timeouts each team gets.
        /// </summary>
        public int? TimeoutsAllowed { get; set; }

        /// <summary>
        /// Whether the format has no bonuses.
        /// </summary>
        public bool? TossupsOnly { get; set; }

        /// <summary>
        /// The text that starts and ends a pronunciation guide, such as <c>("</c> and <c>")</c>.
        /// </summary>
        public IReadOnlyList<string>? PronunciationGuideMarkers { get; set; }

        /// <summary>
        /// The names of the formats <see cref="TryGetPreset"/> knows.
        /// </summary>
        public static IEnumerable<string> PresetNames => Presets.Keys;

        // These mirror the formats MODAQ ships with, so a packet using one reads exactly as if the moderator had
        // picked it there
        private static Dictionary<string, Func<GameFormat>> Presets { get; } =
            new Dictionary<string, Func<GameFormat>>(StringComparer.OrdinalIgnoreCase)
            {
                { "acf", () => CreateAcfFormat("ACF", []) },
                { "macf", () => CreateAcfFormat("mACF with powers", [new PowerMarker("(*)", 15)]) },
                {
                    "pace", () => new GameFormat()
                    {
                        DisplayName = "PACE",
                        RegulationTossupCount = 20,
                        MinimumOvertimeQuestionCount = 1,
                        OvertimeIncludesBonuses = false,
                        Powers = [new PowerMarker("(*)", 20)],
                        NegValue = 0,
                        BonusesBounceBack = false,
                        TimeoutsAllowed = 1,
                        TossupsOnly = false,
                        PronunciationGuideMarkers = ["(\"", "\")"]
                    }
                }
            };

        /// <summary>
        /// Gets a common format by name. The names are listed in <see cref="PresetNames"/>.
        /// </summary>
        /// <param name="name">The name of the format, ignoring case.</param>
        /// <param name="format">A new copy of the format, which can be changed freely, or <c>null</c> if no format
        /// has that name.</param>
        /// <returns><c>true</c> if a format has that name.</returns>
        public static bool TryGetPreset(string name, out GameFormat? format)
        {
            if (name != null && Presets.TryGetValue(name.Trim(), out Func<GameFormat>? createFormat))
            {
                format = createFormat();
                return true;
            }

            format = null;
            return false;
        }

        /// <summary>
        /// A copy of this format with its powers in the order the yapp2 format requires, highest value first.
        /// </summary>
        internal GameFormat WithPowersInDescendingOrder()
        {
            GameFormat copy = (GameFormat)this.MemberwiseClone();
            copy.Powers = this.Powers?.OrderByDescending(power => power.Points).ToArray();
            return copy;
        }

        private static GameFormat CreateAcfFormat(string displayName, IReadOnlyList<PowerMarker> powers)
        {
            return new GameFormat()
            {
                DisplayName = displayName,
                RegulationTossupCount = 20,
                MinimumOvertimeQuestionCount = 1,
                OvertimeIncludesBonuses = false,
                Powers = powers,
                NegValue = -5,
                BonusesBounceBack = false,
                TimeoutsAllowed = 1,
                TossupsOnly = false,
                PronunciationGuideMarkers = ["(\"", "\")"]
            };
        }
    }

    /// <summary>
    /// A power marker and what a correct buzz before it is worth.
    /// </summary>
    public class PowerMarker
    {
        public PowerMarker()
        {
            this.Marker = string.Empty;
        }

        public PowerMarker(string marker, int points)
        {
            this.Marker = marker;
            this.Points = points;
        }

        /// <summary>
        /// The literal text marking the end of the power region in a tossup, such as <c>(*)</c>.
        /// </summary>
        public string Marker { get; set; }

        /// <summary>
        /// What a correct buzz before the marker is worth, such as 15.
        /// </summary>
        public int Points { get; set; }
    }
}
