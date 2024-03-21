namespace ZingPDF.Drawing
{
    public class CoordinateTranslator : ICoordinateTranslator
    {
        private readonly ICalculations _calculations;

        public CoordinateTranslator(ICalculations calculations)
        {
            _calculations = calculations ?? throw new ArgumentNullException(nameof(calculations));
        }

        public Point FlipImageCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, Point position, int imageHeight)
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return position;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            return FlipCoordinates(new[] { position }, Convert.ToInt32(newHeight - imageHeight)).First();
        }

        public IEnumerable<Point> FlipPathCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, IEnumerable<Point> coordinates)
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return coordinates;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            return FlipCoordinates(coordinates, Convert.ToInt32(newHeight));
        }

        public BoundingBox FlipTextCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, BoundingBox boundingBox)
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return boundingBox;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            var origin = FlipCoordinates(new[] { boundingBox.Origin }, Convert.ToInt32(newHeight - boundingBox.Height)).First();

            return new BoundingBox(origin, boundingBox.Width, boundingBox.Height);
        }

        public IEnumerable<Point> RotateCoordinates(int angle, double pageWidth, double pageHeight, params Point[] coordinates)
        {
            if (angle == 0)
            {
                return coordinates;
            }

            var origin = _calculations.FindRotationPoint(angle, pageWidth, pageHeight);

            // .NET trig functions require angles in radians.
            var angleInRadians = angle * Math.PI / 180;

            var cosTheta = Math.Cos(angleInRadians);
            var sinTheta = Math.Sin(angleInRadians);

            // The PDF format applies rotations to the global coordinate system
            // using a standard transformation matrix. See ISO 32000-2:2020.
            // See also https://en.wikipedia.org/wiki/Rotation_matrix for more
            // information on how to rotate coordinates in Euclidean space).

            return coordinates.Select(c =>
            {
                var rotatedX = cosTheta * (c.X - origin.X) - sinTheta * (c.Y - origin.Y) + origin.X;
                var rotatedY = sinTheta * (c.X - origin.X) + cosTheta * (c.Y - origin.Y) + origin.Y;

                return new Point((int)rotatedX, (int)rotatedY);
            });
        }

        /// <summary>
        /// Substracts the given page height from a set of coordinates to vertically flip the coordinate system.
        /// </summary>
        private static IEnumerable<Point> FlipCoordinates(IEnumerable<Point> coordinates, int pageHeight)
        {
            return coordinates.Select(c => new Point(c.X, pageHeight - c.Y));
        }
    }
}
