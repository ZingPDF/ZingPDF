using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Represents a push-button field in an AcroForm.
/// </summary>
/// <remarks>
/// Push-button actions are not yet executable through the high-level API, but caption and action-presence
/// inspection are available for discovery workflows.
/// </remarks>
public class PushButtonFormField : FormField<IPdfObject>
{
    private readonly IReadOnlyList<IndirectObject> _kids;

    /// <summary>
    /// Initializes a push-button field wrapper.
    /// </summary>
    public PushButtonFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, description, properties, parent, pdf)
    {
        _kids = kids.ToList().AsReadOnly();
    }

    /// <summary>
    /// Gets the button caption when one is defined in the field or widget appearance characteristics.
    /// </summary>
    public async Task<string?> GetCaptionAsync()
    {
        var caption = await GetCaptionAsync(_fieldDictionary);
        if (!string.IsNullOrWhiteSpace(caption))
        {
            return caption;
        }

        foreach (var widget in WidgetAnnotationObjects)
        {
            caption = await GetCaptionAsync((WidgetAnnotationDictionary)widget.Object);
            if (!string.IsNullOrWhiteSpace(caption))
            {
                return caption;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets whether the button exposes any primary or additional action dictionaries.
    /// </summary>
    public async Task<bool> HasActionAsync()
    {
        if (await HasActionAsync(_fieldDictionary))
        {
            return true;
        }

        foreach (var widget in WidgetAnnotationObjects)
        {
            if (await HasActionAsync((WidgetAnnotationDictionary)widget.Object))
            {
                return true;
            }
        }

        return false;
    }

    private IEnumerable<IndirectObject> WidgetAnnotationObjects
        => _kids.Count == 0
            ? [_fieldIndirectObject]
            : _kids;

    private static async Task<string?> GetCaptionAsync(WidgetAnnotationDictionary widget)
    {
        var appearanceCharacteristics = await widget.MK.GetAsync();
        var caption = appearanceCharacteristics?.GetAs<PdfString>("CA");

        return caption?.DecodeText();
    }

    private static async Task<bool> HasActionAsync(WidgetAnnotationDictionary widget)
        => await widget.A.GetAsync() is not null
           || await widget.AA.GetAsync() is not null;
}
