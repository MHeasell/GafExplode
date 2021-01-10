using GafExplode.Gaf;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Drawing;
using GafExplode.Json;
using System;

namespace GafExplode
{
    class GafImageInfoComparer : IEqualityComparer<GafImageInfo>
    {
        public bool Equals(GafImageInfo x, GafImageInfo y)
        {
            return x.Width == y.Width
                && x.Height == y.Height
                && x.TransparencyIndex == y.TransparencyIndex
                && x.Data.SequenceEqual(y.Data);
        }

        public int GetHashCode(GafImageInfo o)
        {
            var hashCode = o.Width.GetHashCode();
            hashCode = hashCode * 17 + o.Height.GetHashCode();
            hashCode = hashCode * 17 + o.TransparencyIndex.GetHashCode();
            foreach (var d in o.Data)
            {
                hashCode = hashCode * 17 + d.GetHashCode();
            }
            return hashCode;
        }
    }

    class ImageInfoDatabase : ItemDatabase<string, GafImageInfo, Point>
    {
    }

    class ItemDatabase<TKey, T, TMapping>
    {
        public List<T> Items { get; set; }
        public Dictionary<TKey, int> ItemsByKey { get; set; }
        public Dictionary<TKey, TMapping> MappingsByKey { get; set; }
    }

    public class DirectoryGafSource : IGafSource
    {
        private static GafImageInfo GetImageInfo(string filename, int transparencyIndex, Func<Color, byte> paletteLookup)
        {
            using (var bmp = new Bitmap(filename))
            {
                var data = BitmapConvert.ToBytes(bmp, paletteLookup);
                return new GafImageInfo
                {
                    Data = data,
                    Height = bmp.Height,
                    Width = bmp.Width,
                    TransparencyIndex = transparencyIndex,
                };
            }
        }

        private static IEnumerable<KeyValuePair<string, GafImageInfo>> GetImageInfos(string directoryName, GafFrameJson frame, Func<Color, byte> paletteLookup)
        {
            if (frame.ImageFileName != null)
            {
                var info = GetImageInfo(Path.Combine(directoryName, frame.ImageFileName), frame.TransparencyIndex.Value, paletteLookup);
                yield return new KeyValuePair<string, GafImageInfo>(frame.ImageFileName, info);
            }
            else
            {
                foreach (var layer in frame.Layers)
                {
                    var info = GetImageInfo(Path.Combine(directoryName, layer.ImageFileName), layer.TransparencyIndex, paletteLookup);
                    yield return new KeyValuePair<string, GafImageInfo>(layer.ImageFileName, info);
                }
            }
        }

        private static (List<T>, Dictionary<TKey, int>, Dictionary<TKey, TMapping>) TransformAndDeduplicate<TKey, T, TMapping>(
            IEnumerable<KeyValuePair<TKey, T>> items,
            Func<T, (T, TMapping)> transformer,
            IEqualityComparer<T> equalityComparer
        )
        {
            var deduplicatedList = new List<T>();
            var itemsByValue = new Dictionary<T, int>(equalityComparer);
            var itemsByKey = new Dictionary<TKey, int>();
            var mappingsByKey = new Dictionary<TKey, TMapping>();

            foreach (var pair in items)
            {
                var (transformedValue, mapping) = transformer(pair.Value);
                if (!itemsByValue.TryGetValue(transformedValue, out var index))
                {
                    index = deduplicatedList.Count;
                    deduplicatedList.Add(transformedValue);
                    itemsByValue.Add(transformedValue, index);
                }
                itemsByKey.Add(pair.Key, index);
                mappingsByKey.Add(pair.Key, mapping);
            }

            return (deduplicatedList, itemsByKey, mappingsByKey);
        }

        private static (List<T>, Dictionary<TKey, int>) Deduplicate<TKey, T>(IEnumerable<KeyValuePair<TKey, T>> items, IEqualityComparer<T> equalityComparer)
        {
            var deduplicatedList = new List<T>();
            var itemsByValue = new Dictionary<T, int>(equalityComparer);
            var itemsByKey = new Dictionary<TKey, int>();

            foreach (var pair in items)
            {
                if (!itemsByValue.TryGetValue(pair.Value, out var index))
                {
                    index = deduplicatedList.Count;
                    deduplicatedList.Add(pair.Value);
                    itemsByValue.Add(pair.Value, index);
                }
                itemsByKey.Add(pair.Key, index);
            }

            return (deduplicatedList, itemsByKey);
        }

        private static IEnumerable<KeyValuePair<string, GafImageInfo>> GenerateImageInfos(string directoryName, List<GafSequenceJson> entries, Func<Color, byte> paletteLookup)
        {
            return entries.SelectMany(entry => entry.Frames.SelectMany(frame => GetImageInfos(directoryName, frame, paletteLookup)));
        }

        private static ImageInfoDatabase GenerateImageInfoDatabase(string directoryName, List<GafSequenceJson> entries, Func<Color, byte> paletteLookup, bool trimFrames)
        {
            var (images, imagesByFilename, mappingsByFilename) = TransformAndDeduplicate(
                GenerateImageInfos(directoryName, entries, paletteLookup),
                imageInfo =>
                {
                    if (trimFrames)
                    {
                        var rect = Util.ComputeMinBoundingRect(imageInfo);
                        return (Util.TrimImageInfo(imageInfo, rect), new Point(-rect.X, -rect.Y));
                    }
                    else
                    {
                        return (imageInfo, new Point(0, 0));
                    }
                },
                new GafImageInfoComparer());
            return new ImageInfoDatabase { Items = images, ItemsByKey = imagesByFilename, MappingsByKey = mappingsByFilename };
        }

        private static GafLayerInfo GenerateLayerInfoFromFrame(Dictionary<string, int> imageInfoLookup, Dictionary<string, Point> originMappingLookup, GafLayerJson layer)
        {
            return new GafLayerInfo {
                ImageIndex = imageInfoLookup[layer.ImageFileName],
                OriginX = layer.OriginX + originMappingLookup[layer.ImageFileName].X,
                OriginY = layer.OriginY + originMappingLookup[layer.ImageFileName].Y,
                Unknown3 = layer.Unknown3,
            };
        }

        private static GafFrameInfo GenerateLayerInfosFromFrame(Dictionary<string, int> imageInfoLookup, Dictionary<string, Point> originMappingLookup, GafFrameJson frame)
        {
            var info = new GafFrameInfo
            {
                Duration = frame.Duration,
                Unknown3 = frame.Unknown3,
            };

            if (frame.ImageFileName == null)
            {
                info.Layers = frame.Layers.Select(x => GenerateLayerInfoFromFrame(imageInfoLookup, originMappingLookup, x)).ToList();
            }
            else
            {
                info.OriginX = frame.OriginX + originMappingLookup[frame.ImageFileName].X;
                info.OriginY = frame.OriginY + originMappingLookup[frame.ImageFileName].Y;
                info.ImageIndex = imageInfoLookup[frame.ImageFileName];
            }

            return info;
        }

        private static IEnumerable<GafEntryInfo> GenerateGafSequences(Dictionary<string, int> imageInfoLookup, Dictionary<string, Point> originMappingLookup, List<GafSequenceJson> entries)
        {
            return entries.Select(entry => new GafEntryInfo {
                Name = entry.Name,
                FrameInfos = entry.Frames.Select(frame => GenerateLayerInfosFromFrame(imageInfoLookup, originMappingLookup, frame)).ToList(),
            });
        }

        private readonly List<GafSequenceJson> sequences;
        private readonly ImageInfoDatabase imageDb;

        public DirectoryGafSource(string directoryName, Func<Color, byte> paletteLookup, bool trimFrames)
        {
            this.sequences = JsonConvert.DeserializeObject<List<GafSequenceJson>>(File.ReadAllText(Path.Combine(directoryName, "gaf.json")));
            this.imageDb = GenerateImageInfoDatabase(directoryName, this.sequences, paletteLookup, trimFrames);
        }

        public IEnumerable<GafEntryInfo> EnumerateSequences()
        {
            return GenerateGafSequences(this.imageDb.ItemsByKey, this.imageDb.MappingsByKey, this.sequences);
        }

        public IEnumerable<GafImageInfo> EnumerateImageInfos()
        {
            return this.imageDb.Items;
        }
    }
}