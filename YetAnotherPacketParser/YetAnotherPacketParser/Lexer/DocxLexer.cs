using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace YetAnotherPacketParser.Lexer
{
    public class DocxLexer : ILexer
    {
        // No part should be greater than 2 million characters
        private const int MaximumCharactersInPart = 2 * 1024 * 1024;
        private static readonly OpenSettings DocOpenSettings = new OpenSettings()
        {
            MaxCharactersInPart = MaximumCharactersInPart
        };

        /// <summary>
        /// Gets the lines from the .docx file, with metadata indicating what type of line it is.
        /// </summary>
        /// <param name="stream">Stream whose contents are a .docx Microsoft Word file</param>
        /// <returns>If we were unable to open the stream, then the result is a FailureResult. Otherwise, it is a
        /// SuccessResult with a collection of lines from the document.</returns>
        public async Task<IResult<IEnumerable<ILine>>> GetLines(Stream stream)
        {
            Verify.IsNotNull(stream, nameof(stream));

            try
            {
                using (WordprocessingDocument document = WordprocessingDocument.Open(
                    stream, isEditable: false, openSettings: DocOpenSettings))
                {
                    Body? body = document.MainDocumentPart?.Document.Body;
                    if (body == null)
                    {
                        IResult<IEnumerable<ILine>> nullBodyLines = new FailureResult<IEnumerable<ILine>>(
                            Strings.UnableToOpenDocx("Couldn't find the body of the document."));
                        return nullBodyLines;
                    }

                    IResult<IEnumerable<ILine>> lines = new SuccessResult<IEnumerable<ILine>>(GetLinesFromBody(body));
                    return lines;
                }
            }
            catch (ArgumentNullException ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                IResult<IEnumerable<ILine>> lines = new FailureResult<IEnumerable<ILine>>(Strings.UnexpectedNullValue);
                return lines;
            }
            catch (OpenXmlPackageException ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                IResult<IEnumerable<ILine>> lines = new FailureResult<IEnumerable<ILine>>(
                    Strings.UnableToOpenDocx(ex.Message));
                return lines;
            }
            catch (FileFormatException ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                IResult<IEnumerable<ILine>> lines = new FailureResult<IEnumerable<ILine>>(
                    Strings.UnableToOpenDocx(ex.Message));
                return lines;
            }
        }

        private static IEnumerable<ILine> GetLinesFromBody(Body body)
        {
            // Get the list of lines with OpenXML SDK specific classes, then convert those to format-independent Line
            // instances.
            List<TextBlockLine> textBlockLines = GetTextBlockLines(body);
            List<ILine> lines = GetLinesFromTextBlockLines(textBlockLines);
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

                // For accurate line numbers, we must include blank lines
                textBlockLines.Add(new TextBlockLine(textBlocks));
                textBlocks = new List<TextBlock>();
            }

            return textBlockLines;
        }

        // TODO: See if there's a way to break up this method (+100 lines). We could move the inner loop elsewhere, but
        // that only saves about 10 lines.
        private static List<ILine> GetLinesFromTextBlockLines(IEnumerable<TextBlockLine> textBlockLines)
        {
            // TODO: Potential issue: if the numbering doesn't start at 1, then we're off. We could look up the
            // numbering index in the docx file, but question 0s are rare
            int? lastNumberingId = null;
            int currentQuestionNumber = 1;

            StringBuilder currentSegment = new StringBuilder();
            bool bolded = false;
            bool italic = false;
            bool underlined = false;

            List<ILine> lines = new List<ILine>();
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

                    NumberingId? numberingId = textBlock.NumberingId;
#pragma warning disable CS8604 // Possible null reference argument. numberingId is already verified as not null, and != null doesn't mean Val is assigned null
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                    if (numberingId != null && numberingId.Val != null)
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
#pragma warning restore CS8604 // Possible null reference argument.
                    {
                        currentNumberingId = numberingId.Val;
                    }
                }

                // Add the remainder of the line
                if (currentSegment.Length > 0)
                {
                    formattedTextSegments.Add(new FormattedTextSegment(currentSegment.ToString(), italic, bolded, underlined));
                    currentSegment.Clear();
                }

                // If the numbering Ids have changed, we're no longer in the same numbered list. Reset the number we're
                // counting.
                if (currentNumberingId != null && lastNumberingId != currentNumberingId)
                {
                    lastNumberingId = currentNumberingId;
                    currentQuestionNumber = 1;
                }

                // Check the first block to see if it's an answer or digit block
                // Also, don't add empty lines
                if (formattedTextSegments.Count > 0)
                {
                    FormattedText formattedText = new FormattedText(formattedTextSegments);

                    string unformattedText = formattedText.UnformattedText;
                    ILine line;
                    if (LexerClassifier.TextStartsWithQuestionDigit(
                        unformattedText, out string? matchValue, out int? parsedQuestionNumber))
                    {
                        int questionNumber = 0;
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

                        line = new NumberedQuestionLine(
                            formattedText.Substring(matchValue.Length), questionNumber);
                    }
                    else if (currentNumberingId != null)
                    {
                        // docx will use NumberingId instead of including the digit in the document. Therefore, we have
                        // to set a question number in lines that have a numbering ID.
                        line = new NumberedQuestionLine(formattedText.Substring(matchValue.Length), currentQuestionNumber);
                        currentQuestionNumber++;
                    }
                    else if (LexerClassifier.TextStartsWithAnswer(unformattedText, out matchValue))
                    {
                        line = new AnswerLine(formattedText.Substring(matchValue.Length));
                    }
                    else if (LexerClassifier.TextStartsWithBonsuPart(
                        unformattedText, out matchValue, out int? partValue, out char? difficultyModifier) &&
                        partValue != null)
                    {
                        line = new BonusPartLine(formattedText.Substring(matchValue.Length), partValue.Value, difficultyModifier);
                    }
                    else if (LexerClassifier.TextStartsWithPostQuestionMetadata(unformattedText))
                    {
                        line = new PostQuestionMetadataLine(formattedText);
                    }
                    else
                    {
                        line = new Line(formattedText);
                    }

                    lines.Add(line);
                }
            }

            return lines;
        }

        /// <summary>
        /// An intermediate node that stores OpenXML-specific fields like NumberingId before we can convert them to
        /// proper numbers.
        /// </summary>
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
