using ZingPDF.Syntax.CommonDataStructures;

namespace ZingPDF.Elements.Drawing
{
    public class CoordinateTranslator : ICoordinateTranslator
    {
        private readonly ICalculations _calculations;

        public CoordinateTranslator(ICalculations calculations)
        {
            _calculations = calculations ?? throw new ArgumentNullException(nameof(calculations));
        }

        public Coordinate FlipImageCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, Coordinate position, int imageHeight)
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return position;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            return FlipCoordinates([position], Convert.ToInt32(newHeight - imageHeight)).First();
        }

        public IEnumerable<Coordinate> FlipPathCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, IEnumerable<Coordinate> coordinates)
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return coordinates;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            return FlipCoordinates(coordinates, Convert.ToInt32(newHeight));
        }

        public Rectangle FlipTextCoordinatesIfRequired(
            int pageDisplayRotation,
            double pageWidth,
            double pageHeight,
            CoordinateSystem coordinateSystem,
            Rectangle boundingBox
            )
        {
            if (coordinateSystem == CoordinateSystem.BottomUp)
            {
                return boundingBox;
            }

            var newHeight = _calculations.AngleIsPerpendicular(pageDisplayRotation) ? pageWidth : pageHeight;

            var origin = FlipCoordinates([boundingBox.UpperRight], Convert.ToInt32(newHeight - boundingBox.Height)).First();

            return Rectangle.FromCoordinates(origin, new Coordinate(boundingBox.Width, boundingBox.Height), boundingBox.Context);
        }

        public IEnumerable<Coordinate> RotateCoordinates(int angle, double pageWidth, double pageHeight, params Coordinate[] coordinates)
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

                return new Coordinate((int)rotatedX, (int)rotatedY);
            });
        }

        /// <summary>
        /// Substracts the given page height from a set of coordinates to vertically flip the coordinate system.
        /// </summary>
        private static IEnumerable<Coordinate> FlipCoordinates(IEnumerable<Coordinate> coordinates, int pageHeight)
        {
            return coordinates.Select(c => new Coordinate(c.X, pageHeight - c.Y));
        }
    }
}
