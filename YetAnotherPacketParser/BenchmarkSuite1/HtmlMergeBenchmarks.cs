using System;
using System.Collections.Generic;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace YetAnotherPacketParser.Benchmarks
{
    [MemoryDiagnoser]
    public class HtmlMergeBenchmarks
    {
        private List<string> htmls = [];

        [GlobalSetup]
        public void Setup()
        {
            // Create a set of sample HTML documents with varying sizes
            htmls = new List<string>();
            for (int i = 0; i < 1000; i++)
            {
                // 200..1000 chars
                string body = new string('x', (i % 5 + 1) * 200);
                string html = $"<!doctype html><html><head><title>Doc{i}</title></head><body>{body}</body></html>";
                htmls.Add(html);
            }
        }

        [Benchmark]
        public string SubstringBaseline()
        {
            var htmlBodies = new List<string>();
            foreach (var html in htmls)
            {
                int bodyStartIndex = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                int bodyEndIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (bodyStartIndex == -1 || bodyEndIndex == -1 || bodyStartIndex > bodyEndIndex)
                {
                    continue;
                }

                // Skip past "<body>"
                bodyStartIndex += 6;
                string htmlBody = $"<h2>doc</h2>{html.Substring(bodyStartIndex, bodyEndIndex - bodyStartIndex)}";
                htmlBodies.Add(htmlBody);
            }

            return $"<html><body>{string.Join("<br>", htmlBodies)}</body></html>";
        }

        [Benchmark]
        public string SpanOptimized()
        {
            var sb = new StringBuilder();
            sb.Append("<html><body>");

            foreach (var html in htmls)
            {
                int bodyStartIndex = html.IndexOf("<body>", StringComparison.OrdinalIgnoreCase);
                int bodyEndIndex = html.LastIndexOf("</body>", StringComparison.OrdinalIgnoreCase);
                if (bodyStartIndex == -1 || bodyEndIndex == -1 || bodyStartIndex > bodyEndIndex)
                {
                    continue;
                }

                // Skip past "<body>"
                bodyStartIndex += 6;
                ReadOnlySpan<char> bodySpan = html.AsSpan(bodyStartIndex, bodyEndIndex - bodyStartIndex);

                sb.Append("<h2>doc</h2>");
                sb.Append(bodySpan);
                sb.Append("<br>");
            }

            sb.Append("</body></html>");
            return sb.ToString();
        }
    }
}