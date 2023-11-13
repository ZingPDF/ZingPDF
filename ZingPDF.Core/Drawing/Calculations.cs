using System;

namespace ZingPdf.Core.Drawing
{
    public class Calculations : ICalculations
    {
        /// <summary>
        /// Given a value, returns the percentage of another value.
        /// </summary>
        public int PercentageOfValue(double val, double maxValue)
        {
            return (int)Math.Round(100 / maxValue * val);
        }

        /// <summary>
        /// True if the given angle is at 90 degrees to 0.
        /// </summary>
        public bool AngleIsPerpendicular(int angle)
        {
            return angle % 90 == 0 && angle % 180 != 0;
        }

        /// <summary>
        /// Returns to two decimal places a value between 0 and 1 for the given byte value.
        /// </summary>
        public double Normalise(byte val)
        {
            return (double)Math.Round((decimal)val / 255, 2, MidpointRounding.ToEven);
        }

        /// <summary>
        /// PDF pages can be displayed rotated, which also rotates the coordinate system for that page.
        /// Given a page display rotation angle in degrees, find the point about which the coordinate system can be rotated
        ///     to undo that rotation, so that the true left hand side of the coordinate system aligns to the left of the displayed page.
        /// </summary>
        public Coordinate FindRotationPoint(int pageDisplayRotation, double pageWidth, double pageHeight)
        {
            var horizontalCentre = (int)pageWidth / 2;
            var verticalCentre = (int)pageHeight / 2;

            Coordinate rotationCentre;

            // Find the centre of rotation which moves the page origin exactly to the correct new point.
            switch (pageDisplayRotation)
            {
                case 90:
                case -270:
                    // For 90 and -270 degree rotations, rotate around the centre of a square the size of the page width.
                    rotationCentre = new Coordinate(horizontalCentre, horizontalCentre);
                    break;

                case -90:
                case 270:
                    // For -90 and 270 degree rotations, rotate around the centre of a square the size of the page height.
                    rotationCentre = new Coordinate(verticalCentre, verticalCentre);
                    break;

                default:
                case 180:
                case -180:
                    // For 180 degree rotations, rotate around the page centre
                    rotationCentre = new Coordinate(horizontalCentre, verticalCentre);
                    break;
            }

            return rotationCentre;
        }
    }
}
