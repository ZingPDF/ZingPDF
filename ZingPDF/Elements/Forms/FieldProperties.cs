using ZingPDF.InteractiveFeatures.Forms;

namespace ZingPDF.Elements.Forms
{
    /// <summary>
    /// Decoded AcroForm field flags.
    /// </summary>
    public class FieldProperties
    {
        private readonly FieldFlags _flags;

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
        public bool NoToggleToOff => _flags.HasFlag(FieldFlags.NoToggleToOff);
        public bool IsRadio => _flags.HasFlag(FieldFlags.Radio);
        public bool IsPushbutton => _flags.HasFlag(FieldFlags.Pushbutton);
        public bool IsCombo => _flags.HasFlag(FieldFlags.Combo);
        public bool IsEdit => _flags.HasFlag(FieldFlags.Edit);
        public bool IsSort => _flags.HasFlag(FieldFlags.Sort);
        public bool IsFileSelect => _flags.HasFlag(FieldFlags.FileSelect);
        public bool IsMultiSelect => _flags.HasFlag(FieldFlags.MultiSelect);
        public bool DoNotSpellCheck => _flags.HasFlag(FieldFlags.DoNotSpellCheck);
        public bool DoNotScroll => _flags.HasFlag(FieldFlags.DoNotScroll);
        public bool IsComb => _flags.HasFlag(FieldFlags.Comb);
        public bool IsRichText => _flags.HasFlag(FieldFlags.RichText);
        public bool RadiosInUnison => _flags.HasFlag(FieldFlags.RadiosInUnison);

        /// <summary>
        /// Gets the raw combined field flags value.
        /// </summary>
        public int Flags => (int)_flags;
    }
}
