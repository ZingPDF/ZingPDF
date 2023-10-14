namespace ZingPdf.Core.Objects.ObjectGroups.CrossReferenceTable
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.4 - Cross-reference table
    /// </summary>
    internal class CrossReferenceTable : PdfObjectGroup
    {
        public CrossReferenceTable(IEnumerable<CrossReferenceSection> xrefSections)
        {
            if (xrefSections is null) throw new ArgumentNullException(nameof(xrefSections));

            Objects.Add(new PdfKeyword(Constants.Xref));
            Objects.AddRange(xrefSections);
        }
    }
}
