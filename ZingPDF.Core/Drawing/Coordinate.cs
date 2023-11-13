using System;
using System.ComponentModel.DataAnnotations;

namespace ZingPdf.Core.Drawing
{
    public class Coordinate
    {
        [Obsolete("Reserved for deserialisation")]
        public Coordinate() { }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        [Required, Range(0, int.MaxValue)]
        public int X { get; set; }

        [Required, Range(0, int.MaxValue)]
        public int Y { get; set; }
    }
}
