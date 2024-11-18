using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements
{
    public class Rotation
    {
        private readonly int _amount;

        private Rotation(int amount)
        {
            if (amount % 90 != 0) throw new ArgumentOutOfRangeException(nameof(amount), "Rotation value must be a multiple of 90");

            _amount = amount;
        }

        public static readonly Rotation Degrees90 = new(90);
        public static readonly Rotation Degrees180 = new(180);
        public static readonly Rotation Degrees270 = new(270);

        public static Rotation FromValue(int value) => new(value);

        public static implicit operator Integer(Rotation rotation) => new(rotation._amount);
        public static implicit operator Rotation(Integer rotation) => new(rotation);

        public static Rotation operator +(Integer a, Rotation b) => new((int)a.Value + b._amount);
    }
}
