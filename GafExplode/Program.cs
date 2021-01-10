using GafExplode.Gaf;
using GafExplode.Json;
using Newtonsoft.Json;
using System;
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
                PadImages(directoryName);
            }
            else if (args[0] == "explode-no-pad")
            {
                var filename = args[1];
                var directoryName = args[2];

                ExplodeGaf(filename, directoryName);
            }
            else if (args[0] == "unexplode-quantize")
            {
                var directoryName = args[1];
                var filename = args[2];
                UnexplodeGaf(directoryName, filename, true, true);
            }
            else if (args[0] == "unexplode")
            {
                var directoryName = args[1];
                var filename = args[2];
                UnexplodeGaf(directoryName, filename, true, false);
            }
            else if (args[0] == "unexplode-no-trim")
            {
                var directoryName = args[1];
                var filename = args[2];
                UnexplodeGaf(directoryName, filename, false, false);
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

        public static void UnexplodeGaf(string directoryName, string filename, bool trimFrames, bool quantize)
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
                var source = new DirectoryGafSource(directoryName, color =>  quantize ? (byte)GetNearest(reversePalette, palette, color) : reversePalette[color], trimFrames);
                var gafWriter = new Gaf.GafWriter(writer, source);
                gafWriter.Write();
            }
        }

        private static int GetNearest(Dictionary<Color, byte> reverse, Color[] arr, Color c)
        {
            if (reverse.TryGetValue(c, out var i))
            {
                return i;
            }
            return GetNearest(arr, c);
        }

        private static T MinBy<T, U>(IEnumerable<T> source, Func<T, U> keySelector)
        {
            var minElem = source.First();
            var minKey = keySelector(minElem);
            foreach (var elem in source.Skip(1))
            {
                var key = keySelector(elem);
                if (Comparer<U>.Default.Compare(key, minKey) < 0)
                {
                    minElem = elem;
                    minKey = key;
                }
            }
            return minElem;
        }

        private static int GetNearest(Color[] arr, Color c)
        {
            return MinBy(Enumerable.Range(0, arr.Length), i => DistanceSquared(arr[i], c));
        }

        private static int DistanceSquared(Color c1, Color c2)
        {
            var dr = c2.R - c1.R;
            var dg = c2.G - c1.G;
            var db = c2.B - c1.B;
            return (dr * dr) + (dg * dg) + (db * db);
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

        public static void PadImages(string directoryName)
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

