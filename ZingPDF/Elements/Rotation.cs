using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements
{
    public class Rotation
    {
        private Rotation(int value)
        {
            if (value % 90 != 0) throw new ArgumentOutOfRangeException(nameof(value), "Rotation value must be a multiple of 90");

            Value = value;
        }

        public int Value { get; }

        public static readonly Rotation None = new(0);
        public static readonly Rotation Degrees90 = new(90);
        public static readonly Rotation Degrees180 = new(180);
        public static readonly Rotation Degrees270 = new(270);

        public static Rotation FromValue(int value) => new(value);

        public static implicit operator Rotation(Number rotation) => new(rotation);

        public static Rotation operator +(Number a, Rotation b) => new((int)a.Value + b.Value);
        public static Rotation operator +(Rotation a, Rotation b) => new(a.Value + b.Value);
    }
}
