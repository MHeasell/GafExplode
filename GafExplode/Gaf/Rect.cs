using System;

namespace GafExplode.Gaf
{
    public struct Rect
    {
        public static Rect Merge(Rect a, Rect b)
        {
            var minX = Math.Min(a.X, b.X);
            var minY = Math.Min(a.Y, b.Y);

            var maxX = Math.Max(a.X + a.Width, b.X + b.Width);
            var maxY = Math.Max(a.Y + a.Height, b.Y + b.Height);

            return new Rect
            {
                X = minX,
                Y = minY,
                Width = maxX - minX,
                Height = maxY - minY,
            };
        }

        public Rect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; set; }
        public int Y { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
    }
}
