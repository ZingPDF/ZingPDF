using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice
{
    public abstract class ChoiceFormField : FormField<IPdfObject>
    {
        public ChoiceFormField(
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
                        var option = GetOptionValues(o);

                        return new ChoiceItem(option.Item1, option.Item2, IsSelected(option.Item1), SelectOption, DeselectOption);
                    })
                    .ToList()
                : [];
        }

        public IList<ChoiceItem> Options { get; }

        // TODO: move to unit testable class and test
        protected void SelectOption(string value)
        {
            var selectedOptions = GetSelectedOptions();

            if (selectedOptions.Count == 0 || !Properties.IsMultiSelect)
            {
                SetValue(new LiteralString(value));
            }
            else
            {
                selectedOptions.Add(value);

                SetValue(new ArrayObject(selectedOptions));
            }
        }

        protected void DeselectOption(string value)
        {
            var selectedOptions = GetSelectedOptions();

            selectedOptions.Remove(value);

            if (selectedOptions.Count == 1)
            {
                SetValue(new LiteralString(value));
            }
            else
            {
                SetValue(new ArrayObject(selectedOptions));
            }
        }

        private bool IsSelected(string value) => GetSelectedOptions().Contains(value);

        private List<LiteralString> GetSelectedOptions()
        {
            if (_fieldDictionary.V is LiteralString singleOption)
            {
                return [singleOption];
            }

            if (_fieldDictionary.V is ArrayObject ary)
            {
                return ary.Cast<LiteralString>().ToList();
            }

            return [];
        }

        // Each item in the Opt array is either a single text string, or an array of 2 values (export value and display text)
        private static (string, string) GetOptionValues(IPdfObject option)
        {
            if (option is ArrayObject ary)
            {
                return ((ary[0] as LiteralString)!, (ary[1] as LiteralString)!);
            }
            
            var text = option as LiteralString;
            
            return (text!, text!);
        }
    }
}
