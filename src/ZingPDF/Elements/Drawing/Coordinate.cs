namespace ZingPDF.Elements.Drawing
{
    public class Coordinate(double x, double y)
    {
        public double X { get; set; } = x;
        public double Y { get; set; } = y;

        public static Coordinate Zero => new(0, 0);
    }
}
