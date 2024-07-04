using ZingPDF.ObjectModel;
using ZingPDF.ObjectModel.Objects.IndirectObjects;

namespace ZingPDF.Forms
{
    internal class FormManager
    {
        /// <summary>
        /// Recursively retrieve all fields from the heirarchy, returned as <see cref="FormField"/> objects in a flat collection.
        /// </summary>
        public async Task<Dictionary<string, IndirectObject>> GetFieldsAsync(IIndirectObjectDictionary indirectObjects, IEnumerable<IndirectObjectReference> fieldReferences, string? prefix = null)
        {
            Dictionary<string, IndirectObject> fields = [];

            foreach (var reference in fieldReferences)
            {
                var fieldIndirectObject = await indirectObjects.GetAsync(reference)
                    ?? throw new InvalidPdfException("Unable to resolve field reference");

                // A field without a name is considered a widget annotation
                if (fieldIndirectObject.Children[0] is not FieldDictionary field || field.T is null)
                {
                    continue;
                }

                string fieldName = prefix is not null ? $"{prefix}.{field.T}" : field.T!;

                fields.Add(fieldName, fieldIndirectObject);

                if (field.Kids is null)
                {
                    continue;
                }

                var children = await GetFieldsAsync(indirectObjects, field.Kids.Cast<IndirectObjectReference>(), field.T);

                foreach (var kvp in children)
                {
                    fields.Add(kvp.Key, kvp.Value);
                }
            }

            return fields;
        }

        public string? GetFieldValue(IPdfObject? v)
        {
            if (v is null)
            {
                return null;
            }

            // TODO
            return "";
        }
    }
}
