using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Button;

/// <summary>
/// <para>ISO 32000-2:2020 12.7.5.2.3 - Check boxes</para>
/// <para>ISO 32000-2:2020 12.7.5.2.4 - Radio buttons</para>
/// 
/// <para>A checkbox or radio button field may have one or more options. The field itself is a field dictionary. 
/// Each option is defined as a widget annotation. If there is only one, the annotation may 
/// be merged with the field dictionary. When there are more than one, each option is an indirect 
/// object reference in the Kids array property.
/// </para>
/// <para>
/// Each option has 2 states, on and off. The option does not have a simple boolean value 
/// indicating whether it is on or off. It has a V property, which will be a Name. If the option 
/// is not selected, V will be /Off. This is simple and consistent when dealing with radio buttons and checkboxes. 
/// However if it is checked, the Name, by convention, might be /Yes. But it could be any valid 
/// Name value. This value is specified in the appearance dictionary (the AP property). The AP 
/// dictionary is a dictionary of dictionaries. The names of the 2 states are the dictionary keys 
/// of the N (normal) entry of the AP dictionary. This may contain /Off, but this is optional. 
/// The other key is the on state. So when selecting an option, or one of the group, the 
/// procedure is to set the V value, and the AS value to the key of the on state. (AS is required, 
/// and takes precedence over V). All other options will have their AS value set to /Off. The V Name 
/// is essentially used not to set a boolean state value, but to define the appearance of the option. 
/// By default, the appearance of /Off is an empty circle or box. 
/// The appearance dictionary under each key contains an indirect object reference to an appearance 
/// stream, which is the definition of the visual state of the option. In this way, the V and AS properties have
/// two purposes: to define which option is selected, and to choose the visual representation of the option. 
/// By way of illustration, we could define an appearance stream for /Off which shows a tick, and /Yes to show 
/// an empty box. Obviously this would be confusing, but demonstrates the nature of these field types.
/// </para>
/// 
/// <para>
/// In more simple terms, each widget annotation defines the value for each box. The user picks one, and
/// the V value is set to the chosen value. And to keep the AS value in sync, the AS value of each box is 
/// set to /Off, except for the chosen one.
/// </para>
/// 
/// <para>
/// The Opt property is also used to define export values in non-latin writing systems, as the usual way of defining them,
/// i.e. as a key in the AP dictionary, does not support non-latin characters.
/// </para>
/// 
/// <para>
/// As described, a checkbox field is very similar to a radio button group. Each option within the field does not 
/// allow independent selection. The way radio buttons and checkboxes differ is in their behaviour when selected.
/// When a new checkbox is selected, all others are deselected, unless they share an export value, in which case all 
/// with the same value are selected. (This is true throughout the PDF document for fields which share a name).
/// This behaviour is different for radio buttons. Even fields which share names or export values cannot be selected 
/// at the same time. Selecting one deselects the others. This behaviour can be overriden to match that of checkboxes
/// by setting the RadiosInUnison flag.
/// </para>
/// </summary>
internal abstract class ButtonOptionsFormField : FormField<Name>
{
    protected readonly IEnumerable<IndirectObject> _kids;

    internal ButtonOptionsFormField(
        IndirectObject fieldIndirectObject,
        string name,
        Form parent,
        IPdfEditor pdfEditor,
        IEnumerable<IndirectObject> kids
        )
        : base(fieldIndirectObject, name, parent, pdfEditor)
    {
        _kids = kids;

        InitOptions();
    }

    protected abstract void SelectOption(SelectableOption option);
    protected abstract void DeselectOption(SelectableOption option);

    public IList<SelectableOption> Options { get; private set; } = [];

    /// <summary>
    /// Sets up a <see cref="SelectableOption"/> instance for each child option of this field.
    /// This allows the user to easily select/deselect each option.
    /// On toggling the option, it will call the supplied select or deselect callback, which we 
    /// configure here to set the value of this instance.
    /// </summary>
    private void InitOptions()
    {
        List<SelectableOption> options = [];

        Options = WidgetAnnotationObjects.Select(annot =>
        {
            var widgetDict = (WidgetAnnotationDictionary)annot.Object;
            Name exportValue = GetExportValue(widgetDict);

            var @checked = _fieldDictionary.V is not null && (Name)_fieldDictionary.V == exportValue;
            
            return new SelectableOption(Name, exportValue, @checked, SelectOptionAndReset, DeselectOptionAndReset, annot);
        }).ToList();
    }

    private void SelectOptionAndReset(SelectableOption option)
    {
        SelectOption(option);
        InitOptions();
    }

    private void DeselectOptionAndReset(SelectableOption option)
    {
        DeselectOption(option);
        InitOptions();
    }

    protected static Name GetExportValue(WidgetAnnotationDictionary widgetDict)
    {
        // TODO: consider supporting Opt, which may take precedence for the definition of export values.

        Name value = Constants.ButtonStates.On;

        if (widgetDict.AP is not null)
        {
            if (widgetDict.AP.N is IndirectObject)
            {
                // TODO: handle the case where N is a stream
                throw new NotSupportedException("Widget annotation appearance dictionary contains stream-based properties. Contact support for further info.");
            }
            else
            {
                value = (widgetDict.AP.N as Dictionary).Keys.First(k => k != Constants.ButtonStates.Off);
            }
        }

        return value;
    }

    protected IEnumerable<IndirectObject> WidgetAnnotationObjects
        => !_kids.Any()
            ? [_fieldIndirectObject]
            : _kids;

    protected IEnumerable<WidgetAnnotationDictionary> WidgetAnnotations
        => WidgetAnnotationObjects.Select(k => (WidgetAnnotationDictionary)k.Object);
}
