using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class ImageOperation
    {
        [Obsolete("Reserved for deserialisation")]
        public ImageOperation() { }

        public ImageOperation(byte[] image, Coordinate position)
            : this(1, image, position) { }

        public ImageOperation(int pageNumber, byte[] image, Coordinate position)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Value must be greater than zero");

            PageNumber = pageNumber;
            Image = image ?? throw new ArgumentNullException(nameof(image));
            Position = position ?? throw new ArgumentNullException(nameof(position));
        }

        /// <summary>
        /// The page on which to render this operation.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// The image data to be rendered.
        /// </summary>
        [Required]
        public byte[] Image { get; set; }

        /// <summary>
        /// The positions at which to render the image;
        /// </summary>
        [Required]
        public Coordinate Position { get; set; }
    }
}
