using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YetAnotherPacketParser.Ast;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParser.Parser
{
    public class LinesParser : IParser
    {
        private const int FailureSnippetCharacterLimit = 40;

        public LinesParser(LinesParserOptions? options = null)
        {
            if (options == null)
            {
                options = LinesParserOptions.Default;
            }

            this.Options = options;
        }

        public LinesParserOptions Options { get; }

        /// <summary>
        /// Converts the list of lines into an abstract syntax tree, with the PacketNode as a root.
        /// </summary>
        /// <param name="lines">Lines of the packet to parse</param>
        /// <returns>If parsing was successful, a PacketNode representing the structure of the packet. Otherwise, an
        /// error message explaining what went wrong.</returns>
        public IResult<PacketNode> Parse(IEnumerable<ILine> lines)
        {
            Verify.IsNotNull(lines, nameof(lines));

            using (LinesEnumerator enumerator = new LinesEnumerator(lines))
            {
                if (!enumerator.MoveNext())
                {
                    return new FailureResult<PacketNode>(Strings.CannotParseEmptyPacket);
                }

                IResult<List<TossupNode>> tossupsResult = this.ParseTossups(enumerator, out bool moreLinesExist);
                if (!tossupsResult.Success)
                {
                    return new FailureResult<PacketNode>(tossupsResult.ErrorMessage);
                }

                if (!moreLinesExist)
                {
                    return new SuccessResult<PacketNode>(new PacketNode(tossupsResult.Value, bonuses: null));
                }

                IResult<List<BonusNode>> bonusesResult = this.ParseBonuses(enumerator);
                if (!bonusesResult.Success)
                {
                    return new FailureResult<PacketNode>(bonusesResult.ErrorMessage);
                }

                return new SuccessResult<PacketNode>(new PacketNode(tossupsResult.Value, bonusesResult.Value));
            }
        }

        private static bool IsEndOfLeadin(ILine line)
        {
            return line.Type == LineType.BonusPart;
        }

        private static bool IsEndOfQuestion(ILine line)
        {
            return line.Type == LineType.Answer;
        }

        private static string GetFailureMessage(LinesEnumerator lines, string message)
        {
            StringBuilder snippet = new StringBuilder(10);
            ILine? currentLine = null;
            try
            {
                currentLine = lines.Current;
            }
            catch (InvalidOperationException)
            {
                // We're at the end. No more lines
            }

            if (currentLine != null)
            {
                int remainingLength = FailureSnippetCharacterLimit;
                foreach (FormattedTextSegment segment in lines.Current.Text.Segments)
                {
                    if (segment.Text.Length <= remainingLength)
                    {
                        snippet.Append(segment.Text);
                    }
                    else
                    {
                        snippet.Append(segment.Text.Substring(0, remainingLength));
                    }

                    remainingLength = checked(FailureSnippetCharacterLimit - snippet.Length);
                    if (remainingLength < 0)
                    {
                        break;
                    }
                }
            }

            return Strings.ParseFailureMessage(message, lines.LineNumber, snippet.ToString());
        }

        private static bool TryGetNextQuestionLine(LinesEnumerator lines, out NumberedQuestionLine? line)
        {
            // Skip lines until we get to the next question
            line = null;
            try
            {
                // In certain cases (split bonus part line), we'll seek out a new line even if we've reached the end.
                // Instead of failing, report that we can't get the next question line.
                if (lines.Current == null)
                {
                    return false;
                }
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            do
            {
                if (lines.Current.Type == LineType.NumberedQuestion &&
                    lines.Current is NumberedQuestionLine numberedLine)
                {
                    line = numberedLine;
                    return true;
                }
            } while (lines.MoveNext());

            return false;
        }

        private static bool TryGetQuestionNumber(ILine line, out int questionNumber)
        {
            questionNumber = 0;
            if (!(line.Type == LineType.NumberedQuestion && line is NumberedQuestionLine numberedLine))
            {
                return false;
            }

            questionNumber = numberedLine.Number;
            return true;
        }

        private IResult<List<TossupNode>> ParseTossups(LinesEnumerator lines, out bool moreLinesExist)
        {
            moreLinesExist = true;
            int currentQuestionNumber = -1;
            List<TossupNode> tossupNodes = new List<TossupNode>();

            // If the question number drops, it means we're in bonuses now, so stop parsing tossups
            NumberedQuestionLine? line;
            while (TryGetNextQuestionLine(lines, out line) && line != null && line.Number > currentQuestionNumber)
            {
                currentQuestionNumber = line.Number;

                IResult<TossupNode> tossupResult = this.ParseTossup(lines, tossupNodes.Count + 1);
                if (!tossupResult.Success)
                {
                    return new FailureResult<List<TossupNode>>(tossupResult.ErrorMessage);
                }

                tossupNodes.Add(tossupResult.Value);
            }

            if (tossupNodes.Count == 0)
            {
                return new FailureResult<List<TossupNode>>(GetFailureMessage(lines, Strings.NoTossupsFound));
            }

            moreLinesExist = line != null;
            return new SuccessResult<List<TossupNode>>(tossupNodes);
        }

        private IResult<List<BonusNode>> ParseBonuses(LinesEnumerator lines)
        {
            List<BonusNode> bonusNodes = new List<BonusNode>();

            while (TryGetNextQuestionLine(lines, out _))
            {
                IResult<BonusNode> bonusResult = this.ParseBonus(lines, bonusNodes.Count + 1);
                if (!bonusResult.Success)
                {
                    return new FailureResult<List<BonusNode>>(bonusResult.ErrorMessage);
                }

                bonusNodes.Add(bonusResult.Value);
            }

            // It's okay if bonuses are empty, since bonuses may be optional
            return new SuccessResult<List<BonusNode>>(bonusNodes);
        }

        private IResult<TossupNode> ParseTossup(LinesEnumerator lines, int tossupNumber)
        {
            if (!TryGetQuestionNumber(lines.Current, out int questionNumber))
            {
                return new FailureResult<TossupNode>(GetFailureMessage(
                    lines, Strings.NoTossupQuestionNumberFound(tossupNumber)));
            }

            IResult<QuestionNode> questionResult = this.ParseQuestion(lines, $"tossup #{tossupNumber}");
            if (!questionResult.Success)
            {
                return new FailureResult<TossupNode>(questionResult.ErrorMessage);
            }

            // TODO: Support editor's notes. Would require changing the lexer, or having some look-behind so we can
            // see if the previous line starts with an editor's note tag
            return new SuccessResult<TossupNode>(new TossupNode(questionNumber, questionResult.Value));
        }

        private IResult<BonusNode> ParseBonus(LinesEnumerator lines, int bonusNumber)
        {
            if (!TryGetQuestionNumber(lines.Current, out int questionNumber))
            {
                return new FailureResult<BonusNode>(GetFailureMessage(
                    lines, Strings.NoBonusQuestionNumberFound(bonusNumber)));
            }

            IResult<FormattedText> leadinResult = this.GetTextFromLines(
                lines, $"Bonus leadin (#{bonusNumber})", IsEndOfLeadin);
            if (!leadinResult.Success)
            {
                return new FailureResult<BonusNode>(leadinResult.ErrorMessage);
            }

            IResult<List<BonusPartNode>> bonusPartsResult = this.ParseBonusParts(lines);
            if (!bonusPartsResult.Success)
            {
                return new FailureResult<BonusNode>(bonusPartsResult.ErrorMessage);
            }

            return new SuccessResult<BonusNode>(new BonusNode(
                questionNumber, leadinResult.Value, bonusPartsResult.Value, null));
        }

        private IResult<List<BonusPartNode>> ParseBonusParts(LinesEnumerator lines)
        {
            List<BonusPartNode> parts = new List<BonusPartNode>();
            do
            {
                if (lines.Current.Type != LineType.BonusPart)
                {
                    // We're no longer on bonus parts, so get out of the loop
                    break;
                }

                IResult<BonusPartNode> bonusPartResult = this.ParseBonusPart(lines, parts.Count + 1);
                if (!bonusPartResult.Success)
                {
                    return new FailureResult<List<BonusPartNode>>(bonusPartResult.ErrorMessage);
                }

                parts.Add(bonusPartResult.Value);
            } while (lines.MoveNext());

            if (parts.Count == 0)
            {
                return new FailureResult<List<BonusPartNode>>(GetFailureMessage(
                    lines, Strings.CouldntFindBonusPartValueInFirstBlock));
            }

            return new SuccessResult<List<BonusPartNode>>(parts);
        }

        private IResult<BonusPartNode> ParseBonusPart(LinesEnumerator lines, int bonusPartNumber)
        {
            // Should follow the format
            // [value] question
            // ANSWER: xxx
            if (!(lines.Current.Type == LineType.BonusPart && lines.Current is BonusPartLine bonusPartLine))
            {
                return new FailureResult<BonusPartNode>(GetFailureMessage(
                     lines, Strings.CouldntFindBonusPartValue));
            }

            int partValue = bonusPartLine.Value;
            IResult<QuestionNode> questionResult = this.ParseQuestion(lines, $"bonus part #{bonusPartNumber}");
            if (!questionResult.Success)
            {
                return new FailureResult<BonusPartNode>(questionResult.ErrorMessage);
            }

            return new SuccessResult<BonusPartNode>(new BonusPartNode(questionResult.Value, partValue));
        }

        private IResult<QuestionNode> ParseQuestion(LinesEnumerator lines, string context)
        {
            IResult<FormattedText> questionResult = this.GetTextFromLines(lines, context, IsEndOfQuestion);
            if (!questionResult.Success)
            {
                return new FailureResult<QuestionNode>(questionResult.ErrorMessage);
            }
            else if (lines.Current.Type != LineType.Answer)
            {
                return new FailureResult<QuestionNode>(GetFailureMessage(
                    lines, Strings.UnknownLineTypeforAnswer(context, lines.Current.Type)));
            }

            // We can't support multi-line answers, since the answer is the last part of the unit (tossup or bonus).
            // There maybe extra text (editor's notes, headers) that come after, and we don't have a good way of
            // deciding if it belongs to the answer or not
            FormattedText answer = lines.Current.Text;

            // TODO: Support editor's notes. Would require changing the lexer, or having some look-behind so we can
            // see if the previous line starts with an editor's note tag
            return new SuccessResult<QuestionNode>(new QuestionNode(questionResult.Value, answer));
        }

        private IResult<FormattedText> GetTextFromLines(LinesEnumerator lines, string context, Func<ILine, bool> isEnd)
        {
            FormattedText formattedText = lines.Current.Text;
            IEnumerable<FormattedTextSegment> formattedTextSegments = formattedText.Segments;

            // The question number + question stays the same. We should do this in a loop (move next, is answer line, etc.)
            int linesChecked = 0;
            while (linesChecked < this.Options.MaximumLineCountBeforeNextStage)
            {
                if (!lines.MoveNext())
                {
                    return new FailureResult<FormattedText>(GetFailureMessage(
                        lines, Strings.NoMoreLinesFound(context, linesChecked + 1)));
                }
                else if (isEnd(lines.Current))
                {
                    break;
                }

                // Keep adding to the question text, since it's not done
                formattedTextSegments = formattedTextSegments.Concat(lines.Current.Text.Segments);
                linesChecked++;
            }

            if (linesChecked >= this.Options.MaximumLineCountBeforeNextStage)
            {
                return new FailureResult<FormattedText>(GetFailureMessage(
                    lines, Strings.CouldntFindNextPart(context, this.Options.MaximumLineCountBeforeNextStage)));
            }

            if (linesChecked > 0)
            {
                // The question needs to be pieced together from different lines, so create a new FormattedText
                formattedText = new FormattedText(formattedTextSegments);
            }

            return new SuccessResult<FormattedText>(formattedText);
        }

        private class LinesEnumerator : IEnumerator<ILine>
        {
            public LinesEnumerator(IEnumerable<ILine> lines)
            {
                this.Enumerator = lines.GetEnumerator();
                this.LineNumber = 1;
            }

            public ILine Current => this.Enumerator.Current;

            public int LineNumber { get; private set; }

            private IEnumerator<ILine> Enumerator { get; }

            object? IEnumerator.Current => this.Enumerator.Current;

            public void Dispose()
            {
                this.Enumerator.Dispose();
            }

            public bool MoveNext()
            {
                bool moveNext = this.Enumerator.MoveNext();
                if (moveNext)
                {
                    this.LineNumber++;
                }

                return moveNext;
            }

            public void Reset()
            {
                this.Enumerator.Reset();
                this.LineNumber = 1;
            }
        }
    }
}