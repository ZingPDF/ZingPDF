using ZingPDF.Extensions;
using ZingPDF.Syntax.Objects;

namespace ZingPDF.Syntax.FileStructure.Trailer
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.5 - File Trailer
    /// </summary>
    public class Trailer : PdfObject
    {
        public Trailer(
            TrailerDictionary trailerDictionary,
            long xrefTableByteOffset,
            ObjectContext context
            )
            : base(context)
        {
            Dictionary = trailerDictionary ?? throw new ArgumentNullException(nameof(trailerDictionary));
            XrefTableByteOffset = xrefTableByteOffset;
        }

        /// <summary>
        /// The byte offset of the cross reference table.
        /// </summary>
        public long XrefTableByteOffset { get; internal set; }

        /// <summary>
        /// The trailer dictionary.
        /// </summary>
        public TrailerDictionary Dictionary { get; }

        protected override async Task WriteOutputAsync(Stream stream)
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

            await new Keyword(Constants.Trailer, Context).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            await Dictionary.WriteAsync(stream);
            await stream.WriteNewLineAsync();

            await new Keyword(Constants.StartXref, Context).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            await ((Number)XrefTableByteOffset).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            await new Keyword(Constants.Eof, Context).WriteAsync(stream);
        }

        public override object Clone()
        {
            return new Trailer(
                (TrailerDictionary)Dictionary.Clone(),
                XrefTableByteOffset,
                Context
            );
        }
    }
}
