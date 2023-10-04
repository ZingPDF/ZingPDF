using ZingPdf.Core.Objects;

namespace ZingPdf.Core.Parsing
{
    internal class TrailerParser : IPdfObjectParser<Trailer>
    {
        public Trailer Parse(IEnumerable<string> tokens)
        {
            // trailer
            // << /Size 50 /Root 49 0 R /Info 47 0 R /ID [ <66dbd809c84b6f6bd19bb2f8865b77cc> <66dbd809c84b6f6bd19bb2f8865b77cc> ] >>
            // startxref
            // 148076

            return null;

            //return new Trailer(new IndirectObjectReference(0, 0), xrefTable, objectCount);
        }
    }
}
