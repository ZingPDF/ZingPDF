using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Drawing
{
    public class Calculations : ICalculations
    {
        public int PercentageOfValue(double val, double maxValue)
        {
            return (int)Math.Round(100 / maxValue * val);
        }

        public bool AngleIsPerpendicular(int angle)
        {
            return angle % 90 == 0 && angle % 180 != 0;
        }

        public double Normalise(byte val)
        {
            return (double)Math.Round((decimal)val / 255, 2, MidpointRounding.ToEven);
        }

        public Coordinate FindRotationPoint(int pageDisplayRotation, double pageWidth, double pageHeight)
        {
            var horizontalCentre = (int)pageWidth / 2;
            var verticalCentre = (int)pageHeight / 2;

            // Find the centre of rotation which moves the page origin exactly to the correct new point.
            return pageDisplayRotation switch
            {
                90 or -270 => new Coordinate(horizontalCentre, horizontalCentre),// For 90 and -270 degree rotations, rotate around the centre of a square the size of the page width.
                -90 or 270 => new Coordinate(verticalCentre, verticalCentre),// For -90 and 270 degree rotations, rotate around the centre of a square the size of the page height.
                0 or 180 or -180 => new Coordinate(horizontalCentre, verticalCentre),// For 180 degree rotations, rotate around the page centre
                _ => throw new InvalidOperationException()
            };
        }
    }
}
