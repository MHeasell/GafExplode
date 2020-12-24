namespace GafExplode.Gaf
{
    using System;
    using System.IO;

    public static class CompressedFrameReader
    {
        public static byte[] ReadCompressedImage(BinaryReader input, int width, int height, byte transparencyIndex)
        {
            byte[] data = new byte[width * height];

            var s = new MemoryStream(data, true);

            ReadCompressedImage(input, s, width, height, transparencyIndex);

            return data;
        }

        private static void RepeatByte(Stream output, byte value, int count)
        {
            for (int i = 0; i < count; i++)
            {
                output.WriteByte(value);
            }
        }

        private static void ReadCompressedImage(BinaryReader input, Stream output, int width, int height, byte transparencyIndex)
        {
            for (int i = 0; i < height; i++)
            {
                ReadCompressedRow(input, output, width, transparencyIndex);
            }
        }

        private static void ReadCompressedRow(BinaryReader reader, Stream output, int rowLength, byte transparencyIndex)
        {
            int bytes = reader.ReadUInt16();

            byte[] compressedRow = reader.ReadBytes(bytes);

            DecompressRow(compressedRow, output, rowLength, transparencyIndex);
        }

        public static void DecompressRow(byte[] compressedRow, Stream output, int rowLength, byte transparencyIndex)
        {
            var stream = new MemoryStream(compressedRow);
            var reader = new BinaryReader(stream);

            int bytesLeft = rowLength;

            while (bytesLeft > 0 && stream.Position < compressedRow.Length)
            {
                bytesLeft -= DecompressBlock(reader, output, bytesLeft, transparencyIndex);
            }

            // Make up for any missing bytes if the row wasn't long enough
            // after being decompressed.
            RepeatByte(output, transparencyIndex, bytesLeft);
        }

        private static int DecompressBlock(BinaryReader input, Stream output, int limit, byte transparencyIndex)
        {
            // read the mask
            byte mask = input.ReadByte();

            if ((mask & 0x01) == 0x01)
            {
                // skip n pixels (transparency)
                int count = Math.Min(mask >> 1, limit);
                RepeatByte(output, transparencyIndex, count);
                return count;
            }

            if ((mask & 0x02) == 0x02)
            {
                // repeat this byte n times
                int count = Math.Min((mask >> 2) + 1, limit);
                byte val = input.ReadByte();
                RepeatByte(output, val, count);
                return count;
            }

            // by default, copy next n bytes
            int bytesToRead = (mask >> 2) + 1;
            byte[] buffer = input.ReadBytes(bytesToRead);

            int bytesToWrite = Math.Min(bytesToRead, limit);
            output.Write(buffer, 0, bytesToWrite);

            return bytesToWrite;
        }
    }
}