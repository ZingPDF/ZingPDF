namespace ZingPDF.Elements.Drawing
{
    public enum PathType
    {
        /// <summary>
        /// Cubic bézier curve.
        /// </summary>
        /// <remarks>
        /// A cubic bézier curve is defined by four points: two endpoints, and two control points.
        /// </remarks>
        Bezier,

        /// <summary>
        /// Linear paths are straight between points.
        /// </summary>
        Linear
    }
}
