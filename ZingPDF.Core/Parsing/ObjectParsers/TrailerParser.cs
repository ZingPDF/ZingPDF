using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectParsers
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public Trailer Parse(string content)
        {
            // trailer
            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>
            // startxref
            // 148076

            // Parse trailer dictionary
            var trailerDictionary = Parser.For<Dictionary>().Parse(content);

            // Find cross reference table byte offset
            var startIndex = content.IndexOf(Constants.StartXref) + Constants.StartXref.Length;

            // This loop should skip all whitespace chars and EOL markers.
            char c = content[startIndex];
            while (!c.IsInteger())
            {
                startIndex++;
                c = content[startIndex];
            }

            var endIndex = content.IndexOf(Constants.NewLine, startIndex);

            var xrefOffset = long.Parse(content[startIndex..endIndex]);

            return new Trailer(
                trailerDictionary.Get<IndirectObjectReference>("Root"),
                xrefOffset,
                trailerDictionary.Get<Integer>("Size"));
        }
    }
}
