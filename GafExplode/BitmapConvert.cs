using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace GafExplode
{
    public static class BitmapConvert
    {
        public static byte[] ToBytes(Bitmap bitmap, Func<Color, byte> paletteLookup)
        {
            var s = new MemoryStream();
            Serialize(s, bitmap, paletteLookup);
            return s.ToArray();
        }

        public static void Serialize(Stream output, Bitmap bitmap, Func<Color, byte> paletteLookup)
        {
            Rectangle r = new Rectangle(0, 0, bitmap.Width, bitmap.Height);
            BitmapData data = bitmap.LockBits(r, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int length = bitmap.Width * bitmap.Height;

            unsafe
            {
                int* pointer = (int*)data.Scan0;
                for (int i = 0; i < length; i++)
                {
                    Color c = Color.FromArgb(pointer[i]);
                    output.WriteByte(paletteLookup(c));
                }
            }

            bitmap.UnlockBits(data);
        }

        public static Bitmap ToBitmap(byte[] bytes, int width, int height, Func<int, Color> paletteLookup)
        {
            return Deserialize(new MemoryStream(bytes), width, height, paletteLookup);
        }

        public static Bitmap Deserialize(Stream bytes, int width, int height, Func<int, Color> paletteLookup)
        {
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            Rectangle rect = new Rectangle(new Point(0, 0), bitmap.Size);
            BitmapData data = bitmap.LockBits(rect, ImageLockMode.WriteOnly, bitmap.PixelFormat);

            int length = width * height;

            unsafe
            {
                int* pointer = (int*)data.Scan0;
                for (int i = 0; i < length; i++)
                {
                    int readByte = bytes.ReadByte();
                    pointer[i] = paletteLookup(readByte).ToArgb();
                }
            }

            bitmap.UnlockBits(data);

            return bitmap;
        }
    }
}
