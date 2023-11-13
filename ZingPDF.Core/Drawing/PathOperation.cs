using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using ZingPdf.Core.Validation;

namespace ZingPdf.Core.Drawing
{
    public class PathOperation
    {
        [Obsolete("Reserved for deserialisation")]
        public PathOperation() { }

        public PathOperation(StrokeSpecification strokeSpecification, FillSpecification fillSpecification, PathType pathType, IEnumerable<Coordinate> coordinates)
            : this(1, strokeSpecification, fillSpecification, pathType, coordinates) { }

        public PathOperation(int pageNumber, StrokeSpecification strokeSpecification, FillSpecification fillSpecification, PathType pathType, IEnumerable<Coordinate> coordinates)
        {
            if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "Value must be greater than zero");
            if (strokeSpecification == null && fillSpecification == null) throw new ArgumentException("One of strokeSpecification or fillSpecification must be specified");
            if (!Enum.IsDefined(typeof(PathType), pathType)) throw new InvalidEnumArgumentException(nameof(pathType), (int)pathType, typeof(PathType));

            PageNumber = pageNumber;
            StrokeSpecification = strokeSpecification;
            FillSpecification = fillSpecification;
            PathType = pathType;
            Coordinates = coordinates ?? throw new ArgumentNullException(nameof(coordinates));

            if (coordinates.Any(c => c == null)) throw new ArgumentException("Null value encountered in collection argument", nameof(coordinates));
        }

        /// <summary>
        /// The page on which to render this operation.
        /// </summary>
        [Range(1, int.MaxValue)]
        public int PageNumber { get; set; } = 1;

        /// <summary>
        /// Optional stroke specification.
        /// <para />
        /// N.B. At least one of StrokeSpecification or FillSpecification must be specified.
        /// </summary>
        [RequiredIfNull(nameof(FillSpecification))]
        public StrokeSpecification StrokeSpecification { get; set; }

        /// <summary>
        /// Optional fill colour.
        /// <para />
        /// N.B. At least one of StrokeSpecification or FillSpecification must be specified.
        /// </summary>
        [RequiredIfNull(nameof(StrokeSpecification))]
        public FillSpecification FillSpecification { get; set; }

        /// <summary>
        /// The type of drawing operation.
        /// </summary>
        [Required]
        public PathType PathType { get; set; }

        /// <summary>
        /// Coordinates to be used in the drawing operation.
        /// <para />
        /// For linear paths, this can be any number of points.
        /// <para />
        /// For a single bézier curve, this must contain 4 points.
        /// <para />
        /// For a bézier path, this must contain a series of bézier curves, in which each endpoint is also the start point of the next curve.
        /// </summary>
        [Required]
        public IEnumerable<Coordinate> Coordinates { get; set; }
    }
}
