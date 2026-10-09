using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

/// <summary>
/// Represents a list box field.
/// </summary>
public class ListBoxFormField : ChoiceFormField
{
    public ListBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf,
        IParser<ContentStream> contentStreamParser
        )
        : this(fieldIndirectObject, name, description, properties, parent, pdf, contentStreamParser, null)
    {
    }

    internal ListBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf,
        IParser<ContentStream> contentStreamParser,
        IEnumerable<IndirectObject>? widgetObjects
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdf, contentStreamParser, widgetObjects)
    {
    }
}
