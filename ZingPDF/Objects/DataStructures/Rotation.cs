using ZingPDF.Extensions;

namespace ZingPDF.Objects.DataStructures
{
    public class Rotation : PdfObject
    {
        private readonly int _amount;

        private Rotation(int amount)
        {
            if (amount % 90 != 0) throw new ArgumentOutOfRangeException(nameof(amount), "Rotation value must be a multiple of 90");

            _amount = amount;
        }

        protected override async Task WriteOutputAsync(Stream stream)
        {
            await stream.WriteIntAsync(_amount);
        }

        public static Rotation Degrees90 = new(90);
        public static Rotation Degrees180 = new(180);
        public static Rotation Degrees270 = new(270);

        public static Rotation FromValue(int value) => new(value);
    }
}
