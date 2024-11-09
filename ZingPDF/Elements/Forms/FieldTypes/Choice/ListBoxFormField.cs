using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice
{
    public class ListBoxFormField : FormField<IPdfObject>
    {
        public ListBoxFormField(
            IndirectObject fieldIndirectObject,
            string name,
            Form parent,
            IIndirectObjectDictionary indirectObjectDictionary
            )
            : base(fieldIndirectObject, name, parent, indirectObjectDictionary)
        {
            Options = _fieldDictionary.Opt is not null
                ? _fieldDictionary.Opt.Select(o =>
                    {
                        string text;
                        string value;

                        if (o is ArrayObject ary)
                        {
                            text = ary[0] as LiteralString;
                            value = ary[1] as LiteralString;
                        }
                        else
                        {
                            text = o as LiteralString;
                            value = text;
                        }

                        return new ChoiceItem(text, value, (val) =>
                        {
                            // TODO...
                        });
                    }).ToList()
                : [];
        }

        public IList<ChoiceItem> Options { get; }
    }
}
