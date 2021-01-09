using System.Drawing.Imaging;
using System.IO;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using GafExplode.Json;
using System.Drawing;
using System;

namespace GafExplode
{

    class GafReaderAdapter : Gaf.IGafReaderAdapter
    {
        private readonly string outputDirectory;
        private readonly Func<int, Color> paletteLookup;

        public List<GafSequenceJson> entries = new List<GafSequenceJson>();

        private int frameWidth;
        private int frameHeight;

        private int frameDepth = 0;

        public GafReaderAdapter(string outputDirectory, Func<int, Color> paletteLookup)
        {
            this.outputDirectory = outputDirectory;
            this.paletteLookup = paletteLookup;
        }

        public void BeginRead(long entryCount)
        {
        }

        public void BeginEntry(ref Gaf.Structures.GafSequenceHeader entry)
        {
            entries.Add(new GafSequenceJson { Frames = new List<GafFrameJson>(), Name = entry.Name });
            var entryNumber = entries.Count;
            Directory.CreateDirectory(Path.Combine(outputDirectory, $"{entryNumber:D2}_{entry.Name}"));
        }

        public void BeginFrame(ref Gaf.Structures.GaFrameListItem entry, ref Gaf.Structures.GafFrameInfo data)
        {
            frameWidth = data.Width;
            frameHeight = data.Height;

            if (frameDepth > 0)
            {
                var layerJson = new GafLayerJson
                {
                    OriginX = data.OriginX,
                    OriginY = data.OriginY,
                    TransparencyIndex = data.TransparencyIndex,
                    Unknown3 = data.Unknown3,
                };
                entries.Last().Frames.Last().Layers.Add(layerJson);
            }
            else
            {
                var frameJson = new GafFrameJson
                {
                    Duration = entry.Duration,
                    Unknown3 = data.Unknown3,

                };
                if (data.LayerCount > 0)
                {
                    frameJson.Layers = new List<GafLayerJson>();
                }
                else
                {
                    frameJson.OriginX = data.OriginX;
                    frameJson.OriginY = data.OriginY;
                    frameJson.TransparencyIndex = data.TransparencyIndex;
                }
                entries.Last().Frames.Add(frameJson);
            }

            frameDepth += 1;
        }

        public void SetFrameData(byte[] data)
        {
            var sequenceName = entries.Last().Name;
            var sequenceNumber = entries.Count;
            using (var bitmap = BitmapConvert.ToBitmap(data, this.frameWidth, this.frameHeight, this.paletteLookup))
            {
                var frameNumber = entries.Last().Frames.Count;
                if (frameDepth > 1)
                {
                    var layerNumber = entries.Last().Frames.Last().Layers.Count;
                    var relativePath = Path.Combine($"{sequenceNumber:D2}_{sequenceName}", $"{sequenceName}_{frameNumber:D2}_{layerNumber:D2}.png");
                    entries.Last().Frames.Last().Layers.Last().ImageFileName = relativePath;
                    bitmap.Save(Path.Combine(outputDirectory, relativePath), ImageFormat.Png);
                }
                else
                {
                    var relativePath = Path.Combine($"{sequenceNumber:D2}_{sequenceName}", $"{sequenceName:D2}_{frameNumber:D2}.png");
                    entries.Last().Frames.Last().ImageFileName = relativePath;
                    bitmap.Save(Path.Combine(outputDirectory, relativePath), ImageFormat.Png);
                }
            }
        }

        public void EndFrame()
        {
            frameDepth -= 1;
        }

        public void EndEntry()
        {
        }

        public void EndRead()
        {
            File.WriteAllText(Path.Combine(outputDirectory, "gaf.json"), JsonConvert.SerializeObject(entries, Formatting.Indented));
        }
    }
}

