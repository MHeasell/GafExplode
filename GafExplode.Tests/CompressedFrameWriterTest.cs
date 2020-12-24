using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using GafExplode.Gaf;
using FsCheck;
using System.Linq;

namespace GafExplode.Tests
{
    [TestClass]
    public class CompressedFrameWriterTest
    {
        private static readonly Arbitrary<Tuple<int, byte[]>> WidthAlignedItems = Arb.From<Tuple<int, byte[]>>()
            .Filter(tuple => {
                var (width, data) = tuple;
                return width > 0 && width <= data.Length;
            })
            .MapFilter(
                tuple => {
                    var (width, data) = tuple;
                    var choppedLen = width * (data.Length / width);
                    return Tuple.Create(width, data.Take(choppedLen).ToArray());
                },
                tuple =>
                {
                    var (width, data) = tuple;
                    return data.Length % width == 0;
                });

        private static byte[] CompressImage(byte transparencyIndex, int width, byte[] data)
        {
            var compressedDataStream = new MemoryStream();
            CompressedFrameWriter.WriteCompressedImage(new MemoryStream(data, false), new BinaryWriter(compressedDataStream), width, transparencyIndex);
            return compressedDataStream.ToArray();
        }

        private static byte[] CompressAndUncompressImage(byte transparencyIndex, int width, byte[] data)
        {

            var compressedData = CompressImage(transparencyIndex, width, data);
            return CompressedFrameReader.ReadCompressedImage(new BinaryReader(new MemoryStream(compressedData, false)), width, data.Length / width, transparencyIndex);
        }

        private static byte[] CompressRow(byte transparencyIndex, byte[] data)
        {
            var compressedDataStream = new MemoryStream();
            CompressedFrameWriter.CompressRow(new MemoryStream(data, false), compressedDataStream, transparencyIndex);
            return compressedDataStream.ToArray();
        }

        private static byte[] CompressAndUncompressRow(byte transparencyIndex, byte[] data)
        {
            var compressedData = CompressRow(transparencyIndex, data);
            return UncompressRow(transparencyIndex, data.Length, compressedData);
        }

        private static byte[] UncompressRow(byte transparencyIndex, int rowLength, byte[] compressedData)
        {
            var uncompressedDataStream = new MemoryStream();
            CompressedFrameReader.DecompressRow(compressedData, uncompressedDataStream, rowLength, transparencyIndex);
            return uncompressedDataStream.ToArray();
        }

        [TestMethod]
        public void TestWriteReadRowIdentity()
        {
            Prop.ForAll<byte, byte[]>((transparencyIndex, data) =>
            {
                var uncompressedData = CompressAndUncompressRow(transparencyIndex, data);

                return uncompressedData.SequenceEqual(data).Collect(data.Length);
            }).QuickCheckThrowOnFailure();
        }

        [TestMethod]
        public void TestWriteReadImageIdentity()
        {
            Prop.ForAll<byte>(transparencyIndex => Prop.ForAll(WidthAlignedItems, tuple =>
            {
                var (width, data) = tuple;
                var uncompressedData = CompressAndUncompressImage(transparencyIndex, width, data);
                return uncompressedData.SequenceEqual(data).Collect($"width: {width}, height: {data.Length/width}, len: {data.Length}");
            })).QuickCheckThrowOnFailure();
        }

        [TestMethod]
        public void TestSimpleImage()
        {
            var transparencyIndex = (byte)231;
            var width = 1;
            var data = new byte[] { 1 };
            var uncompressedData = CompressAndUncompressImage(transparencyIndex, width, data);

            CollectionAssert.AreEqual(data, uncompressedData);
        }

        [TestMethod]
        public void TestTransparentImage()
        {
            var transparencyIndex = (byte)9;
            var width = 4;
            var data = new byte[] {
                9, 9, 9, 9,
                9, 9, 9, 9,
                9, 9, 9, 9,
            };
            var compressedData = CompressImage(transparencyIndex, width, data);

            var expectedData = new byte[]
            {
                0, 0,
                0, 0,
                0, 0
            };

            CollectionAssert.AreEqual(expectedData, compressedData);
        }

        [TestMethod]
        public void TestLongRow()
        {
            var data = new byte[250];
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = 50;
            }
            data[data.Length - 1] = 6;
            var compressedData = CompressRow(9, data);

            var expectedData = new byte[]
            {
                254, 50,
                254, 50,
                254, 50,
                226, 50,
                0, 6,
            };

            CollectionAssert.AreEqual(expectedData, compressedData);
            CollectionAssert.AreEqual(data, UncompressRow(9, data.Length, compressedData));
        }

        [TestMethod]
        public void TestLongNonRepeatingRow()
        {
            var data = new byte[250];
            for (var i = 0; i < data.Length; ++i)
            {
                data[i] = (byte)(i+1);
            }
            var compressedData = CompressRow(0, data);

            var expectedData = new byte[254];
            expectedData[0] = 252;
            Array.Copy(data, 0, expectedData, 1, 64);
            expectedData[65] = 252;
            Array.Copy(data, 64, expectedData, 66, 64);
            expectedData[130] = 252;
            Array.Copy(data, 128, expectedData, 131, 64);
            expectedData[195] = 228;
            Array.Copy(data, 192, expectedData, 196, 58);

            CollectionAssert.AreEqual(expectedData, compressedData);
            CollectionAssert.AreEqual(data, UncompressRow(9, data.Length, compressedData));
        }

        [TestMethod]
        public void TestUncompressThisRow()
        {
            var data = new byte[]
            {
                0x09,
                0x00,
                0x2e,
                0x07,
                0x00,
                0xad,
                0x03,
                0x00,
                0x2e,
                0x03,
                0x10,
                0xaf,
                0xf5,
                0x2f,
                0xaf,
                0xf5,
                0x03,

                0x0c,
                0xf5,
                0xaf,
                0x5f,
                0x5f,
                0x0b,
            };

            var data2 = new byte[]
            {
                0x09,
                0x00,
                0x2e,
                0x07,
                0x00,
                0xad,
                0x03,
                0x00,
                0x2e,
                0x03,
                0x10,
                0xaf,
                0xf5,
                0x2f,
                0xaf,
                0xf5,
                0x03,

                0x04,
                0xf5,
                0xaf,
                0x06,
                0x5f,
                0x0b,
            };

            var s = new MemoryStream();
            CompressedFrameReader.DecompressRow(data, s, 27, 9);
            var ss = s.ToArray();

            var s2 = new MemoryStream();
            CompressedFrameReader.DecompressRow(data2, s2, 27, 9);
            var ss2 = s.ToArray();
            CollectionAssert.AreEqual(ss, ss2);
        }
    }
}
