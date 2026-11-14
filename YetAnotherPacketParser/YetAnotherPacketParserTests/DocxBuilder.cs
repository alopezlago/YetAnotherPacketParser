using System.IO;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace YetAnotherPacketParserTests
{
    /// <summary>
    /// Builds .docx files in memory, so tests can cover formatting that only exists in Word documents.
    /// </summary>
    internal static class DocxBuilder
    {
        public static MemoryStream CreateDocx(params Paragraph[] paragraphs)
        {
            MemoryStream stream = new MemoryStream();
            using (WordprocessingDocument document = WordprocessingDocument.Create(
                stream, WordprocessingDocumentType.Document))
            {
                MainDocumentPart mainPart = document.AddMainDocumentPart();
                mainPart.Document = new Document(new Body(paragraphs));
                mainPart.Document.Save();
            }

            stream.Position = 0;
            return stream;
        }

        public static Paragraph CreateParagraph(params Run[] runs)
        {
            return new Paragraph(runs);
        }

        public static Run CreateRun(string text, string color = null, bool bolded = false)
        {
            Text textElement = new Text(text)
            {
                Space = SpaceProcessingModeValues.Preserve
            };

            if (color == null && !bolded)
            {
                return new Run(textElement);
            }

            RunProperties properties = new RunProperties();
            if (bolded)
            {
                properties.AppendChild(new Bold());
            }

            if (color != null)
            {
                properties.AppendChild(new Color() { Val = color });
            }

            return new Run(properties, textElement);
        }
    }
}
