using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GafExplode.Gaf
{
    static class Util
    {
        public static string ConvertChars(byte[] data)
        {
            int i = Array.IndexOf<byte>(data, 0);

            if (i == -1)
            {
                i = data.Length;
            }

            return System.Text.Encoding.ASCII.GetString(data, 0, i);
        }

        public static byte[] UnconvertChars(string data, int length)
        {
            var bytes = new byte[length];
            if (data.Length >= length)
            {
                System.Text.Encoding.ASCII.GetBytes(data, 0, length, bytes, 0);
            }
            else
            {
                System.Text.Encoding.ASCII.GetBytes(data, 0, data.Length, bytes, 0);
                for (var i = data.Length; i < length; ++i)
                {
                    bytes[i] = 0;
                }
            }
            return bytes;
        }


        public static IEnumerable<int> BackwardsRange(int start, int count)
        {
            for (var i = start + count - 1; i >= start; --i)
            {
                yield return i;
            }
        }

        public static Rect ComputeMinBoundingRect(GafImageInfo imageInfo)
        {
            // FIXME: this will blow up if the image is entirely blank
            var top = Enumerable.Range(0, imageInfo.Height)
                .First(y => Enumerable.Range(0, imageInfo.Width)
                    .Any(x => imageInfo.Data[(y * imageInfo.Width) + x] != imageInfo.TransparencyIndex));

            var left = Enumerable.Range(0, imageInfo.Width)
                .First(x => Enumerable.Range(0, imageInfo.Height)
                    .Any(y => imageInfo.Data[(y * imageInfo.Width) + x] != imageInfo.TransparencyIndex));

            var bottom = BackwardsRange(0, imageInfo.Height)
                .First(y => Enumerable.Range(0, imageInfo.Width)
                    .Any(x => imageInfo.Data[(y * imageInfo.Width) + x] != imageInfo.TransparencyIndex));

            var right = BackwardsRange(0, imageInfo.Width)
                .First(x => Enumerable.Range(0, imageInfo.Height)
                    .Any(y => imageInfo.Data[(y * imageInfo.Width) + x] != imageInfo.TransparencyIndex));

            return new Rect(left, top, right - left + 1, bottom - top + 1);
        }

        public static GafImageInfo TrimImageInfo(GafImageInfo imageInfo, Rect rect)
        {
            var newData = new byte[rect.Width * rect.Height];
            for (var y = 0; y < rect.Height; ++y)
            {
                for (var x = 0; x < rect.Width; ++x)
                {
                    var sourceY = rect.Y + y;
                    var sourceX = rect.X + x;
                    newData[(y * rect.Width) + x] = imageInfo.Data[(sourceY * imageInfo.Width) + sourceX];
                }
            }
            return new GafImageInfo
            {
                Data = newData,
                Width = rect.Width,
                Height = rect.Height,
                TransparencyIndex = imageInfo.TransparencyIndex,
                Compress = imageInfo.Compress
            };
        }
    }
}
