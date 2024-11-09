using Nito.AsyncEx;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Elements.Forms
{
    public class Form
    {
        private readonly AsyncLazy<IndirectObject> _acroForm;
        private readonly AsyncLazy<InteractiveFormDictionary> _acroFormDictionary;
        private readonly IIndirectObjectDictionary _indirectObjectDictionary;

        private readonly Name _defaultFontResourceName = UniqueStringGenerator.Generate();

        public Form(IndirectObjectReference acroFormReference, IIndirectObjectDictionary indirectObjectDictionary)
        {
            _indirectObjectDictionary = indirectObjectDictionary ?? throw new ArgumentNullException(nameof(indirectObjectDictionary));

            _acroForm = new AsyncLazy<IndirectObject>(async () => await _indirectObjectDictionary.GetAsync(acroFormReference)
                    ?? throw new InvalidPdfException("Unable to resolve form reference"));

            _acroFormDictionary = new AsyncLazy<InteractiveFormDictionary>(async ()
                => (await _acroForm).Get<InteractiveFormDictionary>());
        }

        private IndirectObjectManager IndirectObjects => (IndirectObjectManager)_indirectObjectDictionary;

        public Name DefaultFontResourceName => _defaultFontResourceName;

        public async Task<IEnumerable<IFormField>> GetFieldsAsync()
        {
            var formDict = await _acroFormDictionary;

            var fields = await formDict.ResolveAsync<ArrayObject>(Constants.DictionaryKeys.InteractiveForm.Fields, _indirectObjectDictionary);

            var kids = new List<IndirectObject>();
            foreach (var kid in fields!.Cast<IndirectObjectReference>() ?? [])
            {
                kids.Add(await IndirectObjects.GetAsync(kid));
            }

            return await GetFieldsAsync(kids, null);
        }

        private async Task<IEnumerable<IFormField>> GetFieldsAsync(IEnumerable<IndirectObject> fields, string? prefix)
        {
            // Fields may be terminal or non-terminal.
            // Non-terminal fields are simply containers for other fields and provide inheritable properties
            // The Kids array contains either the field's children, or widget annotations

            List<IFormField> formFields = [];

            foreach (var field in fields)
            {
                // A field without a name is considered a widget annotation, and not a form field
                if (field.Children[0] is not FieldDictionary fieldDict || fieldDict.T is null)
                {
                    continue;
                }

                var kids = new List<IndirectObject>();
                foreach (var kid in fieldDict.Kids?.Cast<IndirectObjectReference>() ?? [])
                {
                    kids.Add(await IndirectObjects.GetAsync(kid));
                }

                string fieldName = prefix is not null ? $"{prefix}.{fieldDict.T}" : fieldDict.T!;

                // If the field is terminal, identify its type, add to the list and continue.
                if (FieldIsTerminal(kids))
                {
                    formFields.Add(GetStronglyTypedFormField(field, fieldName, fieldDict, kids));
                }
                else
                {
                    formFields.AddRange(await GetFieldsAsync(kids, fieldName));
                }
            }

            return formFields;
        }

        private static bool FieldIsTerminal(List<IndirectObject> kids)
        {
            // A terminal field can be identified by having no Kids array,
            //  OR all entries in its Kids array are widget annotations, not fields.

            if (kids.Count == 0)
            {
                return true;
            }

            foreach (var kid in kids)
            {
                var kidDict = (Dictionary)kid.Children[0];

                if (kidDict.ContainsKey(Constants.DictionaryKeys.Field.FT))
                {
                    // field has field children, therefore it's non-terminal
                    return false;
                }
            }

            return true;
        }

        internal async Task MarkFormForUpdate()
        {
            _indirectObjectDictionary.EnsureEditable();

            var acroFormDict = await _acroFormDictionary;

            EnsureNeedAppearances(acroFormDict);

            EnsureDefaultResourceDictionary(acroFormDict);

            IndirectObjects.Update(await _acroForm);
        }

        private static void EnsureNeedAppearances(InteractiveFormDictionary acroFormDictionary)
        {
            // Ensure compliant PDF viewers use the provided appearance stream for each field
            // This setting applies to pre-PDF2.0 documents.
            acroFormDictionary.SetNeedAppearances(false);
        }

        private void EnsureDefaultResourceDictionary(InteractiveFormDictionary acroFormDictionary)
        {
            if (acroFormDictionary.DR is null)
            {
                acroFormDictionary.SetResources(new ResourceDictionary());
            }

            if (acroFormDictionary.DR!.Font is null)
            {
                // TODO: can we reuse an existing font?
                // TODO: make font configurable
                var defaultFont = new Type1FontDictionary("Helvetica");

                var fontIndirectObject = IndirectObjects.Add(defaultFont);

                acroFormDictionary.DR.AddFont(_defaultFontResourceName, fontIndirectObject.Id.Reference);
            }
        }

        private IFormField GetStronglyTypedFormField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            FieldDictionary fieldDictionary,
            List<IndirectObject> kids
            )
        {
            // If a terminal field contains only a single annotation, it may optionally be merged with the field dictionary
            // We identify a merged dictionary by the subtype of /Widget

            // checkboxes
            // - Btn field represents a group of one or more checkboxes
            // - There is a widget annotation for each checkbox defining the visual appearance
            // - V contains a Name or array of Names containing the state of each checkbox

            // text
            // - Tx field represents a single field
            // ?? - there may or may not be a widget annotation initally
            // - when saving a value, a widget annotation defines the visual appearance

            var fieldProperties = new FieldProperties(fieldDictionary.Ff ?? 0);

            return fieldDictionary.FT!.ToFormFieldType() switch
            {
                FormFieldType.Button => DeriveButtonField(fieldIndirectObject, fullFieldName, fieldDictionary, fieldProperties, kids),
                FormFieldType.Text => new TextFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary,
                    _defaultFontResourceName
                    ),
                FormFieldType.Choice => DeriveChoiceField(fieldIndirectObject, fullFieldName, fieldDictionary, fieldProperties),
                FormFieldType.Signature => new SignatureFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary
                    ),
                _ => throw new InvalidOperationException("Unexpected error. Code should be unreachable"),
            };
        }

        private IFormField DeriveChoiceField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            FieldDictionary fieldDictionary,
            FieldProperties fieldProperties
            )
        {
            if (fieldProperties.IsCombo)
            {
                return new ComboBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary
                );
            }
            else
            {
                return new ListBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary
                );
            }
        }

        private IFormField DeriveButtonField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            FieldDictionary fieldDictionary,
            FieldProperties fieldProperties,
            List<IndirectObject> kids
            )
        {
            if (fieldProperties.IsPushbutton)
            {
                return new PushButtonFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary
                );
            }
            else if (fieldProperties.IsRadio)
            {
                return new RadioButtonFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary
                );
            }
            else
            {
                // kids is widget annotations for each checkbox.
                // widget annotation should have AP dictionary.
                // AP dictionary contains N (normal) and D for down.
                // TODO: consider returning info from these dictionaries

                return new CheckboxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _indirectObjectDictionary,
                    kids
                );
            }
        }
    }
}
