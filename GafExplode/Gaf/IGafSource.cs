using System.Collections.Generic;

namespace GafExplode.Gaf
{
    public struct GafEntryInfo
    {
        public string Name { get; set; }
        public List<GafFrameInfo> FrameInfos { get; set; }
    }

    public struct GafFrameInfo
    {
        public long Duration { get; set; }

        public long Unknown3 { get; set; }

        public int? PosX { get; set; }
        public int? PosY { get; set; }
        public int? ImageIndex { get; set; }
        public List<GafLayerInfo> Layers { get; set; }
    }

    public struct GafLayerInfo
    {
        public int PosX { get; set; }
        public int PosY { get; set; }
        public int ImageIndex { get; set; }

        public long Unknown3 { get; set; }
    }

    public struct GafImageInfo
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int TransparencyIndex { get; set; }
        public byte[] Data { get; set; }
    }

    public interface IGafSource
    {
        IEnumerable<GafEntryInfo> EnumerateEntries();

        IEnumerable<GafImageInfo> EnumerateImageInfos();
    }
}
