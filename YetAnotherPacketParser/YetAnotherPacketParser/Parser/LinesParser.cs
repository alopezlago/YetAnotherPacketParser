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

        public IResult<PacketNode> Parse(IEnumerable<Line> lines)
        {
            Verify.IsNotNull(lines, nameof(lines));

            using (LinesEnumerator enumerator = new LinesEnumerator(lines))
            {
                if (!enumerator.MoveNext())
                {
                    return new FailureResult<PacketNode>("Cannot parse empty packet.");
                }

                IResult<TossupsNode> tossupsResult = this.ParseTossups(enumerator, out bool moreLinesExist);
                if (!tossupsResult.Success)
                {
                    return new FailureResult<PacketNode>(tossupsResult.ErrorMessage);
                }

                if (!moreLinesExist)
                {
                    return new SuccessResult<PacketNode>(new PacketNode(tossupsResult.Value, bonuses: null));
                }

                IResult<BonusesNode> bonusesResult = this.ParseBonuses(enumerator);
                if (!bonusesResult.Success)
                {
                    return new FailureResult<PacketNode>(bonusesResult.ErrorMessage);
                }

                return new SuccessResult<PacketNode>(new PacketNode(tossupsResult.Value, bonusesResult.Value));
            }
        }

        private static bool IsEndOfLeadin(Line line)
        {
            return line.PartValue.HasValue;
        }

        private static bool IsEndOfQuestion(Line line)
        {
            return line.IsAnswerLine;
        }

        private static string GetFailureMessage(LinesEnumerator lines, string message)
        {
            StringBuilder snippet = new StringBuilder(10);
            if (lines.Current != null)
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

            string snippetMessage = snippet.Length > 0 ? $@", ""{snippet}""" : "";
            return $"{message} (Line #{lines.LineNumber}{snippetMessage})";
        }

        private static bool TryGetNextQuestionLine(LinesEnumerator lines, out Line? line)
        {
            // Skip lines until we get to the next question
            line = null;
            if (lines.Current == null)
            {
                return false;
            }

            do
            {
                if (lines.Current.Number.HasValue)
                {
                    line = lines.Current;
                    return true;
                }
            } while (lines.MoveNext());

            return false;
        }

        private IResult<TossupsNode> ParseTossups(LinesEnumerator lines, out bool moreLinesExist)
        {
            moreLinesExist = true;
            int currentQuestionNumber = -1;
            List<TossupNode> tossupNodes = new List<TossupNode>();

            // If the question number drops, it means we're in bonuses now, so stop parsing tossups
            Line? line;
            while (TryGetNextQuestionLine(lines, out line) &&
                line?.Number != null &&
                line.Number > currentQuestionNumber)
            {
                currentQuestionNumber = line.Number.Value;

                IResult<TossupNode> tossupResult = this.ParseTossup(lines, tossupNodes.Count + 1);
                if (!tossupResult.Success)
                {
                    return new FailureResult<TossupsNode>(tossupResult.ErrorMessage);
                }

                tossupNodes.Add(tossupResult.Value);
            }

            if (tossupNodes.Count == 0)
            {
                return new FailureResult<TossupsNode>(GetFailureMessage(
                    lines, "Failed to parse tossups. No tossups found."));
            }

            moreLinesExist = line != null;
            return new SuccessResult<TossupsNode>(new TossupsNode(tossupNodes));
        }

        private IResult<BonusesNode> ParseBonuses(LinesEnumerator lines)
        {
            List<BonusNode> bonusNodes = new List<BonusNode>();

            while (TryGetNextQuestionLine(lines, out _))
            {
                IResult<BonusNode> bonusResult = this.ParseBonus(lines, bonusNodes.Count + 1);
                if (!bonusResult.Success)
                {
                    return new FailureResult<BonusesNode>(bonusResult.ErrorMessage);
                }

                bonusNodes.Add(bonusResult.Value);
            }

            // It's okay if bonuses are empty, since bonuses may be optional
            return new SuccessResult<BonusesNode>(new BonusesNode(bonusNodes));
        }

        private IResult<TossupNode> ParseTossup(LinesEnumerator lines, int tossupNumber)
        {
            if (!lines.Current.Number.HasValue)
            {
                return new FailureResult<TossupNode>(GetFailureMessage(
                    lines, $"Failed to parse tossup #{tossupNumber}. No question number found."));
            }

            int questionNumber = lines.Current.Number.Value;
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
            if (!lines.Current.Number.HasValue)
            {
                return new FailureResult<BonusNode>(GetFailureMessage(
                    lines, $"Failed to parse bonus #{bonusNumber}. No question number found."));
            }

            int questionNumber = lines.Current.Number.Value;

            IResult<FormattedText> leadinResult = this.GetTextFromLines(
                lines, $"Bonus leadin (#{bonusNumber})", IsEndOfLeadin);
            if (!leadinResult.Success)
            {
                return new FailureResult<BonusNode>(leadinResult.ErrorMessage);
            }

            IResult<BonusPartsNode> bonusPartsResult = this.ParseBonusParts(lines);
            if (!bonusPartsResult.Success)
            {
                return new FailureResult<BonusNode>(bonusPartsResult.ErrorMessage);
            }

            return new SuccessResult<BonusNode>(new BonusNode(
                questionNumber, leadinResult.Value, bonusPartsResult.Value, null));
        }

        private IResult<BonusPartsNode> ParseBonusParts(LinesEnumerator lines)
        {
            List<BonusPartNode> parts = new List<BonusPartNode>();
            do
            {
                if (!lines.Current.PartValue.HasValue)
                {
                    // We're no longer on bonus parts, so get out of the loop
                    break;
                }

                IResult<BonusPartNode> bonusPartResult = this.ParseBonusPart(lines, parts.Count + 1);
                if (!bonusPartResult.Success)
                {
                    return new FailureResult<BonusPartsNode>(bonusPartResult.ErrorMessage);
                }

                parts.Add(bonusPartResult.Value);
            } while (lines.MoveNext());

            if (parts.Count == 0)
            {
                return new FailureResult<BonusPartsNode>(GetFailureMessage(
                    lines,
                    "Failed to parse bonus parts. Couldn't find the part's value in the first block of text."));
            }

            return new SuccessResult<BonusPartsNode>(new BonusPartsNode(parts));
        }

        private IResult<BonusPartNode> ParseBonusPart(LinesEnumerator lines, int bonusPartNumber)
        {
            // Should follow the format
            // [value] question
            // ANSWER: xxx
            if (!lines.Current.PartValue.HasValue)
            {
                return new FailureResult<BonusPartNode>(GetFailureMessage(
                     lines,
                     "Failed to parse bonus part. Couldn't find the part's value."));
            }

            int partValue = lines.Current.PartValue.Value;
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
            else if (lines.Current.Number.HasValue)
            {
                return new FailureResult<QuestionNode>(GetFailureMessage(
                    lines, $"Failed to parse {context}. Expected answer line starts with a question number in it."));
            }

            // We can't support multi-line answers, since the answer is the last part of the unit (tossup or bonus).
            // There maybe extra text (editor's notes, headers) that come after, and we don't have a good way of
            // deciding if it belongs to the answer or not
            FormattedText answer = lines.Current.Text;

            // TODO: Support editor's notes. Would require changing the lexer, or having some look-behind so we can
            // see if the previous line starts with an editor's note tag
            return new SuccessResult<QuestionNode>(new QuestionNode(questionResult.Value, answer));
        }

        private IResult<FormattedText> GetTextFromLines(LinesEnumerator lines, string context, Func<Line, bool> isEnd)
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
                        lines,
                        $"Failed to parse {context}. No more lines found. Number of lines searched for after the last part: {linesChecked + 1}"));
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
                string lineString = this.Options.MaximumLineCountBeforeNextStage == 1 ? "line" : "lines";
                return new FailureResult<FormattedText>(GetFailureMessage(
                    lines,
                    $"Failed to parse {context}. We couldn't find the next part after {this.Options.MaximumLineCountBeforeNextStage} {lineString}."));
            }

            if (linesChecked > 0)
            {
                // The question needs to be pieced together from different lines, so create a new FormattedText
                formattedText = new FormattedText(formattedTextSegments);
            }

            return new SuccessResult<FormattedText>(formattedText);
        }

        private class LinesEnumerator : IEnumerator<Line>
        {
            public LinesEnumerator(IEnumerable<Line> lines)
            {
                this.Enumerator = lines.GetEnumerator();
                this.LineNumber = 1;
            }

            public Line Current => this.Enumerator.Current;

            public int LineNumber { get; private set; }

            private IEnumerator<Line> Enumerator { get; }

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
