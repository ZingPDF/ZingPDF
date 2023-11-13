using System.Collections.Generic;

namespace ZingPdf.Core.Drawing
{
    public interface ICoordinateTranslator
    {
        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        Coordinate FlipImageCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, Coordinate position, int imageHeight);

        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        IEnumerable<Coordinate> FlipPathCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, IEnumerable<Coordinate> coordinates);

        /// <summary>
        /// If the coordinate system is set top TopDown, apply a vertical translation to the Y coordinates.
        /// </summary>
        BoundingBox FlipTextCoordinatesIfRequired(int pageDisplayRotation, double pageWidth, double pageHeight, CoordinateSystem coordinateSystem, BoundingBox boundingBox);

        /// <summary>
        /// PDF pages can be displayed rotated, which also rotates the coordinate system for that page.
        /// Rotate coordinates by a given angle, such that they are oriented relative to the displayed angle.
        /// </summary>
        IEnumerable<Coordinate> RotateCoordinates(int angle, double pageWidth, double pageHeight, params Coordinate[] coordinates);
    }
}
