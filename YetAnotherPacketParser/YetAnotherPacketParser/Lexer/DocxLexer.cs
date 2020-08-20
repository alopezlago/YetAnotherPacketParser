using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace YetAnotherPacketParser.Lexer
{
    // An unconventional lexer: grab information like question number and text from newlines. Since <w:br> is interpretted
    // as a newline but not as a paragraph, we have to enumerate through all the paragraphs to find them.
    public class DocxLexer : ILexer
    {
        // No part should be greater than 2 million characters
        private const int MaximumCharactersInPart = 2 * 1024 * 1024;
        private static readonly OpenSettings DocOpenSettings = new OpenSettings()
        {
            MaxCharactersInPart = MaximumCharactersInPart
        };

        // Include spaces after the start tag so we get all of the spaces in a match, and we can avoid having to trim
        // them manually.
        private static readonly Regex AnswerRegEx = new Regex("^\\s*ANS(WER)?\\s*(:|\\.)\\s*", RegexOptions.IgnoreCase);
        private static readonly Regex QuestionDigitRegEx = new Regex("^\\s*(\\d+|tb|tie(breaker)?)\\s*\\.\\s*", RegexOptions.IgnoreCase);
        private static readonly Regex BonusPartValueRegex = new Regex("^\\s*\\[\\s*(\\d)+\\s*\\]\\s*");

        public Task<IResult<IEnumerable<Line>>> GetLines(string filename)
        {
            Verify.IsNotNull(filename, nameof(filename));

            try
            {
                using (WordprocessingDocument document = WordprocessingDocument.Open(
                    filename, isEditable: false, openSettings: DocOpenSettings))
                {
                    Body body = document.MainDocumentPart.Document.Body;
                    IResult<IEnumerable<Line>> lines = new SuccessResult<IEnumerable<Line>>(GetLinesFromBody(body));
                    return Task.FromResult(lines);
                }
            }
            catch (ArgumentNullException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>("Unexpected null value found");
                return Task.FromResult(lines);
            }
            catch (OpenXmlPackageException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>(
                    "Unable to open the .docx file: " + ex.Message);
                return Task.FromResult(lines);
            }
            catch (FileFormatException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>(
                    "Unable to open the .docx file: " + ex.Message);
                return Task.FromResult(lines);
            }
        }

        public Task<IResult<IEnumerable<Line>>> GetLines(Stream stream)
        {
            Verify.IsNotNull(stream, nameof(stream));

            try
            {
                using (WordprocessingDocument document = WordprocessingDocument.Open(
                    stream, isEditable: false, openSettings: DocOpenSettings))
                {
                    Body body = document.MainDocumentPart.Document.Body;
                    IResult<IEnumerable<Line>> lines = new SuccessResult<IEnumerable<Line>>(GetLinesFromBody(body));
                    return Task.FromResult(lines);
                }
            }
            catch (ArgumentNullException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>("Unexpected null value found");
                return Task.FromResult(lines);
            }
            catch (OpenXmlPackageException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>(
                    "Unable to open the .docx file: " + ex.Message);
                return Task.FromResult(lines);
            }
            catch (FileFormatException ex)
            {
                Console.Error.WriteLine(ex);
                IResult<IEnumerable<Line>> lines = new FailureResult<IEnumerable<Line>>(
                    "Unable to open the .docx file: " + ex.Message);
                return Task.FromResult(lines);
            }
        }

        private static IEnumerable<Line> GetLinesFromBody(Body body)
        {
            List<TextBlockLine> textBlockLines = GetTextBlockLines(body);
            List<Line> lines = GetLinesFromTextBlockLines(textBlockLines);
            return lines;
        }

        private static List<TextBlockLine> GetTextBlockLines(Body body)
        {
            IEnumerable<Paragraph> paragraphs = body.ChildElements
                .Where(element => element.LocalName == "p")
                .Cast<Paragraph>();

            List<TextBlockLine> textBlockLines = new List<TextBlockLine>();
            foreach (Paragraph paragraph in paragraphs)
            {
                bool numberingAdded = false;
                List<TextBlock> textBlocks = new List<TextBlock>();
                NumberingId? numberingId = paragraph.ParagraphProperties?.NumberingProperties?.NumberingId;
                IEnumerable<Run> runs = paragraph.ChildElements.Where(element => element.LocalName == "r").Cast<Run>();

                foreach (Run run in runs)
                {
                    RunProperties? properties = run.RunProperties;
                    IEnumerable<OpenXmlElement> textualElements = run.ChildElements.Where(
                        element => element.LocalName == "t" || element.LocalName == "cr" || element.LocalName == "br");
                    foreach (OpenXmlElement textualElement in textualElements)
                    {
                        switch (textualElement.LocalName)
                        {
                            case "t":
                                // The numbering ID only applies to the first line in the paragraph
                                NumberingId? blockNumberId = !numberingAdded ? numberingId : null;
                                numberingAdded = true;

                                OpenXmlLeafTextElement textElement = (OpenXmlLeafTextElement)textualElement;
                                textBlocks.Add(new TextBlock(textElement.Text, blockNumberId, properties));
                                break;
                            case "cr":
                            case "br":
                                // cr and br and new lines (carriage return and page break). If we have blocks, add
                                // them as a separate line, and start a new line of blocks.
                                if (textBlocks.Count > 0)
                                {
                                    textBlockLines.Add(new TextBlockLine(textBlocks));
                                    textBlocks = new List<TextBlock>();
                                }
                                break;
                        }
                    }
                }

                // For accurate line numbers, we should include blank lines
                textBlockLines.Add(new TextBlockLine(textBlocks));
                textBlocks = new List<TextBlock>();
            }

            return textBlockLines;
        }

        // TODO: See if there's a way to break up this method (+100 lines)
        private static List<Line> GetLinesFromTextBlockLines(IEnumerable<TextBlockLine> textBlockLines)
        {
            // Potential issue: if the numbering doesn't start at 1, then we're off. We could look up the numbering
            // index, but question 0s are rare
            int? lastNumberingId = null;
            int currentQuestionNumber = 1;

            StringBuilder currentSegment = new StringBuilder();
            bool bolded = false;
            bool italic = false;
            bool underlined = false;

            List<Line> lines = new List<Line>();
            foreach (TextBlockLine textBlockLine in textBlockLines)
            {
                List<FormattedTextSegment> formattedTextSegments = new List<FormattedTextSegment>();

                // Convert the lines into formatted text
                int? currentNumberingId = null;
                foreach (TextBlock textBlock in textBlockLine.Blocks)
                {
                    bool blockBolded = false;
                    bool blockItalic = false;
                    bool blockUnderlined = false;
                    if (textBlock.Properties != null)
                    {
                        blockBolded = textBlock.Properties.Bold != null;
                        blockItalic = textBlock.Properties.Italic != null;
                        blockUnderlined = textBlock.Properties.Underline != null;
                    }

                    if (blockBolded != bolded || blockItalic != italic || blockUnderlined != underlined)
                    {
                        // Formatting has changed. This means the last segment finished. Add it if it has anything.
                        if (currentSegment.Length > 0)
                        {
                            formattedTextSegments.Add(
                                new FormattedTextSegment(currentSegment.ToString(), italic, bolded, underlined));
                            currentSegment.Clear();
                        }

                        // We only need to update the values if they've changed
                        bolded = blockBolded;
                        italic = blockItalic;
                        underlined = blockUnderlined;
                    }

                    currentSegment.Append(textBlock.Text);

                    if (textBlock.NumberingId != null)
                    {
                        currentNumberingId = textBlock.NumberingId.Val;
                    }
                }

                // Add the remainder of the line
                if (currentSegment.Length > 0)
                {
                    formattedTextSegments.Add(new FormattedTextSegment(currentSegment.ToString(), italic, bolded, underlined));
                    currentSegment.Clear();
                }

                // If the numbering Ids have changed, we're no longer in the same list. Reset it.
                if (currentNumberingId != null && lastNumberingId != currentNumberingId)
                {
                    lastNumberingId = currentNumberingId;
                    currentQuestionNumber = 1;
                }

                // TODO: This could be done in a different stage. Lexing shouldn't figure out if it's an answer line
                // yet, nor what the question number is. This is something that should be refactored. The one problem
                // is that we need to get the numbering ID information at this stage, and if we're doing these checks,
                // we might as well do the other ones.

                // Check the first block to see if it's an answer or digit block
                // Also, don't add empty lines
                int? questionNumber = null;
                int? bonusPartValue = null;
                bool isAnswerLine = false;
                if (formattedTextSegments.Count > 0)
                {
                    FormattedText formattedText = new FormattedText(formattedTextSegments);

                    string unformattedText = formattedText.UnformattedText;
                    if (TextStartsWithQuestionDigit(
                        unformattedText, out string? matchValue, out int? parsedQuestionNumber))
                    {
                        if (parsedQuestionNumber.HasValue)
                        {
                            questionNumber = parsedQuestionNumber.Value;
                            currentQuestionNumber = parsedQuestionNumber.Value + 1;
                        }
                        else
                        {
                            // Tiebreaker. Just use the existing number
                            questionNumber = currentQuestionNumber;
                            currentQuestionNumber++;
                        }
                    }
                    else if (TextStartsWithAnswer(unformattedText, out matchValue))
                    {
                        isAnswerLine = true;
                    }
                    else if (TextStartsWithBonsuPart(unformattedText, out matchValue, out int? partValue)
                        && partValue != null)
                    {
                        bonusPartValue = partValue;
                    }

                    if (matchValue != null)
                    {
                        formattedText = formattedText.Substring(matchValue.Length);
                    }

                    if (questionNumber == null && currentNumberingId != null)
                    {
                        questionNumber = currentQuestionNumber;
                        currentQuestionNumber++;
                    }

                    Line line = new Line(formattedText, questionNumber, bonusPartValue, isAnswerLine);
                    lines.Add(line);
                }
            }

            return lines;
        }

        private static bool TextStartsWithQuestionDigit(string text, out string? matchValue, out int? number)
        {
            number = null;
            Match match = QuestionDigitRegEx.Match(text);
            if (!match.Success)
            {
                matchValue = null;
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

        private static bool TextStartsWithAnswer(string text, out string? matchValue)
        {
            Match match = AnswerRegEx.Match(text);
            if (!match.Success)
            {
                matchValue = null;
                return false;
            }

            matchValue = match.Value;
            return true;
        }

        private static bool TextStartsWithBonsuPart(string text, out string? matchValue, out int? partValue)
        {
            partValue = null;
            Match match = BonusPartValueRegex.Match(text);
            if (!match.Success)
            {
                matchValue = null;
                return false;
            }

            matchValue = match.Value;
            string partValueText = match.Value
                .Replace("[", string.Empty, StringComparison.Ordinal)
                .Replace("]", string.Empty, StringComparison.Ordinal)
                .Trim();
            if (!int.TryParse(partValueText, out int value))
            {
                return false;
            }

            partValue = value;
            return true;
        }

        private class TextBlock
        {
            public TextBlock(string text, NumberingId? numberingId, RunProperties? properties)
            {
                this.Text = text;
                this.NumberingId = numberingId;
                this.Properties = properties;
            }

            public NumberingId? NumberingId { get; }

            public RunProperties? Properties { get; }

            public string Text { get; }
        }

        private class TextBlockLine
        {
            public TextBlockLine(IEnumerable<TextBlock> blocks)
            {
                this.Blocks = blocks;
            }

            public IEnumerable<TextBlock> Blocks { get; }
        }
    }
}
