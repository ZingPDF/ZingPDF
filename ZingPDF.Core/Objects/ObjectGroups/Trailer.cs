using ZingPdf.Core.Objects.IndirectObjects;
using ZingPdf.Core.Objects.Primitives;

namespace ZingPdf.Core.Objects.ObjectGroups
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.5 - File Trailer
    /// </summary>
    internal class Trailer : PdfObjectGroup
    {
        public Trailer(
            IndirectObjectReference documentCatalogReference,
            long? xrefTableByteOffset,
            int objectCount,
            IndirectObjectReference? infoReference = null,
            ArrayObject? id = null
            )
        {
            if (documentCatalogReference is null) throw new ArgumentNullException(nameof(documentCatalogReference));

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
            ObjectCount = objectCount;
            Dictionary = BuildTrailerDictionary(objectCount, documentCatalogReference, infoReference, id);

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
        /// The number of indirect objects contained in the increment.
        /// </summary>
        public int ObjectCount { get; }

        /// <summary>
        /// The trailer dictionary.
        /// </summary>
        public Dictionary Dictionary { get; }

        private static Dictionary BuildTrailerDictionary(
            Integer objectCount,
            IndirectObjectReference documentCatalogReference,
            IndirectObjectReference? infoReference,
            ArrayObject? id
            )
        {
            var trailerDictionary = new Dictionary<Name, PdfObject>
            {
                { "Size", objectCount },
                { "Root", documentCatalogReference }
            };

            if (false) // TODO: this is for when there is more than one cross reference table
            {
                //trailerDictionary.Add("Prev", new IndirectObjectReference(0, 0));
            }

            if (false) // TODO: this is for when the document is encrypted
            {
                //trailerDictionary.Add("Encrypt", new Dictionary(new Dictionary<Name, PdfObject> { }));
            }

            if (infoReference != null)
            {
                trailerDictionary.Add("Info", infoReference);
            }

            if (id != null) // This is required if encrypted, optional otherwise
            {
                trailerDictionary.Add("ID", id);
            }

            return trailerDictionary;
        }
    }
}
