namespace GafExplode.Gaf.Structures
{
    using System.IO;

    public struct GafSequenceHeader
    {
        public ushort FrameCount;
        public ushort Unknown1;
        public uint Unknown2;
        public string Name;

        public static void Read(BinaryReader b, ref GafSequenceHeader entry)
        {
            entry.FrameCount = b.ReadUInt16();
            entry.Unknown1 = b.ReadUInt16();
            entry.Unknown2 = b.ReadUInt32();
            entry.Name = Util.ConvertChars(b.ReadBytes(32));
        }

        public void Write(BinaryWriter w)
        {
            w.Write(this.FrameCount);
            w.Write(this.Unknown1);
            w.Write(this.Unknown2);
            w.Write(Util.UnconvertChars(this.Name, 32));
        }
    }
}
