using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace GafExplode.Gaf
{
    struct RleInfo
    {
        public static RleInfo Empty = new RleInfo { Value = 0, RunLength = 0 };
        public byte Value { get; set; }
        public int RunLength { get; set; }
    }

    public static class CompressedFrameWriter
    {
        public static void WriteCompressedImage(Stream input, BinaryWriter output, int width, byte transparencyIndex)
        {
            var row = new byte[width];

            while (true)
            {
                var bytesRead = input.Read(row, 0, width);
                if (bytesRead == 0)
                {
                    return;
                }
                if (bytesRead != width)
                {
                    throw new Exception("Ran out of bytes in the middle of a row");
                }

                var compressedRowBuffer = new MemoryStream();
                CompressRow(new MemoryStream(row, false), compressedRowBuffer, transparencyIndex);
                var compressedRow = compressedRowBuffer.ToArray();
                output.Write((ushort)compressedRow.Length);
                output.Write(compressedRow, 0, compressedRow.Length);
            }
        }

        public static void CompressRow(Stream input, Stream output, byte transparencyIndex)
        {
            var byteBuffer = new List<byte>();
            RleInfo rleInfo = NextRleInfo(input);

            // If the row is completely transparent, skip writing it altogether.
            if (rleInfo.Value == transparencyIndex && input.Position == input.Length)
            {
                return;
            }

            while (rleInfo.RunLength > 0)
            {
                if (rleInfo.Value == transparencyIndex)
                {
                    DumpAllBuffer(byteBuffer, output);
                    byteBuffer.Clear();
                    rleInfo.RunLength = EncodeTransparencyRun(rleInfo.RunLength, output);
                    if (rleInfo.RunLength > 0) { continue; }
                }
                else if ((byteBuffer.Count == 0 && rleInfo.RunLength == 2) || rleInfo.RunLength >= 3)
                {
                    DumpAllBuffer(byteBuffer, output);
                    byteBuffer.Clear();
                    rleInfo.RunLength = EncodeValueRun(rleInfo.Value, rleInfo.RunLength, output);
                    if (rleInfo.RunLength > 0) { continue; }
                }
                else
                {
                    for (var i = 0; i < rleInfo.RunLength; ++i)
                    {
                        byteBuffer.Add(rleInfo.Value);
                    }
                }

                rleInfo = NextRleInfo(input);
            }

            DumpAllBuffer(byteBuffer, output);
        }

        private static void DumpAllBuffer(List<byte> byteBuffer, Stream writer)
        {
            for (var i = 0; i < byteBuffer.Count; i += 64)
            {
                var count = Math.Min(64, byteBuffer.Count - i);
                var firstByte = (byte)((count - 1) << 2);
                writer.WriteByte(firstByte);
                for (var j = 0; j < count; ++j)
                {
                    writer.WriteByte(byteBuffer[i + j]);
                }
            }
        }

        private static int EncodeValueRun(byte value, int runLength, Stream writer)
        {
            var count = Math.Min(runLength, 64);
            var firstByte = (byte)(((count - 1) << 2) | 2);
            writer.WriteByte(firstByte);
            writer.WriteByte(value);
            return runLength - count;
        }

        private static int EncodeTransparencyRun(int runLength, Stream writer)
        {
            var count = Math.Min(runLength, 127);
            var firstByte = (byte)((count << 1) | 1);
            writer.WriteByte(firstByte);
            return runLength - count;
        }

        private static RleInfo NextRleInfo(Stream input)
        {
            int prevValue = input.ReadByte();
            if (prevValue == -1)
            {
                return RleInfo.Empty;
            }

            var count = 1;
            int newValue;
            while ((newValue = input.ReadByte()) != -1)
            {
                if (newValue != prevValue)
                {
                    input.Seek(-1, SeekOrigin.Current);
                    return new RleInfo { Value = (byte)prevValue, RunLength = count };
                }
                count += 1;
            }

            return new RleInfo { Value = (byte)prevValue, RunLength = count };
        }
    }
}
