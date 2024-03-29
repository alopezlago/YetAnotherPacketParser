﻿using System;
using System.Collections.Generic;
using YetAnotherPacketParser.Lexer;

namespace YetAnotherPacketParser
{
    internal static class Strings
    {
        // TODO: Add the rest of the strings from PacketConverter (10 strings)

        public const string CannotParseEmptyPacket = "Cannot parse empty packet.";
        public const string CouldntFindBonusPartValue = "Failed to parse bonus part. Couldn't find the part's value.";
        public const string CouldntFindBonusPartValueInFirstBlock = "Failed to parse bonus parts. Couldn't find the part's value in the first block of text.";
        public const string HtmlFileNeedsBodyElement = "HTML file needs a <body> element.";
        public const string LexingError = "Lexing Error.";
        public const string NoTossupsFound = "Failed to parse tossups. No tossups found.";
        public const string Null = "<null>";
        public const string ParseError = "Parse Error.";
        public const string UnexpectedNullValue = "Unexpected null value found";
        public const string UnknownOutputError = "No output to write. Did you choose a correct format (json, html)?";
        public const string ValueMustBeGreaterThanZero = "Value must be greater than 0";

        public static string AnswerLine(string answer)
        {
            return $"ANSWER: {answer}";
        }

        public static string BonusPart(int value, string part)
        {
            return $"[{value}] {part}";
        }

        public static string CompilationComplete(string packetName)
        {
            return $"{packetName}: Compilation complete.";
        }

        public static string CouldntFindNextPart(string context, int linesCount)
        {
            string lineString = linesCount == 1 ? "line" : "lines";
            return $"Failed to parse {context}. We couldn't find the next part after {linesCount} {lineString}.";
        }

        public static string DocumentTooLarge(string name, double maxLengthInMB)
        {
            return $"Document \"{name}\" is too large. Documents must be {maxLengthInMB} MB or less.";
        }

        public static string InvalidData(string message)
        {
            return $"Invalid data: {message}";
        }

        public static string LexingComplete(string packetName)
        {
            return $"{packetName}: Lexing complete.";
        }

        public static string NoBonusQuestionNumberFound(int bonusNumber)
        {
            return $"Failed to parse bonus #{bonusNumber}. No question number found.";
        }

        public static string NoMoreLinesFound(string context, int linesChecked)
        {
            return $"Failed to parse {context}. No more lines found. Number of lines searched for after the last part: {linesChecked}";
        }

        public static string NonThreePartBonusesFound(IEnumerable<int> bonusNumbers)
        {
            string bonusNumbersText = string.Join(", ", bonusNumbers);
            return $"Warning: non-three part bonuses found. Bonus number(s): {bonusNumbersText}";
        }

        public static string NoTossupQuestionNumberFound(int tossupNumber)
        {
            return $"Failed to parse tossup #{tossupNumber}. No question number found.";
        }

        public static string NumberedQuestion(int number, string questionText)
        {
            return $"{number}. {questionText}";
        }

        public static string ParseFailureMessage(string message, int lineNumber, string snippet)
        {
            string snippetMessage = snippet?.Length > 0 ? $@", ""{snippet}""" : "";
            return $"{message} (Line #{lineNumber}{snippetMessage})";
        }

        public static string ParsingComplete(string packetName, int tossupsCount, int bonusesCount)
        {
            return $"{packetName}: Parsing complete. {tossupsCount} tossup(s), {bonusesCount} bonus(es).";
        }

        public static string TimingLog(
            string packetName, long timeInMsLines, long timeInMsParse, long timeInMsCompile, long totalTimeMs)
        {
            return $"{packetName}: Lex {timeInMsLines}ms, Parse {timeInMsParse}ms, Compile {timeInMsCompile}ms. Total: {totalTimeMs}ms";
        }

        public static string TooManyPacketsToParse(int maximumPackets)
        {
            return $"Too many documents to parse. This only parses at most {maximumPackets} documents at a time.";
        }

        public static string UnableToOpenDocx(string message)
        {
            return $"Unable to open the .docx file: {message}";
        }

        public static string UnableToOpenHtml(string message)
        {
            return $"Unable to open the .html file: {message}";
        }

        public static string UnexpectedToken(LineType expected, LineType actual)
        {
            return $@"Unexpected line found. Was expecting a line of type ""{expected}"", but found a line of type ""{actual}""";
        }

        public static string UnknownError(string errorMessage)
        {
            return $"Unknown error: {errorMessage}";
        }

        public static string UnknownLineTypeforAnswer(string context, LineType type)
        {
            return $"Failed to parse {context}. Expected answer line, but found an " +
                    $"\"{Enum.GetName(typeof(LineType), type)}\" line.";
        }
    }
}
