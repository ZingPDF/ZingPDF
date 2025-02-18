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
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Elements.Forms
{
    public class Form
    {
        private bool _dirty;

        private readonly AsyncLazy<IndirectObject> _acroForm;
        private readonly AsyncLazy<InteractiveFormDictionary> _acroFormDictionary;
        private readonly PdfObjectManager _pdfObjectManager;

        private readonly Name _defaultFontResourceName = UniqueStringGenerator.Generate();

        public Form(IndirectObjectReference acroFormReference, PdfObjectManager pdfObjectManager)
        {
            ArgumentNullException.ThrowIfNull(acroFormReference, nameof(acroFormReference));
            ArgumentNullException.ThrowIfNull(pdfObjectManager, nameof(pdfObjectManager));

            _pdfObjectManager = pdfObjectManager;

            _acroForm = new AsyncLazy<IndirectObject>(async () => await _pdfObjectManager.GetAsync(acroFormReference)
                    ?? throw new InvalidPdfException("Unable to resolve form reference"));

            _acroFormDictionary = new AsyncLazy<InteractiveFormDictionary>(async ()
                => (InteractiveFormDictionary)(await _acroForm).Object);
        }

        public Name DefaultFontResourceName => _defaultFontResourceName;

        public async Task<IEnumerable<IFormField>> GetFieldsAsync()
        {
            var formDict = await _acroFormDictionary;

            var fields = await formDict.Fields.GetAsync(_pdfObjectManager);

            var kids = new List<IndirectObject>();
            foreach (var kid in fields!.Cast<IndirectObjectReference>() ?? [])
            {
                kids.Add(await _pdfObjectManager.GetAsync(kid));
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
                if (field.Object is not FieldDictionary fieldDict || fieldDict.T is null)
                {
                    continue;
                }

                ArrayObject kidRefs = [];

                if (fieldDict.Kids != null)
                {
                    kidRefs = await fieldDict.Kids.GetAsync(_pdfObjectManager);
                }

                var kids = new List<IndirectObject>();
                foreach (var kid in kidRefs.Cast<IndirectObjectReference>())
                {
                    kids.Add(await _pdfObjectManager.GetAsync(kid));
                }

                string partialFieldName = (await fieldDict.T.GetAsync(_pdfObjectManager))!;

                string fieldName = prefix is not null ? $"{prefix}.{partialFieldName}" : partialFieldName;

                // If the field is terminal, identify its type, add to the list and continue.
                if (FieldIsTerminal(kids))
                {
                    formFields.Add(await GetStronglyTypedFormFieldAsync(field, fieldName, fieldDict, kids));
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
                var kidDict = (Dictionary)kid.Object;

                if (kidDict.ContainsKey(Constants.DictionaryKeys.Field.FT))
                {
                    // field has field children, therefore it's non-terminal
                    return false;
                }
            }

            return true;
        }

        internal async Task UpdateAsync()
        {
            if (!_dirty)
            {
                return;
            }

            var acroFormDict = await _acroFormDictionary;

            EnsureNeedAppearances(acroFormDict);

            await EnsureDefaultResourceDictionaryAsync(acroFormDict);

            _pdfObjectManager.Update(await _acroForm);
        }

        internal void MarkForUpdate()
        {
            _dirty = true;
        }

        private static void EnsureNeedAppearances(InteractiveFormDictionary acroFormDictionary)
        {
            // Ensure compliant PDF viewers use the provided appearance stream for each field
            // This setting applies to pre-PDF2.0 documents.
            acroFormDictionary.SetNeedAppearances(false);
        }

        private async Task EnsureDefaultResourceDictionaryAsync(InteractiveFormDictionary acroFormDictionary)
        {
            ResourceDictionary defaultResources = [];

            if (acroFormDictionary.DR is null)
            {
                acroFormDictionary.SetResources([]);
            }
            else
            {
                defaultResources = await acroFormDictionary.DR.GetAsync(_pdfObjectManager);
            }

            if (defaultResources.Font is null)
            {
                // TODO: can we reuse an existing font?
                // TODO: make font configurable
                var defaultFont = new Type1FontDictionary("Helvetica");

                var fontIndirectObject = _pdfObjectManager.Add(defaultFont);

                defaultResources.AddFont(_defaultFontResourceName, fontIndirectObject.Id.Reference);
            }
        }

        private async Task<IFormField> GetStronglyTypedFormFieldAsync(
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

            var flags = 0;

            if (fieldDictionary.Ff != null)
            {
                flags = await fieldDictionary.Ff.GetAsync(_pdfObjectManager);
            }

            var fieldProperties = new FieldProperties(flags);

            var fieldTypeName = await fieldDictionary.FT!.GetAsync(_pdfObjectManager);

            return fieldTypeName.ToFormFieldType() switch
            {
                FormFieldType.Button => DeriveButtonField(fieldIndirectObject, fullFieldName, fieldProperties, kids),
                FormFieldType.Text => new TextFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager,
                    _defaultFontResourceName
                    ),
                FormFieldType.Choice => DeriveChoiceField(fieldIndirectObject, fullFieldName, fieldProperties),
                FormFieldType.Signature => new SignatureFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager
                    ),
                _ => throw new InvalidOperationException("Unexpected error. Code should be unreachable"),
            };
        }

        private IFormField DeriveChoiceField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            FieldProperties fieldProperties
            )
        {
            if (fieldProperties.IsCombo)
            {
                return new ComboBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager
                );
            }
            else
            {
                return new ListBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager
                );
            }
        }

        private IFormField DeriveButtonField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
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
                    _pdfObjectManager
                );
            }
            else if (fieldProperties.IsRadio)
            {
                return new RadioButtonFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager,
                    kids
                );
            }
            else
            {
                return new CheckboxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    this,
                    _pdfObjectManager,
                    kids
                );
            }
        }
    }
}
