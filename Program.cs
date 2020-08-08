using System;
using System.Collections.Generic;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace YetAnotherPacketParser
{
    public class Program
    {
        // w:r -> run
        // w:t -> text
        // w:p -> paragraph
        // w:pPr > w:numPr > w:numId -> number value
        // w:r > w:rPr > w:i -> italic
        // w:r > w:rPr > w:b -> bold
        // w:r > w:rPr > w:u -> underline (val should be "single" for single-underline)

        // For a parser (Docx/HTML -> JSON), should do the following
        // Convert to an intermediate AST that's easy to convert to JSON (tossups list, bonuses list)
        // For Docx, can make it more open as time goes on. But can enforce paragraph number
        // - Find the first instance of numbering. These are tossups
        // - Keep translating them as tossups
        //   - One paragraph that has the number (number is increasing), or "TB" or "Tiebreaker" as the start
        //   - Next paragraph has an answer (starts with ANSWER in its inner text, or ANS)
        //   - Any number of paragraphs without a number. Need to handle Editor's notes
        // - If we encounter a lower number, then we're now on bonuses. Switch to bonus parsing
        //   - One paragraph that has the number (leadin)
        //   - The following pattern
        //     - Starts with [, then rest of the question text
        //     - Starts with ANSWER or ANS in its inner text

        static void Main(string[] args)
        {
            const string path = @"D:\qbsets\Fall2015\Berkeley B + MIT A.docx";
            using (WordprocessingDocument document = WordprocessingDocument.Open(path, isEditable: false))
            {
                // Get the document body
                Body body = document.MainDocumentPart.Document.Body;
                IEnumerable<OpenXmlElement> children = body.ChildElements.Where(element => element.LocalName == "p");
                foreach (OpenXmlElement element in children)
                {
                    // Console.WriteLine(element);
                    Console.WriteLine($@"{element.LocalName}, {element.XName}, {element.Prefix}");
                    //Console.WriteLine("Paragraph " + element.OuterXml);
                    //Console.WriteLine();
                    IEnumerable<OpenXmlElement> runs = element.ChildElements.Where(element => element.LocalName == "r" || element.LocalName == "numId");
                    foreach (OpenXmlElement run in runs)
                    {
                        if (run.LocalName == "r")
                        {
                            Console.WriteLine($@"    {run.InnerText}");
                        }
                        else if (run.LocalName == "numId")
                        {
                            Console.WriteLine($@"    VALUE: {(run as NumberingId).Val}");
                        }
                    }
                }
            }

            Console.ReadLine();
        }
    }
}
