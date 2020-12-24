namespace GafExplode.Gaf.Structures
{
    using System.IO;

    public struct GafHeader
    {
        /// <summary>
        /// Version stamp - always 0x00010100.
        /// </summary>
        public uint IdVersion;

        /// <summary>
        /// Number of sequences contained in this file.
        /// </summary>
        public uint SequenceCount;

        /// <summary>
        /// Purpose unknown. Always 0.
        /// </summary>
        public uint Unknown1;

        public static void Read(BinaryReader b, ref GafHeader header)
        {
            header.IdVersion = b.ReadUInt32();
            header.SequenceCount = b.ReadUInt32();
            header.Unknown1 = b.ReadUInt32();
        }

        public void Write(BinaryWriter b)
        {
            b.Write(this.IdVersion);
            b.Write(this.SequenceCount);
            b.Write(this.Unknown1);
        }
    }
}
