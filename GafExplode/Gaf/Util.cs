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
    }
}
