namespace ZingPDF.Drawing
{
    public class Text
    {
        public Text(string value, TextOptions options, BoundingBox bounds)
        {
            if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException($"'{nameof(value)}' cannot be null or whitespace.", nameof(value));

            Value = value;
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Bounds = bounds ?? throw new ArgumentNullException(nameof(bounds));
        }

        /// <summary>
        /// The text to be rendered.
        /// </summary>
        public string Value { get; }

        /// <summary>
        /// The options for the rendered text.
        /// </summary>
        public TextOptions Options { get; set; }

        /// <summary>
        /// The bounds within which to render the text.
        /// </summary>
        public BoundingBox Bounds { get; set; }

        /// <summary>
        /// The anti-clockwise angle of rotation in degrees.
        /// </summary>
        /// <remarks>
        /// The rotation is performed about the upper left corner of the BoundingBox.
        /// </remarks>
        public double Rotation { get; set; }

        /// <summary>
        /// The alignment of the text.
        /// </summary>
        public TextAlignment Alignment { get; set; }
    }
}
