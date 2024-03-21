namespace ZingPDF.Drawing
{
    public interface ICoordinateTranslator
    {
        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        Point FlipImageCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, Point position, int imageHeight);

        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        IEnumerable<Point> FlipPathCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, IEnumerable<Point> coordinates);

        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        BoundingBox FlipTextCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, BoundingBox boundingBox);

        /// <summary>
        /// PDF pages can be displayed rotated, which also rotates the coordinate system for that page.
        /// Rotate coordinates by a given angle, such that they are oriented relative to the displayed angle.
        /// </summary>
        IEnumerable<Point> RotateCoordinates(int angle, double pageWidth, double pageHeight, params Point[] coordinates);
    }
}
