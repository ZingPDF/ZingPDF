using ZingPDF.InteractiveFeatures.Forms;

namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Decoded AcroForm field flags.
    /// </summary>
    public class FieldProperties
    {
        private readonly FieldFlags _flags;

        /// <summary>
        /// Initializes a new set of decoded field flags.
        /// </summary>
        /// <param name="flags">The raw AcroForm field flag bitmask.</param>
        public FieldProperties(int flags)
        {
            _flags = (FieldFlags)flags;
        }

        /// <summary>
        /// Gets whether the field is read-only.
        /// </summary>
        public bool IsReadOnly => _flags.HasFlag(FieldFlags.ReadOnly);
        /// <summary>
        /// Gets whether the field is required.
        /// </summary>
        public bool IsRequired => _flags.HasFlag(FieldFlags.Required);
        /// <summary>
        /// Gets whether the field should be excluded from export operations.
        /// </summary>
        public bool NoExport => _flags.HasFlag(FieldFlags.NoExport);
        /// <summary>
        /// Gets whether a text field supports multiple lines.
        /// </summary>
        public bool IsMultiline => _flags.HasFlag(FieldFlags.Multiline);
        /// <summary>
        /// Gets whether a text field is a password field.
        /// </summary>
        public bool IsPassword => _flags.HasFlag(FieldFlags.Password);

        /// <summary>
        /// Gets whether a selected radio button may be toggled back to the off state.
        /// </summary>
        public bool NoToggleToOff => _flags.HasFlag(FieldFlags.NoToggleToOff);

        /// <summary>
        /// Gets whether the field is a radio-button group.
        /// </summary>
        public bool IsRadio => _flags.HasFlag(FieldFlags.Radio);

        /// <summary>
        /// Gets whether the field is a push button.
        /// </summary>
        public bool IsPushbutton => _flags.HasFlag(FieldFlags.Pushbutton);

        /// <summary>
        /// Gets whether the choice field is a combo box.
        /// </summary>
        public bool IsCombo => _flags.HasFlag(FieldFlags.Combo);

        /// <summary>
        /// Gets whether the combo box allows arbitrary user-entered values.
        /// </summary>
        public bool IsEdit => _flags.HasFlag(FieldFlags.Edit);

        /// <summary>
        /// Gets whether the choice field options are intended to be sorted.
        /// </summary>
        public bool IsSort => _flags.HasFlag(FieldFlags.Sort);

        /// <summary>
        /// Gets whether the field represents a file-selection path.
        /// </summary>
        public bool IsFileSelect => _flags.HasFlag(FieldFlags.FileSelect);

        /// <summary>
        /// Gets whether the choice field allows multiple selections.
        /// </summary>
        public bool IsMultiSelect => _flags.HasFlag(FieldFlags.MultiSelect);

        /// <summary>
        /// Gets whether spell checking should be skipped for the field.
        /// </summary>
        public bool DoNotSpellCheck => _flags.HasFlag(FieldFlags.DoNotSpellCheck);

        /// <summary>
        /// Gets whether the field should avoid scrolling its contents.
        /// </summary>
        public bool DoNotScroll => _flags.HasFlag(FieldFlags.DoNotScroll);

        /// <summary>
        /// Gets whether the text field uses comb formatting.
        /// </summary>
        public bool IsComb => _flags.HasFlag(FieldFlags.Comb);

        /// <summary>
        /// Gets whether the field supports rich-text content.
        /// </summary>
        public bool IsRichText => _flags.HasFlag(FieldFlags.RichText);

        /// <summary>
        /// Gets whether radio buttons sharing the same export value should stay in sync.
        /// </summary>
        public bool RadiosInUnison => _flags.HasFlag(FieldFlags.RadiosInUnison);

        /// <summary>
        /// Gets the raw combined field flags value.
        /// </summary>
        public int Flags => (int)_flags;
    }
}
