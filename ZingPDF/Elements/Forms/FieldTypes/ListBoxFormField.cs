using ZingPDF.Syntax.ContentStreamsAndResources;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class ListBoxFormField : FormField<IEnumerable<string>>
    {
        public ListBoxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            IEnumerable<string>? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary)
        {
        }

        protected internal override ContentStreamObject BuildVisualContent()
        {
            throw new NotImplementedException();
        }
    }
}
