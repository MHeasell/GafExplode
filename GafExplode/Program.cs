using GafExplode.Gaf;
using GafExplode.Json;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace GafExplode
{
    public class Program
    {
        static void Main(string[] args)
        {
            if (args[0] == "explode")
            {
                var filename = args[1];
                var directoryName = args[2];

                ExplodeGaf(filename, directoryName);
            }
            else if (args[0] == "unexplode")
            {
                var directoryName = args[1];
                var filename = args[2];
                UnexplodeGaf(directoryName, filename, true);
            }
            else if (args[0] == "unexplode-no-trim")
            {
                var directoryName = args[1];
                var filename = args[2];
                UnexplodeGaf(directoryName, filename, false);
            }
            else if(args[0] == "pad-frames")
            {
                var directoryName = args[1];
                PadFrames(directoryName);
            }
        }

        private static Color[] LoadPalette()
        {
            var exePath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
            var palette = Palette.Read(Path.Combine(exePath, "PALETTE.PAL"));
            return palette;
        }

        public static void ExplodeGaf(string filename, string directoryName)
        {
            var palette = LoadPalette();
            var adapter = new GafReaderAdapter(directoryName, idx => palette[idx]);
            using (var reader = new Gaf.GafReader(filename, adapter))
            {
                reader.Read();
            }
        }

        public static void UnexplodeGaf(string directoryName, string filename, bool trimFrames)
        {
            var reversePalette = new Dictionary<Color, byte>();
            var palette = LoadPalette();
            for (var i = 0; i < palette.Length; ++i)
            {
                // TA palette has some duplicate colors in it - just use the first one
                if (!reversePalette.ContainsKey(palette[i]))
                {
                    reversePalette[palette[i]] = (byte)i;
                }
            }

            using (var writer = new BinaryWriter(File.OpenWrite(filename)))
            {
                var source = new DirectoryGafSource(directoryName, color => reversePalette[color], trimFrames);
                var gafWriter = new Gaf.GafWriter(writer, source);
                gafWriter.Write();
            }
        }

        private static IEnumerable<Rect> EnumerateRects(string directoryName, GafSequenceJson sequence)
        {
            foreach (var frame in sequence.Frames)
            {
                if (frame.ImageFileName != null)
                {
                    using (var bitmap = new Bitmap(Path.Combine(directoryName, frame.ImageFileName)))
                    {
                        yield return new Rect(-frame.OriginX.Value, -frame.OriginY.Value, bitmap.Width, bitmap.Height);
                    }
                }
                else
                {
                    foreach (var layer in frame.Layers)
                    {
                        using (var bitmap = new Bitmap(Path.Combine(directoryName, layer.ImageFileName)))
                        {
                            yield return new Rect(-layer.OriginX, -layer.OriginY, bitmap.Width, bitmap.Height);
                        }
                    }
                }
            }
        }

        public static void PadFrames(string directoryName)
        {
            var reversePalette = new Dictionary<Color, byte>();
            var palette = LoadPalette();
            for (var i = 0; i < palette.Length; ++i)
            {
                // TA palette has some duplicate colors in it - just use the first one
                if (!reversePalette.ContainsKey(palette[i]))
                {
                    reversePalette[palette[i]] = (byte)i;
                }
            }


            var gafJsonPath = Path.Combine(directoryName, "gaf.json");
            var sequences = JsonConvert.DeserializeObject<List<GafSequenceJson>>(File.ReadAllText(gafJsonPath));

            foreach (var sequence in sequences)
            {
                var rect = EnumerateRects(directoryName, sequence).Aggregate(Rect.Merge);

                foreach (var frame in sequence.Frames)
                {
                    if (frame.ImageFileName != null)
                    {
                        var x = -frame.OriginX.Value - rect.X;
                        var y = -frame.OriginY.Value - rect.Y;
                        var fullPath = Path.Combine(directoryName, frame.ImageFileName);
                        using (var containerBitmap = new Bitmap(rect.Width, rect.Height))
                        {
                            using (var graphics = Graphics.FromImage(containerBitmap))
                            {
                                using (var brush = new SolidBrush(palette[frame.TransparencyIndex.Value]))
                                {
                                    graphics.FillRectangle(brush, 0, 0, containerBitmap.Size.Width, containerBitmap.Size.Height);
                                }
                                using (var bitmap = new Bitmap(fullPath))
                                {
                                    graphics.DrawImage(bitmap, new Point(x, y));
                                }
                            }
                            containerBitmap.Save(fullPath, ImageFormat.Png);
                        }
                        frame.OriginX = -rect.X;
                        frame.OriginY = -rect.Y;
                    }
                    else
                    {
                        foreach (var layer in frame.Layers)
                        {
                            var x = -layer.OriginX - rect.X;
                            var y = -layer.OriginY - rect.Y;
                            var fullPath = Path.Combine(directoryName, layer.ImageFileName);
                            using (var containerBitmap = new Bitmap(rect.Width, rect.Height))
                            {
                                using (var graphics = Graphics.FromImage(containerBitmap))
                                {
                                    using (var brush = new SolidBrush(palette[layer.TransparencyIndex]))
                                    {
                                        graphics.FillRectangle(brush, 0, 0, containerBitmap.Size.Width, containerBitmap.Size.Height);
                                    }
                                    using (var bitmap = new Bitmap(fullPath))
                                    {
                                        graphics.DrawImage(bitmap, new Point(x, y));
                                    }
                                }
                                containerBitmap.Save(fullPath, ImageFormat.Png);
                            }
                            layer.OriginX = -rect.X;
                            layer.OriginY = -rect.Y;
                        }
                    }
                }
            }

            File.WriteAllText(gafJsonPath, JsonConvert.SerializeObject(sequences, Formatting.Indented));
        }
    }
}

