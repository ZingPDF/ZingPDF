namespace ZingPDF.Drawing
{
    public interface ICalculations
    {
        /// <summary>
        /// Given a value, returns the percentage of another value.
        /// </summary>
        int PercentageOfValue(double value1, double value2);

        /// <summary>
        /// Returns to two decimal places a value between 0 and 1 for the given byte value.
        /// </summary>
        double Normalise(byte val);

        /// <summary>
        /// True if the given angle is at 90 degrees to 0.
        /// </summary>
        bool AngleIsPerpendicular(int angle);

        /// <summary>
        /// PDF pages can be displayed rotated, which also rotates the coordinate system for that page.
        /// Given a page display rotation angle in degrees, find the point about which the coordinate system can be rotated
        ///     to undo that rotation, so that the true left hand side of the coordinate system aligns to the left of the displayed page.
        /// </summary>
        Point FindRotationPoint(int pageDisplayRotation, double pageWidth, double pageHeight);
    }
}
