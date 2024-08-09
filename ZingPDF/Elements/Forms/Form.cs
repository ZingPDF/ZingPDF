using Nito.AsyncEx;
using ZingPDF.Elements.Forms.FieldTypes;
using ZingPDF.Extensions;
using ZingPDF.IncrementalUpdates;
using ZingPDF.InteractiveFeatures.Forms;
using ZingPDF.Syntax;
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
            => await GetFieldsAsync((await _acroFormDictionary).Fields.Cast<IndirectObjectReference>(), null);

        private async Task<IEnumerable<IFormField>> GetFieldsAsync(IEnumerable<IndirectObjectReference> fieldReferences, string? prefix)
        {
            List<IFormField> fields = [];

            foreach (var fieldReference in fieldReferences)
            {
                var fieldIndirectObject = await _indirectObjectDictionary.GetAsync(fieldReference)
                    ?? throw new InvalidPdfException($"Unable to dereference form field reference: {fieldReference}");

                // A field without a name is considered a widget annotation
                if (fieldIndirectObject.Children[0] is not FieldDictionary field || field.T is null)
                {
                    continue;
                }

                string fieldName = prefix is not null ? $"{prefix}.{field.T}" : field.T!;

                fields.Add(GetStronglyTypedFormField(fieldIndirectObject, fieldName, field));
                if (field.Kids is null)
                {
                    continue;
                }

                fields.AddRange(await GetFieldsAsync(field.Kids.Cast<IndirectObjectReference>(), field.T));
            }

            return fields;
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

        private IFormField GetStronglyTypedFormField(IndirectObject fieldIndirectObject, string fullFieldName, FieldDictionary fieldDictionary)
        {
            return fieldDictionary.FT!.ToFormFieldType() switch
            {
                FormFieldType.Button => throw new NotImplementedException(),
                FormFieldType.Text => new TextFormField(
                    fieldIndirectObject,
                    fullFieldName,
                    fieldDictionary.TU,
                    GetTextFieldValue(fieldDictionary.V),
                    new FieldProperties(fieldDictionary.Ff ?? 0),
                    this,
                    _indirectObjectDictionary,
                    _defaultFontResourceName
                    ),
                FormFieldType.Choice => throw new NotImplementedException(),
                FormFieldType.Signature => throw new NotImplementedException(),
                _ => throw new InvalidOperationException("Unexpected error. Code should be unreachable"),
            };
        }

        private string GetTextFieldValue(IPdfObject? value)
        {
            switch (value)
            {

            }

            return "";
        }
    }
}
