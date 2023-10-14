using ZingPdf.Core.Objects.ObjectGroups;

namespace ZingPdf.Core.Objects
{
    /// <summary>
    /// ISO 32000-2:2020 7.5.3 - File body
    /// </summary>
    internal class Body : PdfObjectGroup
    {
        public Body(PdfObject[] objects)
        {
            if (objects is null) throw new ArgumentNullException(nameof(objects));

            Objects.AddRange(objects);
        }
    }
}
