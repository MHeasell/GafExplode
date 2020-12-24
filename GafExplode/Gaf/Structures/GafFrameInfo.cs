namespace GafExplode.Gaf.Structures
{
    using System.IO;

    public struct GafFrameInfo
    {
        public ushort Width;
        public ushort Height;
        public short OriginX;
        public short OriginY;
        public byte TransparencyIndex;
        public bool Compressed;
        public ushort LayerCount;
        public uint Unknown2;
        public uint PtrFrameData;
        public uint Unknown3;

        public static void Read(BinaryReader b, ref GafFrameInfo e)
        {
            e.Width = b.ReadUInt16();
            e.Height = b.ReadUInt16();
            e.OriginX = b.ReadInt16();
            e.OriginY = b.ReadInt16();
            e.TransparencyIndex = b.ReadByte();
            e.Compressed = b.ReadBoolean();
            e.LayerCount = b.ReadUInt16();
            e.Unknown2 = b.ReadUInt32();
            e.PtrFrameData = b.ReadUInt32();
            e.Unknown3 = b.ReadUInt32();
        }

        public void Write(BinaryWriter w)
        {
            w.Write(this.Width);
            w.Write(this.Height);
            w.Write(this.OriginX);
            w.Write(this.OriginY);
            w.Write(this.TransparencyIndex);
            w.Write(this.Compressed);
            w.Write(this.LayerCount);
            w.Write(this.Unknown2);
            w.Write(this.PtrFrameData);
            w.Write(this.Unknown3);
        }
    }
}
