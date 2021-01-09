using System.Drawing;
using System.IO;

namespace GafExplode
{
    public static class Palette
    {
        public static Color[] Read(string filename)
        {
            using (var s = File.OpenRead(filename))
            {
                return Read(s);
            }
        }

        public static Color[] Read(Stream s)
        {
            var arr = new Color[256];
            for (var i = 0; i < 256; ++i)
            {
                arr[i] = ReadColor(s);
            }
            return arr;
        }

        public static Color ReadColor(Stream s)
        {
            int r = s.ReadByte();
            int g = s.ReadByte();
            int b = s.ReadByte();

            // alpha, ignored
            s.ReadByte();

            return Color.FromArgb(r, g, b);
        }
    }
}
