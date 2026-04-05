using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// Represents a push-button field in an AcroForm.
/// </summary>
/// <remarks>
/// Push-button actions are not yet executable through the high-level API, but caption and action inspection
/// are available for discovery workflows.
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

    /// <summary>
    /// Gets the primary action type name from the first available action dictionary.
    /// </summary>
    public async Task<string?> GetActionTypeAsync()
        => (await GetPrimaryActionDictionaryAsync())?.GetAs<Name>("S")?.Value;

    /// <summary>
    /// Gets the target URI when the primary action is a URI action.
    /// </summary>
    public async Task<string?> GetActionUriAsync()
    {
        var action = await GetPrimaryActionDictionaryAsync();
        if (action?.GetAs<Name>("S")?.Value != "URI")
        {
            return null;
        }

        return action.GetAs<PdfString>("URI")?.DecodeText()
            ?? action.GetAs<Name>("URI")?.Value;
    }

    /// <summary>
    /// Gets the named action value when the primary action is a named action.
    /// </summary>
    public async Task<string?> GetNamedActionAsync()
    {
        var action = await GetPrimaryActionDictionaryAsync();
        if (action?.GetAs<Name>("S")?.Value != "Named")
        {
            return null;
        }

        return action.GetAs<Name>("N")?.Value;
    }

    /// <summary>
    /// Gets the distinct trigger keys defined in any additional-actions dictionaries on the field or widgets.
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAdditionalActionTriggersAsync()
    {
        var triggers = new HashSet<string>(StringComparer.Ordinal);

        await AddAdditionalActionTriggersAsync(_fieldDictionary, triggers);

        foreach (var widget in WidgetAnnotationObjects)
        {
            await AddAdditionalActionTriggersAsync((WidgetAnnotationDictionary)widget.Object, triggers);
        }

        return triggers.OrderBy(x => x, StringComparer.Ordinal).ToList().AsReadOnly();
    }

    private IEnumerable<IndirectObject> WidgetAnnotationObjects
        => _kids.Count == 0
            ? [_fieldIndirectObject]
            : _kids;

    private async Task<Dictionary?> GetPrimaryActionDictionaryAsync()
    {
        var action = await _fieldDictionary.A.GetAsync();
        if (action is not null)
        {
            return action;
        }

        foreach (var widget in WidgetAnnotationObjects)
        {
            action = await ((WidgetAnnotationDictionary)widget.Object).A.GetAsync();
            if (action is not null)
            {
                return action;
            }
        }

        return null;
    }

    private static async Task<string?> GetCaptionAsync(WidgetAnnotationDictionary widget)
    {
        var appearanceCharacteristics = await widget.MK.GetAsync();
        var caption = appearanceCharacteristics?.GetAs<PdfString>("CA");

        return caption?.DecodeText();
    }

    private static async Task<bool> HasActionAsync(WidgetAnnotationDictionary widget)
        => await widget.A.GetAsync() is not null
           || await widget.AA.GetAsync() is not null;

    private static async Task AddAdditionalActionTriggersAsync(WidgetAnnotationDictionary widget, ISet<string> triggers)
    {
        var additionalActions = await widget.AA.GetAsync();
        if (additionalActions is null)
        {
            return;
        }

        foreach (var key in additionalActions.Keys)
        {
            triggers.Add(key);
        }
    }
}
