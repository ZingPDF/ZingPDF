using ZingPDF.IncrementalUpdates;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// <para>ISO 32000-2:2020 12.7.5.2.2 - Push-buttons</para>
/// </summary>
internal class PushButtonFormField : FormField<IPdfObject>
{
    public PushButtonFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IPdfEditor pdfEditor
        )
        : base(fieldIndirectObject, name, parent, pdfEditor)
    {
    }
}
