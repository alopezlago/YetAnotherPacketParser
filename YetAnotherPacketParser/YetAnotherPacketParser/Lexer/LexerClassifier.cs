using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace YetAnotherPacketParser.Lexer
{
    internal static class LexerClassifier
    {
        private const int DefaultBonusPartValue = 10;
        private const string BonusValueGroupName = "value";

        // Include spaces after the start tag so we get all of the spaces in a match, and we can avoid having to trim
        // them manually.
        private static readonly Regex AnswerRegEx = new Regex(
            "^\\s*ANS(WER)?\\s*(:|\\.)\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex QuestionDigitRegEx = new Regex(
            "^\\s*(\\d+|tb|tie(breaker)?)\\s*\\.\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BonusPartValueRegex = new Regex(
            "^\\s*\\[(?<value>\\s*(\\d+\\s*[ehm]?\\s*|[ehm]\\s*))\\]\\s*",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
        private static readonly Regex PostQuestionMetadataRegex = new Regex(
            "^\\s*<[^<>]+>\\s*", RegexOptions.Compiled);

        public static bool TextStartsWithQuestionDigit(string text, out string matchValue, out int? number)
        {
            number = null;
            Match match = QuestionDigitRegEx.Match(text);
            if (!match.Success)
            {
                matchValue = string.Empty;
                number = null;
                return false;
            }

            matchValue = match.Value;
            // Use the captured group for the numeric part (group 1) to avoid allocating via Replace
            string numericPart = match.Groups[1].Value;
            if (int.TryParse(numericPart, out int parsedNumber))
            {
                // We could be at a tiebreaker, so don't fail if we can't find the number
                number = parsedNumber;
            }

            return true;
        }

        public static bool TextStartsWithAnswer(string text, out string matchValue)
        {
            Match match = AnswerRegEx.Match(text);
            if (!match.Success)
            {
                matchValue = string.Empty;
                return false;
            }

            matchValue = match.Value;
            return true;
        }

        public static bool TextStartsWithBonsuPart(
            string text, out string matchValue, out int? partValue, out char? difficultyModifier)
        {
            partValue = null;
            difficultyModifier = null;
            Match match = BonusPartValueRegex.Match(text);
            if (!match.Success)
            {
                matchValue = string.Empty;
                return false;
            }

            matchValue = match.Value;
            ReadOnlySpan<char> partValueText = match.Groups[BonusValueGroupName].Value.AsSpan().Trim();

            // If there's a difficulty modifier at the last character, include it. It's optional.
            if (partValueText.Length > 0)
            {
                char lastLetter = partValueText[^1];
                if (char.IsLetter(lastLetter))
                {
                    difficultyModifier = char.ToLower(lastLetter, CultureInfo.InvariantCulture);
                    partValueText = partValueText.Slice(0, partValueText.Length - 1).TrimEnd();
                }
            }

            if (partValueText.Length == 0)
            {
                partValue = DefaultBonusPartValue;
            }
            else if (int.TryParse(partValueText, out int value))
            {
                partValue = value;
            }
            else
            {
                return false;
            }

            return true;
        }

        public static bool TextStartsWithPostQuestionMetadata(string text)
        {
            return PostQuestionMetadataRegex.Match(text).Success;
        }
    }
}
