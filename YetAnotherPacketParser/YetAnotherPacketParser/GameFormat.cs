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
        /// The fewest tossups read in overtime before a tied game can end. Zero means there's no overtime, and a game
        /// tied after regulation ends in a tie.
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
        // picked it there. They're built once up front, and TryGetPreset hands out copies so callers can't change them.
        private static Dictionary<string, GameFormat> Presets { get; } =
            new Dictionary<string, GameFormat>(StringComparer.OrdinalIgnoreCase)
            {
                { "acf", CreateAcfFormat("ACF", []) },
                { "macf", CreateAcfFormat("mACF with powers", [new PowerMarker("(*)", 15)]) },
                {
                    "pace", new GameFormat()
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
            if (name != null && Presets.TryGetValue(name.Trim(), out GameFormat? preset))
            {
                format = preset.Clone();
                return true;
            }

            format = null;
            return false;
        }

        /// <summary>
        /// Checks that the format's values make sense, such as a neg value that isn't positive. Fields left as
        /// <c>null</c> are always valid, since a reader uses its own setting for them.
        /// </summary>
        /// <returns>A description of each problem with the format. The list is empty if the format is valid.</returns>
        public IReadOnlyList<string> Validate()
        {
            List<string> errors = new List<string>();
            if (this.RegulationTossupCount <= 0)
            {
                errors.Add($"regulationTossupCount must be positive, but it's {this.RegulationTossupCount}.");
            }

            if (this.MinimumOvertimeQuestionCount < 0)
            {
                errors.Add(
                    $"minimumOvertimeQuestionCount can't be negative, but it's {this.MinimumOvertimeQuestionCount}.");
            }

            if (this.NegValue > 0)
            {
                errors.Add($"negValue can't be positive, but it's {this.NegValue}.");
            }

            if (this.TimeoutsAllowed < 0)
            {
                errors.Add($"timeoutsAllowed can't be negative, but it's {this.TimeoutsAllowed}.");
            }

            if (this.Powers != null)
            {
                HashSet<string> markers = new HashSet<string>(StringComparer.Ordinal);
                foreach (PowerMarker? power in this.Powers)
                {
                    if (power == null || string.IsNullOrWhiteSpace(power.Marker))
                    {
                        errors.Add("Every power needs a marker.");
                        continue;
                    }

                    if (power.Points <= 0)
                    {
                        errors.Add($"The power marked \"{power.Marker}\" must be worth a positive number of points, " +
                            $"but it's worth {power.Points}.");
                    }

                    if (!markers.Add(power.Marker))
                    {
                        errors.Add($"The power marker \"{power.Marker}\" is listed more than once.");
                    }
                }
            }

            if (this.PronunciationGuideMarkers != null &&
                (this.PronunciationGuideMarkers.Count != 2 ||
                    this.PronunciationGuideMarkers.Any(marker => string.IsNullOrEmpty(marker))))
            {
                errors.Add("pronunciationGuideMarkers must have exactly two markers, the start and the end of a guide.");
            }

            return errors;
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

        // A copy that shares nothing changeable with this format, so changing the copy's powers can't change this one
        private GameFormat Clone()
        {
            GameFormat copy = (GameFormat)this.MemberwiseClone();
            copy.Powers = this.Powers?.Select(power => new PowerMarker(power.Marker, power.Points)).ToArray();
            copy.PronunciationGuideMarkers = this.PronunciationGuideMarkers?.ToArray();
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
