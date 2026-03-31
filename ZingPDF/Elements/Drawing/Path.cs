using System.ComponentModel;

namespace ZingPDF.Elements.Drawing
{
    /// <summary>
    /// Defines a path.
    /// </summary>
    /// <remarks>
    /// A linear path may be created using number of points. Lines between points will be straight.
    /// <para />
    /// A cubic bézier curve may be created by specifying 4 points (start, control point 1, control point 2, end).
    /// <para />
    /// Any curved path may be created by specifying a series of bézier curves in which each endpoint is also the start point of the next curve.
    /// </remarks>
    public class Path
    {
        public Path(StrokeOptions? strokeOptions, FillOptions? fillOptions, PathType type, IEnumerable<Coordinate> points)
        {
            if (strokeOptions == null && fillOptions == null) throw new ArgumentException("One of strokeOptions or fillOptions must be specified");
            if (!Enum.IsDefined(typeof(PathType), type)) throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(PathType));
            ArgumentNullException.ThrowIfNull(points);

            var pointList = points.ToArray();
            if (pointList.Any(c => c == null)) throw new ArgumentException($"Null value encountered in {nameof(points)} collection", nameof(points));

            ValidatePoints(type, pointList);

            StrokeOptions = strokeOptions;
            FillOptions = fillOptions;
            Type = type;
            Points = pointList;
        }

        /// <summary>
        /// Stroke options.
        /// </summary>
        /// <remarks>
        /// At least one of <see cref="StrokeOptions"/> or <see cref="FillOptions"/> must be specified.
        /// </remarks>
        public StrokeOptions? StrokeOptions { get; }

        /// <summary>
        /// Fill options.
        /// </summary>
        /// <remarks>
        /// At least one of <see cref="StrokeOptions"/> or <see cref="FillOptions"/> must be specified.
        /// </remarks>
        public FillOptions? FillOptions { get; }

        /// <summary>
        /// The type of path.
        /// </summary>
        public PathType Type { get; }

        /// <summary>
        /// Points defining the path.
        /// </summary>
        public IEnumerable<Coordinate> Points { get; }

        private static void ValidatePoints(PathType type, IReadOnlyCollection<Coordinate> points)
        {
            switch (type)
            {
                case PathType.Linear when points.Count < 2:
                    throw new ArgumentException("A linear path requires at least 2 points.", nameof(points));
                case PathType.Bezier when points.Count < 4 || (points.Count - 1) % 3 != 0:
                    throw new ArgumentException("A bezier path requires 4 points, plus 3 points for each additional segment.", nameof(points));
            }
        }
    }
}
