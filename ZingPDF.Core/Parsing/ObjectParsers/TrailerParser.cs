using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectParsers
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public Trailer Parse(IEnumerable<string> tokens)
        {
            // trailer
            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>
            // startxref
            // 148076

            // Parse trailer dictionary
            var trailerDictionary = Parser.For<Dictionary>().Parse(tokens);

            // Find cross reference table byte offset
            var index = tokens.ToList().IndexOf(Constants.StartXref);

            var xrefOffset = long.Parse(tokens.ElementAt(index + 1));

            return new Trailer(
                trailerDictionary.Get<IndirectObjectReference>("Root"),
                xrefOffset,
                trailerDictionary.Get<Integer>("Size"));
        }
    }
}
