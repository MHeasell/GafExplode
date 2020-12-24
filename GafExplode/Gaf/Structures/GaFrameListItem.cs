namespace GafExplode.Gaf.Structures
{
    using System.IO;

    public struct GaFrameListItem
    {
        /// <summary>
        /// Pointer to frame info.
        /// </summary>
        public uint PtrFrameInfo;

        /// <summary>
        /// The duration to display the frame for in game ticks.
        /// One game tick is 1/30th of a second.
        /// </summary>
        public uint Duration;

        public static void Read(BinaryReader b, ref GaFrameListItem e)
        {
            e.PtrFrameInfo = b.ReadUInt32();
            e.Duration = b.ReadUInt32();
        }

        public void Write(BinaryWriter b)
        {
            b.Write(this.PtrFrameInfo);
            b.Write(this.Duration);
        }
    }
}
