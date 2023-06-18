using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace GafExplode.Gaf
{
    class WrittenImageInfo
    {
        public long Pointer { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int TransparencyIndex { get; set; }
        public bool Compressed { get; set; }
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
                var pos = writer.BaseStream.Position;
                if (imageInfo.Compress)
                {
                    CompressedFrameWriter.WriteCompressedImage(new MemoryStream(imageInfo.Data), this.writer, imageInfo.Width, (byte)imageInfo.TransparencyIndex);
                }
                else
                {
                    writer.Write(imageInfo.Data);
                }
                writtenImageInfos.Add(new WrittenImageInfo
                {
                    Width = imageInfo.Width,
                    Height = imageInfo.Height,
                    TransparencyIndex = imageInfo.TransparencyIndex,
                    Compressed = imageInfo.Compress,
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
                OriginX = (short)info.OriginX,
                OriginY = (short)info.OriginY,
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
                header.OriginX = (short)info.OriginX;
                header.OriginY = (short)info.OriginY;
                header.Width = (ushort)imageInfo.Width;
                header.Height = (ushort)imageInfo.Height;
                header.Compressed = imageInfo.Compressed;
                header.TransparencyIndex = (byte)imageInfo.TransparencyIndex;
                header.PtrFrameData = (uint)imageInfo.Pointer;
            }
            else
            {
                var rect = info.Layers.Select(layer => new Rect
                {
                    X = -layer.OriginX,
                    Y = -layer.OriginY,
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
