using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice;

/// <summary>
/// Represents a combo box field.
/// </summary>
public class ComboBoxFormField : ChoiceFormField
{
    public ComboBoxFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf,
        IParser<ContentStream> contentStreamParser
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdf, contentStreamParser)
    {
    }

    /// <summary>
    /// Selects a user-entered combo-box value that is not necessarily present in the option list.
    /// </summary>
    public Task SelectCustomValueAsync(string value)
        => SelectOptionAsync(PdfString.FromTextAuto(value, ObjectContext.FromImplicitOperator));

    /// <summary>
    /// Deselects a previously selected custom combo-box value.
    /// </summary>
    public Task DeselectCustomValueAsync(string value)
        => DeselectOptionAsync(PdfString.FromTextAuto(value, ObjectContext.FromImplicitOperator));
}
