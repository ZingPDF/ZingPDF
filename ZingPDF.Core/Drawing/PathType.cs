namespace ZingPdf.Core.Drawing
{
    public enum PathType
    {
        /// <summary>
        /// Lines are straight between points.
        /// </summary>
        Linear,

        /// <summary>
        /// Represents a cubic bézier curve.
        /// <para />
        /// Each curve along a path is defined by four points: two endpoints, and two control points.
        /// </summary>
        Bezier,
    }
}
