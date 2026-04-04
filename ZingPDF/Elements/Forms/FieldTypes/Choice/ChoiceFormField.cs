using System.Collections.ObjectModel;
using ZingPDF.Syntax;
using ZingPDF.Syntax.Objects;
using ZingPDF.Syntax.Objects.IndirectObjects;
using ZingPDF.Syntax.Objects.Strings;

namespace ZingPDF.Elements.Forms.FieldTypes.Choice
{
    /// <summary>
    /// Base class for choice fields such as combo boxes and list boxes.
    /// </summary>
    public abstract class ChoiceFormField(
        IndirectObject fieldIndirectObject,
        string name,
        string? description,
        FieldProperties properties,
        Form parent,
        IPdf pdf
        )
        : FormField<IPdfObject>(fieldIndirectObject, name, description, properties, parent, pdf)
    {
        /// <summary>
        /// Gets the available options for the field, including their current selected state.
        /// </summary>
        public async Task<IReadOnlyList<ChoiceItem>> GetOptionsAsync()
        {
            if (_fieldDictionary.Opt == null)
            {
                return [];
            }

            var optValues = await _fieldDictionary.Opt.GetAsync();

            List<ChoiceItem> options = [];

            foreach (var option in optValues)
            {
                var optionValues = GetOptionValues(option);
                var selected = await IsSelectedAsync(optionValues.Item1);

                options.Add(new ChoiceItem(optionValues.Item1, optionValues.Item2, selected, SelectOptionAsync, DeselectOptionAsync));
            }
            
            return options.AsReadOnly();
        }

        /// <summary>
        /// Gets an option by its display text.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null"/> when no option with the supplied display text exists.
        /// Matching is case-sensitive.
        /// </remarks>
        public async Task<ChoiceItem?> GetOptionByTextAsync(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text, nameof(text));

            return (await GetOptionsAsync()).FirstOrDefault(x => ZingPDF.Extensions.PdfStringExtensions.Decode(x.Text) == text);
        }

        /// <summary>
        /// Gets an option by its stored export value.
        /// </summary>
        /// <remarks>
        /// Returns <see langword="null"/> when no option with the supplied export value exists.
        /// Matching is case-sensitive.
        /// </remarks>
        public async Task<ChoiceItem?> GetOptionByValueAsync(string value)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

            return (await GetOptionsAsync()).FirstOrDefault(x => ZingPDF.Extensions.PdfStringExtensions.Decode(x.Value) == value);
        }

        /// <summary>
        /// Selects an option by its display text.
        /// </summary>
        public async Task<bool> SelectOptionByTextAsync(string text)
        {
            var option = await GetOptionByTextAsync(text);
            if (option is null)
            {
                return false;
            }

            await option.SelectAsync();
            return true;
        }

        /// <summary>
        /// Selects an option by its stored export value.
        /// </summary>
        public async Task<bool> SelectOptionByValueAsync(string value)
        {
            var option = await GetOptionByValueAsync(value);
            if (option is null)
            {
                return false;
            }

            await option.SelectAsync();
            return true;
        }

        protected async Task SelectOptionAsync(PdfString value)
        {
            var selectedOptions = await GetSelectedOptionsAsync();

            if (selectedOptions.Count == 0 || !Properties.IsMultiSelect)
            {
                SetValue(value);
            }
            else
            {
                selectedOptions.Add(value);

                SetValue(new ArrayObject(selectedOptions, ObjectContext.UserCreated));
            }
        }

        protected async Task DeselectOptionAsync(PdfString value)
        {
            var selectedOptions = await GetSelectedOptionsAsync();

            selectedOptions.Remove(value);

            if (selectedOptions.Count == 1)
            {
                SetValue(value);
            }
            else
            {
                SetValue(new ArrayObject(selectedOptions, ObjectContext.UserCreated));
            }
        }

        private async Task<bool> IsSelectedAsync(PdfString value) => (await GetSelectedOptionsAsync()).Contains(value);

        private async Task<List<PdfString>> GetSelectedOptionsAsync()
        {
            if (_fieldDictionary.V == null)
            {
                return [];
            }

            var val = await _fieldDictionary.V.GetAsync();

            if (val is PdfString singleOption)
            {
                return [singleOption];
            }

            if (val is ArrayObject ary)
            {
                return ary.Cast<PdfString>().ToList();
            }

            throw new InvalidOperationException($"Invalid field value encountered: {val}");
        }

        // Each item in the Opt array is either a single text string, or an array of 2 values (export value and display text)
        private static (PdfString, PdfString) GetOptionValues(IPdfObject option)
        {
            if (option is ArrayObject ary)
            {
                return ((ary[0] as PdfString)!, (ary[1] as PdfString)!);
            }
            
            var text = option as PdfString;
            
            return (text!, text!);
        }
    }
}
