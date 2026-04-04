using Nito.AsyncEx;
using ZingPDF.Elements.Forms.FieldTypes.Button;
using ZingPDF.Elements.Forms.FieldTypes.Choice;
using ZingPDF.Elements.Forms.FieldTypes.Signature;
using ZingPDF.Elements.Forms.FieldTypes.Text;
using ZingPDF.Extensions;
using ZingPDF.Fonts;
using ZingPDF.Fonts.FontProviders;
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
        public async Task<IEnumerable<IFormField>> GetFieldsAsync() => await _fields;

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

            return (await _fields).FirstOrDefault(x => x.Name == fieldName);
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
                    _pdf
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
                    _pdf
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
