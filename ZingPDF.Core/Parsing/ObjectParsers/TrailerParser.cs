using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects;
using ZingPdf.Core.Objects.ObjectGroups;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Parsing.ObjectParsers
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public IParseResult<Trailer> Parse(string content)
        {
            // trailer
            //     <</Size 22
            //       /Root 2 0 R
            //       /Info 1 0 R
            //       /ID [<81b14aafa313db63dbd6f981e49f94f4>
            //         <81b14aafa313db63dbd6f981e49f94f4>
            //       ]
            //     >>
            // startxref
            // 18799
            // %%EOF

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

            var trailer = new Trailer(
                trailerDictionary.Obj.Get<IndirectObjectReference>("Root"),
                xrefOffset,
                trailerDictionary.Obj.Get<Integer>("Size"));

            return new ParseResult<Trailer>(trailer, string.Empty);
        }
    }
}
