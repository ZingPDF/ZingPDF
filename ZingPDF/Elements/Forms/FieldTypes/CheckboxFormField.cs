using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.ContentStreamsAndResources;

namespace ZingPDF.Elements.Forms.FieldTypes
{
    public class CheckboxFormField : FormField<bool>
    {
        public CheckboxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            string? description,
            bool? value,
            FieldProperties properties,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary,
            Name fontResourceName
            )
            : base(fieldIndirectObject, name, description, value, properties, parent, indirectObjectDictionary, fontResourceName) { }

        protected internal override ContentStreamObject BuildVisualContent()
        {
            throw new NotImplementedException();
        }
    }
}
 