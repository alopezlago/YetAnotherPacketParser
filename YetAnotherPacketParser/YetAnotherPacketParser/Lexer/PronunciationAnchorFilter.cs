using System.Collections.Generic;
using System.Text;

namespace YetAnotherPacketParser.Lexer
{
    /// <summary>
    /// Decides which colored runs in a document are pronunciation-guide anchors, i.e. the word(s) that a guide
    /// covers.
    /// </summary>
    /// <remarks>
    /// Authoring tools color the anchored words so a writer can see what a guide is attached to, e.g. Denis
    /// <em>Diderot</em> ("DID-er-OW") with "Diderot" tinted. Color alone can't be the signal, though, because the same
    /// documents color other things too - the guide itself is usually set in gray - so a colored run only counts as an
    /// anchor when a guide immediately follows it.
    /// </remarks>
    internal static class PronunciationAnchorFilter
    {
        // How far past the anchor the guide's "(" may start. Enough room for a closing quotation mark, a comma, or a
        // possessive ("Diderot's ("DID-er-OW")"), and not enough to reach a parenthetical in the next clause.
        private const int MaximumCharactersBeforeGuide = 3;

        /// <summary>
        /// Clears the pronunciation anchor flag on colored text that no guide follows.
        /// </summary>
        /// <param name="segments">The segments of a single line, in order.</param>
        /// <returns>The same segments, with the flag left only on runs a guide follows. The list passed in is
        /// returned unchanged when every anchor is genuine, which is the common case.</returns>
        public static List<FormattedTextSegment> RemoveAnchorsWithoutGuides(List<FormattedTextSegment> segments)
        {
            Verify.IsNotNull(segments, nameof(segments));

            // Almost every line has no colored text at all, so don't build anything until we know there is
            bool anyAnchors = false;
            foreach (FormattedTextSegment segment in segments)
            {
                if (segment.IsPronunciationAnchor)
                {
                    anyAnchors = true;
                    break;
                }
            }

            if (!anyAnchors)
            {
                return segments;
            }

            StringBuilder lineBuilder = new StringBuilder();
            foreach (FormattedTextSegment segment in segments)
            {
                lineBuilder.Append(segment.Text);
            }

            string line = lineBuilder.ToString();

            // Consecutive anchored segments are one anchor (the writer may have bolded part of it, which splits the
            // run in two), so the guide has to be looked for after the last of them.
            List<FormattedTextSegment> filteredSegments = new List<FormattedTextSegment>(segments.Count);
            int index = 0;
            int segmentIndex = 0;
            while (segmentIndex < segments.Count)
            {
                FormattedTextSegment segment = segments[segmentIndex];
                if (!segment.IsPronunciationAnchor)
                {
                    filteredSegments.Add(segment);
                    index += segment.Text.Length;
                    segmentIndex++;
                    continue;
                }

                int anchorStart = index;
                int anchorEnd = index;
                int lastSegmentIndex = segmentIndex;
                while (lastSegmentIndex < segments.Count && segments[lastSegmentIndex].IsPronunciationAnchor)
                {
                    anchorEnd += segments[lastSegmentIndex].Text.Length;
                    lastSegmentIndex++;
                }

                bool isAnchor = IsAnchorText(line, anchorStart, anchorEnd) && HasGuideAfter(line, anchorEnd);
                for (; segmentIndex < lastSegmentIndex; segmentIndex++)
                {
                    FormattedTextSegment anchorSegment = segments[segmentIndex];
                    filteredSegments.Add(isAnchor ?
                        anchorSegment :
                        new FormattedTextSegment(
                            anchorSegment.Text,
                            anchorSegment.Italic,
                            anchorSegment.Bolded,
                            anchorSegment.Underlined,
                            anchorSegment.IsSubscript,
                            anchorSegment.IsSuperscript,
                            isPronunciationAnchor: false));
                }

                index = anchorEnd;
            }

            return filteredSegments;
        }

        /// <summary>
        /// Whether the colored run could be anchored words at all, as opposed to a guide or a stretch of colored
        /// punctuation.
        /// </summary>
        private static bool IsAnchorText(string line, int start, int end)
        {
            bool hasLetterOrDigit = false;
            for (int i = start; i < end; i++)
            {
                char character = line[i];

                // A colored run that opens with a parenthesis is the guide itself, not what it covers
                if (character == '(')
                {
                    return false;
                }

                if (char.IsLetterOrDigit(character))
                {
                    hasLetterOrDigit = true;
                }
            }

            return hasLetterOrDigit;
        }

        /// <summary>
        /// Whether a pronunciation guide starts right after the given index.
        /// </summary>
        private static bool HasGuideAfter(string line, int index)
        {
            int limit = index + MaximumCharactersBeforeGuide;
            for (int i = index; i <= limit && i < line.Length; i++)
            {
                char character = line[i];
                if (character == '(')
                {
                    return IsGuide(line, i);
                }

                // A parenthesis that closes before one opens means we started inside a parenthetical, so whatever
                // follows belongs to it rather than being a guide for this text
                if (character == ')')
                {
                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Whether the parenthetical starting at <paramref name="openParenIndex"/> reads like a pronunciation guide.
        /// </summary>
        private static bool IsGuide(string line, int openParenIndex)
        {
            for (int i = openParenIndex + 1; i < line.Length; i++)
            {
                char character = line[i];
                if (character == ')')
                {
                    return false;
                }

                // Power markers - "(*)" and "(+)" - are the parentheticals most likely to follow a word, and they
                // have no letters in them. Anything with a letter is treated as a respelling.
                if (char.IsLetter(character))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
