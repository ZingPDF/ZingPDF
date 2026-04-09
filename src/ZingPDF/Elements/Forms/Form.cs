using Nito.AsyncEx;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Elements.Drawing;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
using ZingPDF.Graphics;
using ZingPDF.Graphics.FormXObjects;
using ZingPDF.InteractiveFeatures.Annotations;
using ZingPDF.InteractiveFeatures.Annotations.AppearanceStreams;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Parsing.Parsers;
using ZingPDF.Syntax;
using ZingPDF.Syntax.CommonDataStructures;
using ZingPDF.Syntax.DocumentStructure;
using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.Dictionaries;
using ZingPDF.Syntax.Objects.Dictionaries.PropertyWrappers;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Streams;
using ZingPDF.Syntax.Objects.Strings;
using ZingPDF.Text;
using ZingPDF.Text.SimpleFonts;

namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Represents an AcroForm attached to a PDF document.
    /// </summary>
    /// <remarks>
    /// Call <see cref="GetFieldsAsync()"/> to discover fields, then pattern match the returned values to
    /// the public field types such as <see cref="FieldTypes.Text.TextFormField"/>,
    /// <see cref="FieldTypes.Choice.ChoiceFormField"/>, <see cref="FieldTypes.Button.ButtonOptionsFormField"/>,
    /// or <see cref="FieldTypes.Signature.SignatureFormField"/>. If you already know the fully qualified
    /// field name, use <see cref="GetFieldAsync(string)"/> or <see cref="GetFieldAsync{TField}(string)"/>.
    /// </remarks>
    public class Form
    {
        private bool _dirty;
        private bool _flattened;

        private readonly AsyncLazy<IndirectObject> _acroForm;
        private readonly AsyncLazy<InteractiveFormDictionary> _acroFormDictionary;
        private readonly AsyncLazy<IReadOnlyList<IFormField>> _fields;
        private readonly List<IFormField> _createdFields = [];
        private readonly IPdf _pdf;
        private readonly IParser<ContentStream> _contentStreamParser;

        //private readonly Name _defaultFontResourceName = UniqueStringGenerator.Generate();

        private readonly AsyncLazy<IEnumerable<IFontMetricsProvider>> _fontProviders;

        /// <summary>
        /// Initializes an AcroForm wrapper for a loaded PDF document.
        /// </summary>
        public Form(OptionalProperty<InteractiveFormDictionary> acroForm, IPdf pdf, IParser<ContentStream> contentStreamParser)
        {
            ArgumentNullException.ThrowIfNull(acroForm, nameof(acroForm));
            ArgumentNullException.ThrowIfNull(pdf, nameof(pdf));
            ArgumentNullException.ThrowIfNull(contentStreamParser, nameof(contentStreamParser));

            _pdf = pdf;
            _contentStreamParser = contentStreamParser;
            _acroForm = new AsyncLazy<IndirectObject>(async () => await acroForm.GetIndirectObjectAsync()
                    ?? throw new InvalidPdfException("Unable to resolve form reference"));

            _acroFormDictionary = new AsyncLazy<InteractiveFormDictionary>(async ()
                => (InteractiveFormDictionary)(await _acroForm).Object);

            _fields = new AsyncLazy<IReadOnlyList<IFormField>>(LoadFieldsAsync);

            _fontProviders = new AsyncLazy<IEnumerable<IFontMetricsProvider>>(async() =>
            {
                List<IFontMetricsProvider> fontProviders = [new PDFStandardFontMetricsProvider()];

                InteractiveFormDictionary formDict = await _acroFormDictionary;
                var drProperty = await formDict.DR.GetAsync();
                if (drProperty != null)
                {
                    fontProviders.AddRange(await ResourceDictionary.FromDictionary(drProperty).GetFontMetricsProvidersAsync(_pdf.Objects));
                }

                return fontProviders;
            });
        }

        //public Name DefaultFontResourceName => _defaultFontResourceName;

        internal async Task<InteractiveFormDictionary> GetFormDictionaryAsync() => await _acroFormDictionary;
        internal async Task<IEnumerable<IFontMetricsProvider>> GetFontProvidersAsync() => await _fontProviders;

        /// <summary>
        /// Enumerates the terminal form fields in the document.
        /// </summary>
        /// <remarks>
        /// Field names are returned as fully qualified names using dot notation for nested fields.
        /// </remarks>
        public async Task<IEnumerable<IFormField>> GetFieldsAsync()
        {
            var loadedFields = await _fields;
            if (_createdFields.Count == 0)
            {
                return loadedFields;
            }

            var merged = new Dictionary<string, IFormField>(StringComparer.Ordinal);
            foreach (var field in loadedFields)
            {
                merged[field.Name] = field;
            }

            foreach (var field in _createdFields)
            {
                merged[field.Name] = field;
            }

            return merged.Values;
        }

        /// <summary>
        /// Gets a terminal form field by its fully qualified field name.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null"/> when no terminal field with the supplied name exists.
        /// Field name matching is case-sensitive.
        /// </remarks>
        public async Task<IFormField?> GetFieldAsync(string fieldName)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));

            return (await GetFieldsAsync()).FirstOrDefault(x => x.Name == fieldName);
        }

        /// <summary>
        /// Gets a terminal form field by its fully qualified field name and expected field type.
        /// </summary>
        /// <typeparam name="TField">The expected public field wrapper type.</typeparam>
        /// <remarks>
        /// Returns <see langword="null"/> when the field does not exist or is not of the requested type.
        /// </remarks>
        public async Task<TField?> GetFieldAsync<TField>(string fieldName) where TField : class, IFormField
        {
            return await GetFieldAsync(fieldName) as TField;
        }

        /// <summary>
        /// Adds a new text field to a page and returns the created field wrapper.
        /// </summary>
        /// <remarks>
        /// This creates both the terminal field dictionary and its widget annotation, wires the field into the
        /// document AcroForm, and configures a default font resource so the field can later render high-level
        /// appearance updates through <see cref="TextFormField.SetValueAsync(string?)"/>.
        /// </remarks>
        public async Task<TextFormField> AddTextFieldAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            Action<TextFormFieldCreationOptions>? configureOptions = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
            ArgumentNullException.ThrowIfNull(bounds);

            await EnsureFieldCanBeCreatedAsync(fieldName);

            var options = TextFormFieldCreationOptions.Initialize(configureOptions);
            var page = await _pdf.GetPageAsync(pageNumber);
            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;
            var font = await _pdf.RegisterStandardFontAsync(options.FontName);

            await EnsureDefaultAppearanceResourcesAsync(acroFormDictionary, font, options.FontSize);

            var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
                [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
                [Constants.DictionaryKeys.Field.FT] = (Name)"Tx",
                [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Annotation.Rect] = (Rectangle)bounds.Clone(),
                [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
                [Constants.DictionaryKeys.Field.VariableText.DA] = CreateDefaultAppearance(font.ResourceName, options.FontSize)
            }, _pdf, ObjectContext.UserCreated);

            if (!string.IsNullOrWhiteSpace(options.Description))
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto(options.Description, ObjectContext.UserCreated));
            }

            var fieldObject = await _pdf.Objects.AddAsync(fieldDictionary);
            await AddFieldToFormAsync(acroFormDictionary, acroFormObject, fieldObject.Reference);
            await AddAnnotationToPageAsync(page, fieldObject.Reference);

            var textField = new TextFormField(
                fieldObject,
                fieldName,
                options.Description,
                new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0),
                this,
                _pdf,
                _contentStreamParser);

            if (options.DefaultValue is not null)
            {
                await textField.SetValueAsync(options.DefaultValue);
            }
            else
            {
                await textField.ClearAsync();
            }

            _pdf.Objects.Update(page.IndirectObject);
            _pdf.Objects.Update(acroFormObject);

            _createdFields.Add(textField);
            MarkForUpdate();

            return textField;
        }

        /// <summary>
        /// Adds a new checkbox field to a page and returns the created field wrapper.
        /// </summary>
        public async Task<CheckboxFormField> AddCheckboxFieldAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            Action<CheckboxFormFieldCreationOptions>? configureOptions = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
            ArgumentNullException.ThrowIfNull(bounds);

            await EnsureFieldCanBeCreatedAsync(fieldName);

            var options = CheckboxFormFieldCreationOptions.Initialize(configureOptions);
            var page = await _pdf.GetPageAsync(pageNumber);
            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;
            var appearanceDictionary = await CreateButtonAppearanceDictionaryAsync(bounds.Size, options.ExportValue, radioStyle: false);

            var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
                [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
                [Constants.DictionaryKeys.Field.FT] = (Name)"Btn",
                [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Annotation.Rect] = (Rectangle)bounds.Clone(),
                [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
                [Constants.DictionaryKeys.Annotation.Border] = CreateDefaultBorderArray(),
                [Constants.DictionaryKeys.Annotation.AP] = appearanceDictionary,
                [Constants.DictionaryKeys.WidgetAnnotation.H] = (Name)"N",
                [Constants.DictionaryKeys.Annotation.AS] = (Name)(options.Checked ? options.ExportValue : Constants.ButtonStates.Off),
                [Constants.DictionaryKeys.Field.V] = (Name)(options.Checked ? options.ExportValue : Constants.ButtonStates.Off)
            }, _pdf, ObjectContext.UserCreated);

            if (!string.IsNullOrWhiteSpace(options.Description))
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto(options.Description, ObjectContext.UserCreated));
            }

            var fieldObject = await _pdf.Objects.AddAsync(fieldDictionary);
            await AddFieldToFormAsync(acroFormDictionary, acroFormObject, fieldObject.Reference);
            await AddAnnotationToPageAsync(page, fieldObject.Reference);

            var checkboxField = new CheckboxFormField(
                fieldObject,
                fieldName,
                options.Description,
                new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0),
                this,
                _pdf,
                []);

            _createdFields.Add(checkboxField);
            MarkForUpdate();

            return checkboxField;
        }

        /// <summary>
        /// Adds a new radio-button field group to a page and returns the created field wrapper.
        /// </summary>
        public async Task<RadioButtonFormField> AddRadioButtonFieldAsync(
            int pageNumber,
            string fieldName,
            IEnumerable<RadioButtonFieldOption> options,
            Action<RadioButtonFormFieldCreationOptions>? configureOptions = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
            ArgumentNullException.ThrowIfNull(options);

            var optionList = options.ToList();
            if (optionList.Count == 0)
            {
                throw new ArgumentException("At least one radio-button option is required.", nameof(options));
            }

            await EnsureFieldCanBeCreatedAsync(fieldName);

            var config = RadioButtonFormFieldCreationOptions.Initialize(configureOptions);
            var page = await _pdf.GetPageAsync(pageNumber);
            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;

            var flags = FieldFlags.Radio;
            if (config.NoToggleToOff)
            {
                flags |= FieldFlags.NoToggleToOff;
            }

            if (config.RadiosInUnison)
            {
                flags |= FieldFlags.RadiosInUnison;
            }

            var selectedValue = string.IsNullOrWhiteSpace(config.SelectedValue)
                ? Constants.ButtonStates.Off
                : config.SelectedValue;

            var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Field.FT] = (Name)"Btn",
                [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Field.Ff] = (Number)(int)flags,
                [Constants.DictionaryKeys.Field.Kids] = new ArrayObject([], ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Field.V] = (Name)selectedValue
            }, _pdf, ObjectContext.UserCreated);

            if (!string.IsNullOrWhiteSpace(config.Description))
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto(config.Description, ObjectContext.UserCreated));
            }

            var fieldObject = await _pdf.Objects.AddAsync(fieldDictionary);
            await AddFieldToFormAsync(acroFormDictionary, acroFormObject, fieldObject.Reference);
            var kidReferences = await fieldDictionary.Kids.GetAsync()
                ?? throw new InvalidOperationException("Expected the radio button field to contain a Kids array.");

            var kids = new List<IndirectObject>(optionList.Count);

            foreach (var option in optionList)
            {
                var appearanceDictionary = await CreateButtonAppearanceDictionaryAsync(option.Bounds.Size, option.Value, radioStyle: true);
                var widgetDictionary = WidgetAnnotationDictionary.FromDictionary(new Dictionary<string, IPdfObject>
                {
                    [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
                    [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
                    [Constants.DictionaryKeys.Annotation.Rect] = (Rectangle)option.Bounds.Clone(),
                    [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
                    [Constants.DictionaryKeys.Parent] = fieldObject.Reference,
                    [Constants.DictionaryKeys.Annotation.Border] = CreateDefaultBorderArray(),
                    [Constants.DictionaryKeys.Annotation.AP] = appearanceDictionary,
                    [Constants.DictionaryKeys.WidgetAnnotation.H] = (Name)"N",
                    [Constants.DictionaryKeys.Annotation.AS] = (Name)(selectedValue == option.Value ? option.Value : Constants.ButtonStates.Off)
                }, _pdf, ObjectContext.UserCreated);

                var widgetObject = await _pdf.Objects.AddAsync(widgetDictionary);
                kidReferences.Add(widgetObject.Reference);
                await AddAnnotationToPageAsync(page, widgetObject.Reference);
                kids.Add(widgetObject);
            }

            _pdf.Objects.Update(fieldObject);

            var radioField = new RadioButtonFormField(
                fieldObject,
                fieldName,
                config.Description,
                new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0),
                this,
                _pdf,
                kids);

            _createdFields.Add(radioField);
            MarkForUpdate();

            return radioField;
        }

        /// <summary>
        /// Adds a new combo box field to a page and returns the created field wrapper.
        /// </summary>
        public async Task<ComboBoxFormField> AddComboBoxFieldAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            IEnumerable<ChoiceFieldOption> options,
            Action<ChoiceFormFieldCreationOptions>? configureOptions = null)
            => (ComboBoxFormField)await AddChoiceFieldCoreAsync(pageNumber, fieldName, bounds, options, comboBox: true, configureOptions);

        /// <summary>
        /// Adds a new list box field to a page and returns the created field wrapper.
        /// </summary>
        public async Task<ListBoxFormField> AddListBoxFieldAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            IEnumerable<ChoiceFieldOption> options,
            Action<ChoiceFormFieldCreationOptions>? configureOptions = null)
            => (ListBoxFormField)await AddChoiceFieldCoreAsync(pageNumber, fieldName, bounds, options, comboBox: false, configureOptions);

        /// <summary>
        /// Adds a new signature field to a page and returns the created field wrapper.
        /// </summary>
        public async Task<SignatureFormField> AddSignatureFieldAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            Action<SignatureFormFieldCreationOptions>? configureOptions = null)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
            ArgumentNullException.ThrowIfNull(bounds);

            await EnsureFieldCanBeCreatedAsync(fieldName);

            var options = SignatureFormFieldCreationOptions.Initialize(configureOptions);
            var page = await _pdf.GetPageAsync(pageNumber);
            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;

            var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
                [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
                [Constants.DictionaryKeys.Field.FT] = (Name)"Sig",
                [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Annotation.Rect] = (Rectangle)bounds.Clone(),
                [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
                [Constants.DictionaryKeys.Annotation.Border] = CreateDefaultBorderArray()
            }, _pdf, ObjectContext.UserCreated);

            if (!string.IsNullOrWhiteSpace(options.Description))
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto(options.Description, ObjectContext.UserCreated));
            }

            var fieldObject = await _pdf.Objects.AddAsync(fieldDictionary);
            await AddFieldToFormAsync(acroFormDictionary, acroFormObject, fieldObject.Reference);
            await AddAnnotationToPageAsync(page, fieldObject.Reference);

            acroFormDictionary.Set(Constants.DictionaryKeys.InteractiveForm.SigFlags, (Number)3);
            _pdf.Objects.Update(acroFormObject);

            var signatureField = new SignatureFormField(
                fieldObject,
                fieldName,
                options.Description,
                new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0),
                this,
                _pdf);

            _createdFields.Add(signatureField);
            MarkForUpdate();

            return signatureField;
        }

        /// <summary>
        /// Flattens the AcroForm into normal page content and removes the interactive form structure.
        /// </summary>
        /// <remarks>
        /// Flattening preserves the current widget appearance streams by placing them onto their pages as normal
        /// XObject content. After flattening, <see cref="IPdf.GetFormAsync"/> will no longer return a form for the
        /// saved document.
        /// </remarks>
        public async Task FlattenAsync()
        {
            if (_flattened)
            {
                return;
            }

            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;
            var rootFieldRefs = await acroFormDictionary.Fields.GetAsync() ?? [];

            await FlattenWidgetAnnotationsAsync();

            var fieldHierarchyObjectIds = new Dictionary<int, ushort>();
            foreach (var fieldRef in rootFieldRefs.OfType<IndirectObjectReference>())
            {
                await CollectFieldHierarchyObjectIdsAsync(fieldRef, fieldHierarchyObjectIds);
            }

            foreach (var (index, generationNumber) in fieldHierarchyObjectIds)
            {
                _pdf.Objects.Delete(new IndirectObjectId(index, generationNumber));
            }

            var latestTrailer = await _pdf.Objects.GetLatestTrailerDictionaryAsync();
            var catalogReference = latestTrailer.Root
                ?? throw new InvalidPdfException("Missing Root entry");
            var catalogObject = await _pdf.Objects.GetAsync(catalogReference);
            var documentCatalog = catalogObject.Object as DocumentCatalogDictionary
                ?? throw new InvalidPdfException("Unable to resolve document catalog");

            documentCatalog.Unset(Constants.DictionaryKeys.DocumentCatalog.AcroForm);
            _pdf.Objects.Update(catalogObject);
            _pdf.Objects.Delete(new IndirectObjectId(acroFormObject.Id.Index, acroFormObject.Id.GenerationNumber));

            _dirty = false;
            _flattened = true;
        }

        private async Task<IReadOnlyList<IFormField>> LoadFieldsAsync()
        {
            var formDict = await _acroFormDictionary;

            var fields = await formDict.Fields.GetAsync();

            var kids = new List<IndirectObject>();
            foreach (var kid in fields!.Cast<IndirectObjectReference>() ?? [])
            {
                kids.Add(await _pdf.Objects.GetAsync(kid));
            }

            return (await GetFieldsAsync(kids, null)).ToList();
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
                    kids.Add(await _pdf.Objects.GetAsync(kid));
                }

                string partialFieldName = (await fieldDict.T.GetAsync())!.Decode();

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
            if (_flattened || !_dirty)
            {
                return;
            }

            var acroFormDict = await _acroFormDictionary;

            EnsureNeedAppearances(acroFormDict);

            //await EnsureDefaultResourceDictionaryAsync(acroFormDict);

            _pdf.Objects.Update(await _acroForm);
        }

        internal void MarkForUpdate()
        {
            _dirty = true;
        }

        private async Task EnsureFieldCanBeCreatedAsync(string fieldName)
        {
            if (_flattened)
            {
                throw new InvalidOperationException("Cannot add a form field after the form has been flattened.");
            }

            if (await GetFieldAsync(fieldName) is not null)
            {
                throw new InvalidOperationException($"A form field named '{fieldName}' already exists.");
            }
        }

        private async Task AddFieldToFormAsync(
            InteractiveFormDictionary acroFormDictionary,
            IndirectObject acroFormObject,
            IndirectObjectReference fieldReference)
        {
            (await acroFormDictionary.Fields.GetAsync()).Add(fieldReference);
            _pdf.Objects.Update(acroFormObject);
        }

        private async Task AddAnnotationToPageAsync(Page page, IndirectObjectReference annotationReference)
        {
            var annotations = await page.Dictionary.Annots.GetAsync() ?? new ArrayObject([], ObjectContext.UserCreated);
            annotations.Add(annotationReference);
            page.Dictionary.Set(Constants.DictionaryKeys.PageTree.Page.Annots, annotations);
            _pdf.Objects.Update(page.IndirectObject);
        }

        private async Task<ChoiceFormField> AddChoiceFieldCoreAsync(
            int pageNumber,
            string fieldName,
            Rectangle bounds,
            IEnumerable<ChoiceFieldOption> options,
            bool comboBox,
            Action<ChoiceFormFieldCreationOptions>? configureOptions)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName, nameof(fieldName));
            ArgumentNullException.ThrowIfNull(bounds);
            ArgumentNullException.ThrowIfNull(options);

            var optionList = options.ToList();
            if (optionList.Count == 0)
            {
                throw new ArgumentException("At least one choice option is required.", nameof(options));
            }

            await EnsureFieldCanBeCreatedAsync(fieldName);

            var config = ChoiceFormFieldCreationOptions.Initialize(configureOptions);
            var page = await _pdf.GetPageAsync(pageNumber);
            var acroFormObject = await _acroForm;
            var acroFormDictionary = await _acroFormDictionary;
            var font = await _pdf.RegisterStandardFontAsync(config.FontName);

            await EnsureDefaultAppearanceResourcesAsync(acroFormDictionary, font, config.FontSize);

            var flags = FieldFlags.None;
            if (comboBox)
            {
                flags |= FieldFlags.Combo;
                if (config.AllowCustomValues)
                {
                    flags |= FieldFlags.Edit;
                }
            }
            else if (config.AllowMultipleSelection)
            {
                flags |= FieldFlags.MultiSelect;
            }

            if (config.SortOptions)
            {
                flags |= FieldFlags.Sort;
            }

            var optionArray = new ArrayObject([], ObjectContext.UserCreated);
            foreach (var option in optionList)
            {
                if (option.Value == option.Text)
                {
                    optionArray.Add(PdfString.FromTextAuto(option.Text, ObjectContext.UserCreated));
                }
                else
                {
                    optionArray.Add(new ArrayObject(
                    [
                        PdfString.FromTextAuto(option.Value, ObjectContext.UserCreated),
                        PdfString.FromTextAuto(option.Text, ObjectContext.UserCreated)
                    ], ObjectContext.UserCreated));
                }
            }

            var fieldDictionary = FieldDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Type] = (Name)Constants.DictionaryTypes.Annot,
                [Constants.DictionaryKeys.Subtype] = (Name)AnnotationDictionary.Subtypes.Widget,
                [Constants.DictionaryKeys.Field.FT] = (Name)"Ch",
                [Constants.DictionaryKeys.Field.T] = PdfString.FromTextAuto(fieldName, ObjectContext.UserCreated),
                [Constants.DictionaryKeys.Annotation.Rect] = (Rectangle)bounds.Clone(),
                [Constants.DictionaryKeys.Annotation.P] = page.IndirectObject.Reference,
                [Constants.DictionaryKeys.Annotation.Border] = CreateDefaultBorderArray(),
                [Constants.DictionaryKeys.Field.VariableText.DA] = CreateDefaultAppearance(font.ResourceName, config.FontSize),
                [Constants.DictionaryKeys.Field.Opt] = optionArray
            }, _pdf, ObjectContext.UserCreated);

            if (flags != FieldFlags.None)
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.Ff, (Number)(int)flags);
            }

            if (!string.IsNullOrWhiteSpace(config.Description))
            {
                fieldDictionary.Set(Constants.DictionaryKeys.Field.TU, PdfString.FromTextAuto(config.Description, ObjectContext.UserCreated));
            }

            var fieldObject = await _pdf.Objects.AddAsync(fieldDictionary);
            await AddFieldToFormAsync(acroFormDictionary, acroFormObject, fieldObject.Reference);
            await AddAnnotationToPageAsync(page, fieldObject.Reference);

            ChoiceFormField createdField = comboBox
                ? new ComboBoxFormField(fieldObject, fieldName, config.Description, new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0), this, _pdf, _contentStreamParser)
                : new ListBoxFormField(fieldObject, fieldName, config.Description, new FieldProperties(await fieldDictionary.Ff.GetAsync() ?? 0), this, _pdf, _contentStreamParser);

            if (!string.IsNullOrWhiteSpace(config.DefaultValue))
            {
                if (comboBox && config.AllowCustomValues)
                {
                    await ((ComboBoxFormField)createdField).SelectCustomValueAsync(config.DefaultValue);
                }
                else
                {
                    var matched = await createdField.SelectOptionByValueAsync(config.DefaultValue);
                    if (!matched)
                    {
                        throw new InvalidOperationException($"No choice option with export value '{config.DefaultValue}' exists.");
                    }
                }
            }

            _createdFields.Add(createdField);
            MarkForUpdate();

            return createdField;
        }

        private async Task EnsureDefaultAppearanceResourcesAsync(
            InteractiveFormDictionary acroFormDictionary,
            PdfFont font,
            int fontSize)
        {
            var resources = await acroFormDictionary.DR.GetAsync();
            var resourceDictionary = resources is null
                ? new ResourceDictionary(_pdf, ObjectContext.UserCreated)
                : ResourceDictionary.FromDictionary(resources);

            await resourceDictionary.AddFontAsync(font.ResourceName, font.FontReference, _pdf);
            acroFormDictionary.SetResources(resourceDictionary);

            if (await acroFormDictionary.DA.GetAsync() is null)
            {
                acroFormDictionary.Set(
                    Constants.DictionaryKeys.InteractiveForm.DA,
                    CreateDefaultAppearance(font.ResourceName, fontSize));
            }
        }

        private static PdfString CreateDefaultAppearance(Name fontResourceName, int fontSize)
            => PdfString.FromAscii($"/{fontResourceName.Value} {fontSize} Tf 0 g", PdfStringSyntax.Literal, ObjectContext.UserCreated);

        private static ArrayObject CreateDefaultBorderArray()
            => new([(Number)0, (Number)0, (Number)1], ObjectContext.UserCreated);

        private async Task<AppearanceDictionary> CreateButtonAppearanceDictionaryAsync(Size bounds, string onStateName, bool radioStyle)
        {
            var offAppearance = await CreateButtonAppearanceStreamAsync(bounds, isOn: false, radioStyle);
            var onAppearance = await CreateButtonAppearanceStreamAsync(bounds, isOn: true, radioStyle);

            var offAppearanceObject = await _pdf.Objects.AddAsync(offAppearance);
            var onAppearanceObject = await _pdf.Objects.AddAsync(onAppearance);

            var stateDictionary = new Dictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.ButtonStates.Off] = offAppearanceObject.Reference,
                [onStateName] = onAppearanceObject.Reference
            }, _pdf, ObjectContext.UserCreated);

            return AppearanceDictionary.FromDictionary(new Dictionary<string, IPdfObject>
            {
                [Constants.DictionaryKeys.Appearance.N] = stateDictionary
            }, _pdf, ObjectContext.UserCreated);
        }

        private async Task<StreamObject<Type1FormDictionary>> CreateButtonAppearanceStreamAsync(Size bounds, bool isOn, bool radioStyle)
        {
            var width = Math.Max(bounds.Width, 12);
            var height = Math.Max(bounds.Height, 12);
            var appearanceBounds = Rectangle.FromDimensions(width, height);
            var background = new RGBColour(0.87, 0.90, 1.00);
            ResourceDictionary? resources = null;

            var stream = new ContentStream(ObjectContext.UserCreated)
                .SaveGraphicsState()
                .SetColour(background);

            stream.Operations.Add(new ContentStreamOperation
            {
                Operator = ContentStream.Operators.PathConstruction.re,
                Operands = [(Number)0, (Number)0, (Number)width, (Number)height]
            });
            stream.Operations.Add(new ContentStreamOperation { Operator = ContentStream.Operators.PathPainting.f });

            if (isOn)
            {
                if (radioStyle)
                {
                    AddCirclePath(stream, new Coordinate(width / 2d, height / 2d), Math.Max(Math.Min(width, height) * 0.22d, 2.5d));
                    stream.SetColour(RGBColour.Black);
                    stream.Operations.Add(new ContentStreamOperation { Operator = ContentStream.Operators.PathPainting.f });
                }
                else
                {
                    var fontResourceName = (Name)"ZaDb";
                    var dingbatsFont = new Type1FontDictionary(_pdf, ObjectContext.UserCreated);
                    dingbatsFont.Set(Constants.DictionaryKeys.Font.BaseFont, (Name)StandardPdfFonts.ZapfDingbats);
                    var fontObject = await _pdf.Objects.AddAsync(dingbatsFont);
                    resources = new ResourceDictionary(_pdf, ObjectContext.UserCreated);
                    await resources.AddFontAsync(fontResourceName, fontObject.Reference, _pdf);

                    var fontSize = Math.Max(Math.Min(width, height) * 0.75d, 9d);
                    stream
                        .BeginTextObject()
                        .SetTextState(fontResourceName, fontSize)
                        .SetColour(RGBColour.Black)
                        .SetTextMatrix(
                            1,
                            0,
                            0,
                            1,
                            Math.Max((width - fontSize * 0.8d) / 2d, 1.6d),
                            Math.Max((height - fontSize * 0.7d) / 2d, 1.4d)
                            )
                        .ShowText(PdfString.FromAscii("4", PdfStringSyntax.Literal, ObjectContext.UserCreated))
                        .EndTextObject();
                }
            }

            stream.RestoreGraphicsState();

            var formDictionary = new Type1FormDictionary(_pdf, ObjectContext.UserCreated, appearanceBounds, resources);
            return await new ContentStreamFactory([stream]).CreateAsync(formDictionary, ObjectContext.UserCreated);
        }

        private static void AddCirclePath(ContentStream stream, Coordinate centre, double radius)
        {
            const double kappa = 0.5522847498307936d;
            var controlOffset = radius * kappa;

            stream.MoveTo(new Coordinate(centre.X + radius, centre.Y));
            stream.CurveTo(
                new Coordinate(centre.X + radius, centre.Y + controlOffset),
                new Coordinate(centre.X + controlOffset, centre.Y + radius),
                new Coordinate(centre.X, centre.Y + radius));
            stream.CurveTo(
                new Coordinate(centre.X - controlOffset, centre.Y + radius),
                new Coordinate(centre.X - radius, centre.Y + controlOffset),
                new Coordinate(centre.X - radius, centre.Y));
            stream.CurveTo(
                new Coordinate(centre.X - radius, centre.Y - controlOffset),
                new Coordinate(centre.X - controlOffset, centre.Y - radius),
                new Coordinate(centre.X, centre.Y - radius));
            stream.CurveTo(
                new Coordinate(centre.X + controlOffset, centre.Y - radius),
                new Coordinate(centre.X + radius, centre.Y - controlOffset),
                new Coordinate(centre.X + radius, centre.Y));
            stream.Operations.Add(new ContentStreamOperation { Operator = ContentStream.Operators.PathConstruction.h });
        }

        private static void EnsureNeedAppearances(InteractiveFormDictionary acroFormDictionary)
        {
            // Ensure compliant PDF viewers use the provided appearance stream for each field
            // This setting applies to pre-PDF2.0 documents.
            acroFormDictionary.SetNeedAppearances(false);
        }

        //private async Task EnsureDefaultResourceDictionaryAsync(InteractiveFormDictionary acroFormDictionary)
        //{
        //    var defaultResources = new ResourceDictionary([], _pdfContext, ObjectOrigin.UserCreated);

        //    if (acroFormDictionary.DR is null)
        //    {
        //        acroFormDictionary.SetResources(defaultResources);
        //    }
        //    else
        //    {
        //        defaultResources = new ResourceDictionary(await acroFormDictionary.DR.GetAsync());
        //    }

        //    if (defaultResources.Font is null)
        //    {
        //        // TODO: can we reuse an existing font?
        //        // TODO: make font configurable
        //        var defaultFont = new Type1FontDictionary(_pdfContext, ObjectOrigin.UserCreated);

        //        var fontIndirectObject = await _pdfContext.Objects.AddAsync(defaultFont);

        //        //await defaultResources.AddFontAsync(_defaultFontResourceName, fontIndirectObject.Id.Reference, _pdfContext);
        //    }
        //}

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

            string? fieldDescription = (await fieldDictionary.TU.GetAsync())?.Decode();

            return fieldTypeName.ToFormFieldType() switch
            {
                FormFieldType.Button => DeriveButtonField(fieldIndirectObject, fullFieldName, fieldDescription, fieldProperties, kids),
                FormFieldType.Text => new TextFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdf,
                    _contentStreamParser
                    ),
                FormFieldType.Choice => DeriveChoiceField(fieldIndirectObject, fullFieldName, fieldDescription, fieldProperties),
                FormFieldType.Signature => new SignatureFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDescription,
                    fieldProperties,
                    this,
                    _pdf
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
                    _pdf,
                    _contentStreamParser
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
                    _pdf,
                    _contentStreamParser
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
                    _pdf,
                    kids
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
                    _pdf,
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
                    _pdf,
                    kids
                );
            }
        }

        private async Task FlattenWidgetAnnotationsAsync()
        {
            var pageCount = await _pdf.GetPageCountAsync();

            for (var pageNumber = 1; pageNumber <= pageCount; pageNumber++)
            {
                var page = await _pdf.GetPageAsync(pageNumber);
                var annotations = await page.Dictionary.Annots.GetAsync();
                if (annotations is null)
                {
                    continue;
                }

                var retainedAnnotations = new List<IPdfObject>();
                var pageUpdated = false;

                foreach (var annotationRef in annotations.OfType<IndirectObjectReference>())
                {
                    var annotationObject = await _pdf.Objects.GetAsync(annotationRef);
                    if (annotationObject.Object is not WidgetAnnotationDictionary widgetAnnotation)
                    {
                        retainedAnnotations.Add(annotationRef);
                        continue;
                    }

                    var flattened = await TryFlattenWidgetAnnotationAsync(page, widgetAnnotation);
                    if (!flattened)
                    {
                        retainedAnnotations.Add(annotationRef);
                        continue;
                    }

                    _pdf.Objects.Delete(new IndirectObjectId(annotationObject.Id.Index, annotationObject.Id.GenerationNumber));
                    pageUpdated = true;
                }

                if (!pageUpdated)
                {
                    continue;
                }

                page.Dictionary.Set(
                    Constants.DictionaryKeys.PageTree.Page.Annots,
                    retainedAnnotations.Count == 0
                        ? null
                        : new ArrayObject(retainedAnnotations, ObjectContext.UserCreated));

                _pdf.Objects.Update(page.IndirectObject);
            }
        }

        private async Task<bool> TryFlattenWidgetAnnotationAsync(Page page, WidgetAnnotationDictionary widgetAnnotation)
        {
            var appearance = await TryResolveAppearanceAsync(widgetAnnotation);
            if (appearance is null)
            {
                return true;
            }

            var (appearanceReference, appearanceBounds) = appearance.Value;
            var resourceName = (Name)UniqueStringGenerator.Generate();

            await page.Dictionary.AddXObjectResourceAsync(resourceName.Value, appearanceReference, _pdf);

            var fieldBounds = await widgetAnnotation.Rect.GetAsync();
            var contentStream = new FormXObjectContentStream(
                resourceName,
                fieldBounds,
                appearanceBounds,
                ObjectContext.UserCreated);

            await AddPageContentStreamAsync(page, contentStream);

            return true;
        }

        private async Task AddPageContentStreamAsync(Page page, ContentStream contentStream)
        {
            var contentStreamObject = await new ContentStreamFactory([contentStream])
                .CreateAsync(new StreamDictionary(_pdf, ObjectContext.UserCreated), ObjectContext.UserCreated);

            var contentStreamIndirectObject = await _pdf.Objects.AddAsync(contentStreamObject);

            await page.Dictionary.AddContentAsync(contentStreamIndirectObject.Reference);
            _pdf.Objects.Update(page.IndirectObject);
        }

        private async Task<(IndirectObjectReference Reference, Rectangle Bounds)?> TryResolveAppearanceAsync(
            WidgetAnnotationDictionary widgetAnnotation)
        {
            var appearanceDictionary = await widgetAnnotation.AP.GetAsync();
            if (appearanceDictionary is null)
            {
                return null;
            }

            var normalAppearance = await appearanceDictionary.N.GetAsync();
            if (normalAppearance is null)
            {
                return null;
            }

            var selectedAppearance = await ResolveSelectedAppearanceEntryAsync(widgetAnnotation, normalAppearance);
            if (selectedAppearance is null)
            {
                return null;
            }

            IStreamObject appearanceStream;
            IndirectObjectReference appearanceReference;

            switch (selectedAppearance)
            {
                case IndirectObjectReference reference:
                    appearanceReference = reference;
                    appearanceStream = await _pdf.Objects.GetAsync<IStreamObject>(reference);
                    break;
                case IStreamObject stream:
                    appearanceStream = stream;
                    appearanceReference = (await _pdf.Objects.AddAsync(stream)).Reference;
                    break;
                default:
                    return null;
            }

            var appearanceBounds = await appearanceStream.Dictionary
                .GetOptionalProperty<Rectangle>(Constants.DictionaryKeys.Form.Type1.BBox)
                .GetAsync()
                ?? await widgetAnnotation.Rect.GetAsync();

            return (appearanceReference, appearanceBounds);
        }

        private async Task<IPdfObject?> ResolveSelectedAppearanceEntryAsync(
            WidgetAnnotationDictionary widgetAnnotation,
            Either<IStreamObject, Dictionary> normalAppearance)
        {
            if (normalAppearance.Value is IStreamObject streamObject)
            {
                return streamObject;
            }

            if (normalAppearance.Value is not Dictionary appearanceStates)
            {
                return null;
            }

            var appearanceState = await widgetAnnotation.AS.GetAsync();
            if (appearanceState is not null && appearanceStates.InnerDictionary.TryGetValue(appearanceState.Value, out var selectedByState))
            {
                return selectedByState;
            }

            if (appearanceStates.InnerDictionary.TryGetValue(Constants.ButtonStates.Off, out var offState))
            {
                return offState;
            }

            return appearanceStates.FirstOrDefault().Value;
        }

        private async Task CollectFieldHierarchyObjectIdsAsync(
            IndirectObjectReference fieldReference,
            IDictionary<int, ushort> objectIds)
        {
            var fieldObject = await _pdf.Objects.GetAsync(fieldReference);
            objectIds[fieldObject.Id.Index] = fieldObject.Id.GenerationNumber;

            if (fieldObject.Object is not FieldDictionary fieldDictionary)
            {
                return;
            }

            var kids = await fieldDictionary.Kids.GetAsync();
            if (kids is null)
            {
                return;
            }

            foreach (var kidReference in kids.OfType<IndirectObjectReference>())
            {
                var kidObject = await _pdf.Objects.GetAsync(kidReference);
                objectIds[kidObject.Id.Index] = kidObject.Id.GenerationNumber;

                if (kidObject.Object is FieldDictionary)
                {
                    await CollectFieldHierarchyObjectIdsAsync(kidReference, objectIds);
                }
            }
        }
    }
}
