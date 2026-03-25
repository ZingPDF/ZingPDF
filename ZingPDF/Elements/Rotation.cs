using ZingPDF.Syntax.Objects;

namespace ZingPDF.Elements
{
    /// <summary>
    /// Represents a page rotation value in 90-degree increments.
    /// </summary>
    public class Rotation
    {
        private Rotation(int value)
        {
            if (value % 90 != 0) throw new ArgumentOutOfRangeException(nameof(value), "Rotation value must be a multiple of 90");

            Value = value;
        }

        /// <summary>
        /// Gets the rotation angle in degrees.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// No page rotation.
        /// </summary>
        public static readonly Rotation None = new(0);

        /// <summary>
        /// A 90-degree clockwise rotation.
        /// </summary>
        public static readonly Rotation Degrees90 = new(90);

        /// <summary>
        /// A 180-degree rotation.
        /// </summary>
        public static readonly Rotation Degrees180 = new(180);

        /// <summary>
        /// A 270-degree clockwise rotation.
        /// </summary>
        public static readonly Rotation Degrees270 = new(270);

        /// <summary>
        /// Creates a rotation from a raw degree value.
        /// </summary>
        /// <param name="value">The rotation angle in degrees. Must be a multiple of 90.</param>
        public static Rotation FromValue(int value) => new(value);

        /// <summary>
        /// Converts a PDF numeric rotation value to a <see cref="Rotation"/> instance.
        /// </summary>
        public static implicit operator Rotation(Number rotation) => new(rotation);

        /// <summary>
        /// Adds a numeric rotation value to a <see cref="Rotation"/>.
        /// </summary>
        public static Rotation operator +(Number a, Rotation b) => new((int)a.Value + b.Value);

        /// <summary>
        /// Combines two rotation values.
        /// </summary>
        public static Rotation operator +(Rotation a, Rotation b) => new(a.Value + b.Value);
    }
}
