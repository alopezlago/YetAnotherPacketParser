using System;
using System.Text.RegularExpressions;

namespace YetAnotherPacketParser.Lexer
{
    internal static class LexerClassifier
    {
        private const int DefaultBonusPartValue = 10;
        // Include spaces after the start tag so we get all of the spaces in a match, and we can avoid having to trim
        // them manually.
        private static readonly Regex AnswerRegEx = new Regex(
            "^\\s*ANS(WER)?\\s*(:|\\.)\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex QuestionDigitRegEx = new Regex(
            "^\\s*(\\d+|tb|tie(breaker)?)\\s*\\.\\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex BonusPartValueRegex = new Regex(
            "^\\s*\\[(\\s*(\\d)+\\s*[ehm]?\\s*|\\s*[ehm]\\s*)\\]\\s*", RegexOptions.Compiled);
        private static readonly Regex PostQuestionMetadataRegex = new Regex(
            "^\\s*<(\\w|\\d|\\s|-|:|,)+(,(\\w|\\d|\\s|-|:|,)+)?>\\s*", RegexOptions.Compiled);

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
            if (int.TryParse(match.Value.Replace(".", string.Empty, StringComparison.Ordinal), out int parsedNumber))
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
            string partValueText = match.Value
                .Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal)
                .Trim();

            // If there's a difficulty modifier at the last character, include it. It's optional.
            char lastLetter = partValueText[^1];
            if (char.IsLetter(lastLetter))
            {
                difficultyModifier = lastLetter;
                partValueText = partValueText.Substring(0, partValueText.Length - 1);
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
