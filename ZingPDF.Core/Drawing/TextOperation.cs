using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class TextOperation
    {
        [Obsolete("Reserved for deserialisation")]
        public TextOperation() { }

        public TextOperation(TextSpecification textSpecification, BoundingBox boundingBox)
            : this(1, textSpecification, boundingBox) { }

        public TextOperation(int pageNumber, TextSpecification textSpecification, BoundingBox boundingBox)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Value must be greater than zero");

            PageNumber = pageNumber;
            TextSpecification = textSpecification ?? throw new ArgumentNullException(nameof(textSpecification));
            BoundingBox = boundingBox ?? throw new ArgumentNullException(nameof(boundingBox));
        }

        /// <summary>
        /// The page on which to render this operation.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The specification defining the text to be rendered.
        /// </summary>
        [Required]
        public TextSpecification TextSpecification { get; set; }

        /// <summary>
        /// The bounds within which to render the text.
        /// </summary>
        [Required]
        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// The angle of rotation in degrees, anti-clockwise. The rotation is performed about the upper left corner of the BoundingBox.
        /// </summary>
        public double RotationDegrees { get; set; }

        [EnumDataType(typeof(TextAlignment))]
        public TextAlignment TextAlignment { get; set; }
    }
}
