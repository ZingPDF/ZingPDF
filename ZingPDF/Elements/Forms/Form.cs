using Nito.AsyncEx;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
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
        private readonly IPdfEditor _pdfEditor;

        private readonly Name _defaultFontResourceName = UniqueStringGenerator.Generate();

        private readonly AsyncLazy<IEnumerable<IFontMetricsProvider>> _fontProviders;

        public Form(DictionaryProperty<InteractiveFormDictionary?> acroForm, IPdfEditor pdfEditor)
        {
            ArgumentNullException.ThrowIfNull(acroForm, nameof(acroForm));
            ArgumentNullException.ThrowIfNull(pdfEditor, nameof(pdfEditor));

            _pdfEditor = pdfEditor;

            _acroForm = new AsyncLazy<IndirectObject>(async () => await acroForm.GetIndirectObjectAsync()
                    ?? throw new InvalidPdfException("Unable to resolve form reference"));

            _acroFormDictionary = new AsyncLazy<InteractiveFormDictionary>(async ()
                => (InteractiveFormDictionary)(await _acroForm).Object);

            _fontProviders = new AsyncLazy<IEnumerable<IFontMetricsProvider>>(async() =>
            {
                var simpleFontMetricsProvider = new SimpleFontMetricsProvider();

                List<IFontMetricsProvider> fontProviders = [new PDFStandardFontMetricsProvider(), simpleFontMetricsProvider];
                InteractiveFormDictionary formDict = await _acroFormDictionary;

                var drProperty = await formDict.DR.GetAsync();
                if (drProperty != null)
                {
                    var defaultResources = ResourceDictionary.FromDictionary(drProperty);

                    var fontDict = await defaultResources.Font.GetAsync();
                    if (fontDict != null)
                    {
                        Dictionary<string, Stream> fontStreams = [];
                        foreach (var kvp in fontDict)
                        {
                            var font = await _pdfEditor.GetAsync<FontDictionary>((IndirectObjectReference)kvp.Value);
                            var fontDescriptor = await font.FontDescriptor.GetAsync();

                            if (fontDescriptor != null)
                            {
                                var widthsArray = await font.Widths.GetAsync();
                                var firstCharCode = await font.FirstChar.GetAsync();

                                var widths = widthsArray
                                    .Cast<Number>()
                                    .Select((width, index) => new { width, index })
                                    .ToDictionary(x => (char)(firstCharCode + x.index), x => (int)x.width);

                                simpleFontMetricsProvider.FontMetrics.Add(
                                    await fontDescriptor.FontName.GetAsync(),
                                    await fontDescriptor.ToFontMetricsAsync(widths)
                                    );
                            }

                            //fontStreams.Add(kvp.Key, await font.GetDecompressedDataAsync(_pdfEditor));
                        }
                    }

                }

                return fontProviders;
            });
        }

        public Name DefaultFontResourceName => _defaultFontResourceName;

        internal async Task<InteractiveFormDictionary> GetFormDictionaryAsync() => await _acroFormDictionary;
        internal async Task<IEnumerable<IFontMetricsProvider>> GetFontProvidersAsync() => await _fontProviders;

        public async Task<IEnumerable<IFormField>> GetFieldsAsync()
        {
            var formDict = await _acroFormDictionary;

            var fields = await formDict.Fields.GetAsync();

            var kids = new List<IndirectObject>();
            foreach (var kid in fields!.Cast<IndirectObjectReference>() ?? [])
            {
                kids.Add(await _pdfEditor.GetAsync(kid));
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

                ArrayObject kidRefs = await fieldDict.Kids.GetAsync() ?? [];

                var kids = new List<IndirectObject>();
                foreach (var kid in kidRefs.Cast<IndirectObjectReference>())
                {
                    kids.Add(await _pdfEditor.GetAsync(kid));
                }

                string partialFieldName = (await fieldDict.T.GetAsync())!;

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

            //await EnsureDefaultResourceDictionaryAsync(acroFormDict);

            _pdfEditor.Update(await _acroForm);
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
            var defaultResources = new ResourceDictionary([], _pdfEditor);

            if (acroFormDictionary.DR is null)
            {
                acroFormDictionary.SetResources(defaultResources);
            }
            else
            {
                defaultResources = new ResourceDictionary(await acroFormDictionary.DR.GetAsync());
            }

            if (defaultResources.Font is null)
            {
                // TODO: can we reuse an existing font?
                // TODO: make font configurable
                var defaultFont = new FontDictionary(FontDictionary.Subtypes.Type1, _pdfEditor);

                var fontIndirectObject = _pdfEditor.Add(defaultFont);

                await defaultResources.AddFontAsync(_defaultFontResourceName, fontIndirectObject.Id.Reference, _pdfEditor);
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

            var fieldProperties = new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0);

            Name fieldTypeName = (await fieldDictionary.FT.GetAsync())!;

            string? fieldDescription = await fieldDictionary.TU.GetAsync();

            return fieldTypeName.ToFormFieldType() switch
            {
                FormFieldType.Button => DeriveButtonField(fieldIndirectObject, fullFieldName, fieldDescription, fieldProperties, kids),
                FormFieldType.Text => new TextFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor,
                    _defaultFontResourceName
                    ),
                FormFieldType.Choice => DeriveChoiceField(fieldIndirectObject, fullFieldName, fieldDescription, fieldProperties),
                FormFieldType.Signature => new SignatureFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor
                    ),
                _ => throw new InvalidOperationException("Unexpected error. Code should be unreachable"),
            };
        }

        private IFormField DeriveChoiceField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            string? fieldDescription,
            FieldProperties fieldProperties
            )
        {
            if (fieldProperties.IsCombo)
            {
                return new ComboBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor
                );
            }
            else
            {
                return new ListBoxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor
                );
            }
        }

        private IFormField DeriveButtonField(
            IndirectObject fieldIndirectObject,
            string fullFieldName,
            string? fieldDescription,
            FieldProperties fieldProperties,
            List<IndirectObject> kids
            )
        {
            if (fieldProperties.IsPushbutton)
            {
                return new PushButtonFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor
                );
            }
            else if (fieldProperties.IsRadio)
            {
                return new RadioButtonFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor,
                    kids
                );
            }
            else
            {
                return new CheckboxFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdfEditor,
                    kids
                );
            }
        }
    }
}
