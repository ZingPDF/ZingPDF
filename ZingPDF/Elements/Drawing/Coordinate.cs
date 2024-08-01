using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Drawing
{
    public class Coordinate(RealNumber x, RealNumber y)
    {
        public RealNumber X { get; set; } = x ?? throw new ArgumentNullException(nameof(x));
        public RealNumber Y { get; set; } = y ?? throw new ArgumentNullException(nameof(y));

        public static Coordinate Zero => new(0, 0);
    }
}
