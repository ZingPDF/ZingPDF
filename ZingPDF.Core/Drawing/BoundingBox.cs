using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class BoundingBox
    {
        [Obsolete("Reserved for deserialisation")]
        public BoundingBox() { }

        public BoundingBox(Coordinate coordinate, int width, int height)
        {
            if (width < 1) throw new ArgumentOutOfRangeException(nameof(width), "Argument must be greater than zero");
            if (height < 1) throw new ArgumentOutOfRangeException(nameof(height), "Argument must be greater than zero");

            Coordinate = coordinate ?? throw new ArgumentNullException(nameof(coordinate));
            Width = width;
            Height = height;
        }

        /// <summary>
        /// The coordinate location at which the bounds are located.
        /// </summary>
        [Required]
        public Coordinate Coordinate { get; set; }

        /// <summary>
        /// The width of the bounding box.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Width { get; set; }

        /// <summary>
        /// The height of the bounding box.
        /// </summary>
        [Required]
        [Range(1, int.MaxValue)]
        public int Height { get; set; }
    }
}
