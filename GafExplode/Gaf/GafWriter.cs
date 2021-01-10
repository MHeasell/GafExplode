using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System;

namespace GafExplode.Gaf
{
    class WrittenImageInfo
    {
        public long Pointer { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int AdjustedOriginX { get; set; }
        public int AdjustedOriginY { get; set; }
        public int TransparencyIndex { get; set; }
    }

    class GafWriter
    {
        private readonly BinaryWriter writer;
        private readonly IGafSource source;

        public GafWriter(BinaryWriter writer, IGafSource source)
        {
            this.writer = writer;
            this.source = source;
        }

        public void Write()
        {
            var entries = source.EnumerateSequences().ToList();

            var header = new Structures.GafHeader
            {
                IdVersion = 0x00010100,
                SequenceCount = (uint)entries.Count,
                Unknown1 = 0
            };

            header.Write(writer);

            // reserve space for the entry pointers
            var entryPointersPos = writer.BaseStream.Position;
            for (int i = 0; i < entries.Count; ++i)
            {
                writer.Write(0);
            }

            // write all the images
            var writtenImageInfos = new List<WrittenImageInfo>();
            foreach (var imageInfo in source.EnumerateImageInfos())
            {
                // FIXME: images not deduped anymore... either move the dedupe here
                // or move the image trimming code out!
                var pos = writer.BaseStream.Position;
                var rect = ComputeMinBoundingRect(imageInfo);
                var smallerImageInfo = TrimImageInfo(imageInfo, rect);
                CompressedFrameWriter.WriteCompressedImage(new MemoryStream(smallerImageInfo.Data), this.writer, smallerImageInfo.Width, (byte)smallerImageInfo.TransparencyIndex);
                writtenImageInfos.Add(new WrittenImageInfo
                {
                    Height = smallerImageInfo.Height,
                    Width = smallerImageInfo.Width,
                    AdjustedOriginX = -rect.X,
                    AdjustedOriginY = -rect.Y,
                    TransparencyIndex = smallerImageInfo.TransparencyIndex,
                    Pointer = pos
                });
            }


            // write all the sequences
            var entryPointers = new List<long>();
            foreach (var entry in entries)
            {
                entryPointers.Add(writer.BaseStream.Position);
                this.WriteGafEntry(writtenImageInfos, entry);
            }

            // go back and fill in the entry pointers
            var currPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(entryPointersPos, SeekOrigin.Begin);
            foreach (var p in entryPointers)
            {
                writer.Write((uint)p);
            }

            // reset seek pos forward to after the last byte we wrote
            writer.BaseStream.Seek(currPos, SeekOrigin.Begin);
        }

        private static GafImageInfo TrimImageInfo(GafImageInfo imageInfo, Rect rect)
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
                TransparencyIndex = imageInfo.TransparencyIndex
            };
        }

        private static IEnumerable<int> BackwardsRange(int start, int count)
        {
            for (var i = start + count - 1; i >= start; --i)
            {
                yield return i;
            }
        }

        private Rect ComputeMinBoundingRect(GafImageInfo imageInfo)
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

        private void WriteGafEntry(List<WrittenImageInfo> imageInfos, GafEntryInfo entryInfo)
        {
            var header = new Structures.GafSequenceHeader
            {
                FrameCount = (ushort)entryInfo.FrameInfos.Count,
                Unknown1 = 1,
                Unknown2 = 0,
                Name = entryInfo.Name
            };
            header.Write(writer);

            // reserve space for frame pointers
            var framePointersPosition = writer.BaseStream.Position;
            foreach (var _ in entryInfo.FrameInfos)
            {
                var entry = new Structures.GaFrameListItem
                {
                    PtrFrameInfo = 0,
                    Duration = 0,
                };
                entry.Write(writer);
            }

            // write frames
            var pointers = new List<long>();
            foreach (var frameInfo in entryInfo.FrameInfos)
            {
                pointers.Add(writer.BaseStream.Position);
                WriteFrameInfo(imageInfos, frameInfo);
            }

            // go back and write frame pointers
            var currPos = writer.BaseStream.Position;
            writer.BaseStream.Seek(framePointersPosition, SeekOrigin.Begin);

            // write pointers
            for (var i = 0; i < entryInfo.FrameInfos.Count; ++i)
            {
                var entry = new Structures.GaFrameListItem
                {
                    PtrFrameInfo = (uint)pointers[i],
                    Duration = (uint)entryInfo.FrameInfos[i].Duration,
                };
                entry.Write(writer);
            }

            // reset to end
            writer.BaseStream.Seek(currPos, SeekOrigin.Begin);
        }

        private void WriteLayerInfo(List<WrittenImageInfo> imageInfos, GafLayerInfo info)
        {
            var imageInfo = imageInfos[info.ImageIndex];

            var header = new Structures.GafFrameInfo
            {
                OriginX = (short)(info.OriginX + imageInfo.AdjustedOriginX),
                OriginY = (short)(info.OriginY + imageInfo.AdjustedOriginY),
                Unknown2 = 0,
                Unknown3 = (uint)info.Unknown3, // Cavedog gafs sometimes have a value here but we don't know what it does.

                LayerCount = 0,
                Width = (ushort)imageInfo.Width,
                Height = (ushort)imageInfo.Height,
                Compressed = true,
                TransparencyIndex = (byte)imageInfo.TransparencyIndex,
                PtrFrameData = (uint)imageInfo.Pointer,
            };

            header.Write(writer);
        }

        private void WriteFrameInfo(List<WrittenImageInfo> imageInfos, GafFrameInfo info)
        {
            var header = new Structures.GafFrameInfo
            {
                Unknown2 = 0,
                Unknown3 = (uint)info.Unknown3, // Cavedog gafs sometimes have a value here but we don't know what it does.
            };

            if (info.ImageIndex.HasValue)
            {
                var imageInfo = imageInfos[info.ImageIndex.Value];
                header.LayerCount = 0;
                header.OriginX = (short)(info.OriginX + imageInfo.AdjustedOriginX);
                header.OriginY = (short)(info.OriginY + imageInfo.AdjustedOriginY);
                header.Width = (ushort)imageInfo.Width;
                header.Height = (ushort)imageInfo.Height;
                header.Compressed = true;
                header.TransparencyIndex = (byte)imageInfo.TransparencyIndex;
                header.PtrFrameData = (uint)imageInfo.Pointer;
            }
            else
            {
                var rect = info.Layers.Select(layer => new Rect
                {
                    X = -(layer.OriginX + imageInfos[layer.ImageIndex].AdjustedOriginX),
                    Y = -(layer.OriginY + imageInfos[layer.ImageIndex].AdjustedOriginY),
                    Width = imageInfos[layer.ImageIndex].Width,
                    Height = imageInfos[layer.ImageIndex].Height
                }).Aggregate(Rect.Merge);

                header.LayerCount = (ushort)info.Layers.Count;
                header.OriginX = (short)(-rect.X);
                header.OriginY = (short)(-rect.Y);
                header.Width = (ushort)rect.Width;
                header.Height = (ushort)rect.Height;
                header.Compressed = false;
                header.TransparencyIndex = 9;
                header.PtrFrameData = (uint)(writer.BaseStream.Position + 24);
            }

            header.Write(writer);

            if (!info.ImageIndex.HasValue)
            {
                var startPos = writer.BaseStream.Position + (4 * info.Layers.Count);
                for (var i = 0; i < info.Layers.Count; ++i)
                {
                    writer.Write((uint)(startPos + (24 * i)));
                }
                var layerPointers = new List<long>();
                foreach (var layer in info.Layers)
                {
                    WriteLayerInfo(imageInfos, layer);
                }
            }
        }
    }
}
