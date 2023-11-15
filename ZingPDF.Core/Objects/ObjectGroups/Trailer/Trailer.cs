using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.ObjectGroups.Trailer
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.5 - File Trailer
    /// </summary>
    internal class Trailer : PdfObjectGroup
    {
        public Trailer(
            TrailerDictionary trailerDictionary,
            long? xrefTableByteOffset
            )
        {
            if (trailerDictionary is null) throw new ArgumentNullException(nameof(trailerDictionary));

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

            XrefTableByteOffset = xrefTableByteOffset;

            Dictionary = trailerDictionary;

            Objects.Add(new Keyword(Constants.Trailer));
            InsertNewLine();

            Objects.Add(Dictionary);
            InsertNewLine();

            Objects.Add(new Keyword(Constants.StartXref));
            InsertNewLine();

            Objects.Add(new Integer(xrefTableByteOffset ?? 0));
            InsertNewLine();

            Objects.Add(new Keyword(Constants.Eof));
            InsertNewLine();
        }

        /// <summary>
        /// The byte offset of the cross reference table. This will be null until the PDF is written.
        /// </summary>
        public long? XrefTableByteOffset { get; }

        /// <summary>
        /// The trailer dictionary.
        /// </summary>
        public TrailerDictionary Dictionary { get; }
    }
}
