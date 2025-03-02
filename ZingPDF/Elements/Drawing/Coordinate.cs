using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements.Drawing
{
    public class Coordinate(Number x, Number y)
    {
        public Number X { get; set; } = x ?? throw new ArgumentNullException(nameof(x));
        public Number Y { get; set; } = y ?? throw new ArgumentNullException(nameof(y));

        public static Coordinate Zero => new(0, 0);
    }
}
