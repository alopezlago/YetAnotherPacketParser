using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using AngleSharp.Html.Dom;

namespace YetAnotherPacketParser.Lexer
{
    internal class HtmlLexer : ILexer
    {
        public async Task<IResult<IEnumerable<ILine>>> GetLines(Stream stream)
        {
            Verify.IsNotNull(stream, nameof(stream));

            // Should be surrounded by a try/catch, in case parsing fails
            try
            {
                IHtmlElement? body;
                using (BrowsingContext context = new BrowsingContext(Configuration.Default))
                {
                    IDocument document = await context.OpenAsync((request) => request.Content(stream)).ConfigureAwait(false);
                    body = document.Body;
                    if (body == null)
                    {
                        return new FailureResult<IEnumerable<ILine>>(Strings.HtmlFileNeedsBodyElement);
                    }
                }

                IList<FormattedText> textLines = GetTextLines(body);
                return ClassifyLines(textLines);
            }
            // Unfortunately, we don't know what AngleSharp can throw, so we have to catch-all from here
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                // This is bad form, but I'll try to narrow down the exceptions ltaer
                await Console.Error.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                IResult<IEnumerable<ILine>> lines = new FailureResult<IEnumerable<ILine>>(
                    Strings.UnableToOpenHtml(ex.Message));
                return lines;
            }
        }

        // We get the root paragraphs, then get all of the lines included in the root paragraph
        private static IList<FormattedText> GetTextLines(IHtmlElement body)
        {
            IList<FormattedText> formattedTexts = new List<FormattedText>();
            Formatting previousFormatting = new Formatting();

            foreach (IElement paragraph in body.Children
                .Where(child => child.NodeType == NodeType.Element && (child.TagName == "P" || child.TagName == "BR")))
            {
                if (!paragraph.HasChildNodes)
                {
                    string textContent = paragraph.TextContent;
                    if (!string.IsNullOrEmpty(textContent))
                    {
                        formattedTexts.Add(new FormattedText(
                            new FormattedTextSegment[]
                            {
                                new FormattedTextSegment(
                                    paragraph.TextContent,
                                    previousFormatting.Italic,
                                    previousFormatting.Bolded,
                                    previousFormatting.Underlined,
                                    previousFormatting.Subscripted,
                                    previousFormatting.Superscripted)
                            }));
                    }

                    continue;
                }

                IList<FormattedTextSegment> segments = new List<FormattedTextSegment>();
                GetString(paragraph, formattedTexts, segments, previousFormatting);
                if (segments.Count > 0)
                {
                    formattedTexts.Add(new FormattedText(segments));
                }
            }

            return formattedTexts;
        }

        private static IResult<IEnumerable<ILine>> ClassifyLines(IList<FormattedText> formattedTexts)
        {
            int currentQuestionNumber = 1;
            IList<ILine> lines = new List<ILine>();

            foreach (FormattedText formattedText in formattedTexts)
            {
                // Copy + pasted from DocxLexer, with some slight changes. Would be great if this code could be shared
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
                else
                {
                    line = new Line(formattedText);
                }

                lines.Add(line);
            }

            return new SuccessResult<IEnumerable<ILine>>(lines);
        }

        // Potential issue: we ignore <ol> tags, which would have an auto-numbering
        private static void GetString(
            INode node, IList<FormattedText> formattedTexts, IList<FormattedTextSegment> segments, Formatting formatting)
        {
            if (!node.HasChildNodes && !string.IsNullOrEmpty(node.TextContent))
            {
                segments.Add(new FormattedTextSegment(
                    node.TextContent,
                    formatting.Italic,
                    formatting.Bolded,
                    formatting.Underlined,
                    formatting.Subscripted,
                    formatting.Superscripted));
                return;
            }

            bool isElement = node.NodeType == NodeType.Element;
            IElement? element = node as Element;

            // Need to change formatting if it's B/REQ, U, I/EM, SUB, or SUP. For other ones, just call GetString on
            // the child.
            if (isElement && element != null)
            {
                if (segments.Count > 0 && (element.TagName == "P" || element.TagName == "BR"))
                {
                    // New line, so save the old text, and reset the segments
                    formattedTexts.Add(new FormattedText(new List<FormattedTextSegment>(segments)));
                    segments.Clear();
                }
                else
                {
                    UpdateFormatting(formatting, element, true);
                }
            }

            foreach (INode child in node.ChildNodes)
            {
                GetString(child, formattedTexts, segments, formatting);
            }

            if (isElement && element != null)
            {
                if (segments.Count > 0 && (element.TagName == "P" || element.TagName == "BR"))
                {
                    formattedTexts.Add(new FormattedText(new List<FormattedTextSegment>(segments)));
                    segments.Clear();
                }
                else
                {
                    UpdateFormatting(formatting, element, false);
                }
            }
        }

        private static void UpdateFormatting(Formatting formatting, IElement element, bool value)
        {
            switch (element.TagName)
            {
                case "B":
                case "REQ":
                    formatting.Bolded = value;
                    break;
                case "I":
                case "EM":
                    formatting.Italic = value;
                    break;
                case "U":
                    formatting.Underlined = value;
                    break;
                case "SUB":
                    formatting.Subscripted = value;
                    break;
                case "SUP":
                    formatting.Superscripted = value;
                    break;
                default:
                    break;
            }
        }
        private class Formatting
        {
            public bool Bolded { get; set; }

            public bool Italic { get; set; }

            public bool Underlined { get; set; }

            public bool Superscripted { get; set; }

            public bool Subscripted { get; set; }
        }
    }
}
