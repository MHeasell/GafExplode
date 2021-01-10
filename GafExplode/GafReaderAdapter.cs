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

        /// <summary>
        /// If true, we pad the dimensions of layers so that all the layers are the same
        /// size as the frame.
        /// If false we extract the raw images with no padding.
        /// </summary>
        private readonly bool padLayers = true;

        public List<GafSequenceJson> entries = new List<GafSequenceJson>();

        private Gaf.Structures.GafFrameInfo currentFrameInfo;
        private Gaf.Structures.GafFrameInfo currentLayerInfo;

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
            if (frameDepth > 0)
            {
                this.currentLayerInfo = data;

                var layerJson = new GafLayerJson
                {
                    TransparencyIndex = data.TransparencyIndex,
                    Unknown3 = data.Unknown3,
                };
                if (!padLayers)
                {
                    layerJson.OriginX = data.OriginX;
                    layerJson.OriginY = data.OriginY;
                }
                entries.Last().Frames.Last().Layers.Add(layerJson);
            }
            else
            {
                this.currentFrameInfo = data;

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
            var frameNumber = entries.Last().Frames.Count;

            if (frameDepth > 1)
            {
                var frameInfo = entries.Last().Frames.Last();
                var layerInfo = frameInfo.Layers.Last();
                var originX = this.currentLayerInfo.OriginX - this.currentFrameInfo.OriginX;
                var originY = this.currentLayerInfo.OriginY - this.currentFrameInfo.OriginY;
                var layerNumber = entries.Last().Frames.Last().Layers.Count;
                var relativePath = Path.Combine($"{sequenceNumber:D2}_{sequenceName}", $"{sequenceName}_{frameNumber:D2}_{layerNumber:D2}.png");
                entries.Last().Frames.Last().Layers.Last().ImageFileName = relativePath;
                var fullPath = Path.Combine(outputDirectory, relativePath);

                using (var bitmap = BitmapConvert.ToBitmap(data, this.currentLayerInfo.Width, this.currentLayerInfo.Height, this.paletteLookup))
                {
                    if (padLayers)
                    {
                        using (var frameBitmap = new Bitmap(this.currentFrameInfo.Width, this.currentFrameInfo.Height))
                        {
                            using (var graphics = Graphics.FromImage(frameBitmap))
                            {
                                using (var brush = new SolidBrush(paletteLookup(this.currentLayerInfo.TransparencyIndex)))
                                {
                                    graphics.FillRectangle(brush, 0, 0, frameBitmap.Size.Width, frameBitmap.Size.Height);
                                }
                                graphics.DrawImage(bitmap, new Point(-originX, -originY));
                            }
                            frameBitmap.Save(fullPath, ImageFormat.Png);
                        }
                    }
                    else
                    {
                        bitmap.Save(fullPath, ImageFormat.Png);
                    }
                }
            }
            else
            {
                using (var bitmap = BitmapConvert.ToBitmap(data, this.currentFrameInfo.Width, this.currentFrameInfo.Height, this.paletteLookup))
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

