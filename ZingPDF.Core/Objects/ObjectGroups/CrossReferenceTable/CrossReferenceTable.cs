using ZingPdf.Core.Extensions;
using ZingPdf.Core.Objects.Primitives;
using ZingPdf.Core.Objects.Primitives.IndirectObjects;

namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.4 - Cross-reference table
    /// </summary>
    internal class CrossReferenceTable : PdfObject
    {
        public CrossReferenceTable(IEnumerable<CrossReferenceSection> xrefSections)
        {
            Sections = xrefSections ?? throw new ArgumentNullException(nameof(xrefSections));
        }

        public IEnumerable<CrossReferenceSection> Sections { get; }

        /// <summary>
        /// Given a parsed PDF, it is important to keep the xref table intact, as it contains the file update history.
        /// The specified byte offsets for each object will likely be incorrect when the file is re-written,
        /// due to whitespace/line-break differences. This method is used to update the byte offsets of 
        /// all existing records, once the objects have been written, and we know their new offsets.
        /// </summary>
        public void UpdateByteOffsets(IEnumerable<IndirectObject> objects)
        {
            foreach(IndirectObject indirectObject in objects)
            {
                foreach (var section in Sections)
                {
                    for (var i = section.Index.StartIndex; i < section.Entries.Count; i++)
                    {
                        if (indirectObject.Id.Index == i)
                        {
                            section.Entries.ElementAt(i).IndirectObjectByteOffset = indirectObject.ByteOffset!.Value;
                            break;
                        }
                    }
                }
            }
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await new Keyword(Constants.Xref).WriteAsync(stream);
            await stream.WriteNewLineAsync();

            foreach(var section in Sections)
            {
                await section.WriteAsync(stream);
            }
        }
    }
}
