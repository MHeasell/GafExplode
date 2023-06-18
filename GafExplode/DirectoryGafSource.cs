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
                && x.Compress == y.Compress
                && x.Data.SequenceEqual(y.Data);
        }

        public int GetHashCode(GafImageInfo o)
        {
            var hashCode = o.Width.GetHashCode();
            hashCode = hashCode * 17 + o.Height.GetHashCode();
            hashCode = hashCode * 17 + o.TransparencyIndex.GetHashCode();
            hashCode = hashCode * 17 + o.Compress.GetHashCode();
            foreach (var d in o.Data)
            {
                hashCode = hashCode * 17 + d.GetHashCode();
            }
            return hashCode;
        }
    }

    struct ImageInfoKey
    {
        string FileName { get; set; }
        int TransparencyIndex { get; set; }
        bool Compress { get; set; }

        public ImageInfoKey(string fileName, int transparencyIndex, bool compress)
        {
            this.FileName = fileName;
            this.TransparencyIndex = transparencyIndex;
            this.Compress = compress;
        }

        public override bool Equals(object obj)
        {
            return obj is ImageInfoKey key
                && this.FileName == key.FileName
                && this.TransparencyIndex == key.TransparencyIndex
                && this.Compress == key.Compress;
        }

        public override int GetHashCode()
        {
            int hashCode = -362860870;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(this.FileName);
            hashCode = hashCode * -1521134295 + this.TransparencyIndex.GetHashCode();
            hashCode = hashCode * -1521134295 + this.Compress.GetHashCode();
            return hashCode;
        }
    }

    class ImageInfoDatabase : ItemDatabase<ImageInfoKey, GafImageInfo, Point>
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
        private static GafImageInfo GetImageInfo(string filename, int transparencyIndex, bool compress, Func<Color, byte> paletteLookup)
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
                    Compress = compress,
                };
            }
        }

        private static IEnumerable<KeyValuePair<ImageInfoKey, GafImageInfo>> GetImageInfos(string directoryName, GafFrameJson frame, Func<Color, byte> paletteLookup)
        {
            if (frame.ImageFileName != null)
            {
                var info = GetImageInfo(Path.Combine(directoryName, frame.ImageFileName), frame.TransparencyIndex.Value, frame.Compress.Value, paletteLookup);
                yield return new KeyValuePair<ImageInfoKey, GafImageInfo>(new ImageInfoKey(frame.ImageFileName, frame.TransparencyIndex.Value, frame.Compress.Value), info);
            }
            else
            {
                foreach (var layer in frame.Layers)
                {
                    var info = GetImageInfo(Path.Combine(directoryName, layer.ImageFileName), layer.TransparencyIndex, layer.Compress, paletteLookup);
                    yield return new KeyValuePair<ImageInfoKey, GafImageInfo>(new ImageInfoKey(layer.ImageFileName, layer.TransparencyIndex, layer.Compress), info);
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

        private static IEnumerable<KeyValuePair<ImageInfoKey, GafImageInfo>> GenerateImageInfos(string directoryName, List<GafSequenceJson> entries, Func<Color, byte> paletteLookup)
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

        private static GafLayerInfo GenerateLayerInfoFromFrame(Dictionary<ImageInfoKey, int> imageInfoLookup, Dictionary<ImageInfoKey, Point> originMappingLookup, GafLayerJson layer)
        {
            var key = new ImageInfoKey(layer.ImageFileName, layer.TransparencyIndex, layer.Compress);
            return new GafLayerInfo {
                ImageIndex = imageInfoLookup[key],
                OriginX = layer.OriginX + originMappingLookup[key].X,
                OriginY = layer.OriginY + originMappingLookup[key].Y,
                Unknown3 = layer.Unknown3,
            };
        }

        private static GafFrameInfo GenerateLayerInfosFromFrame(Dictionary<ImageInfoKey, int> imageInfoLookup, Dictionary<ImageInfoKey, Point> originMappingLookup, GafFrameJson frame)
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
                var key = new ImageInfoKey(frame.ImageFileName, frame.TransparencyIndex.Value, frame.Compress.Value);
                info.OriginX = frame.OriginX + originMappingLookup[key].X;
                info.OriginY = frame.OriginY + originMappingLookup[key].Y;
                info.ImageIndex = imageInfoLookup[key];
            }

            return info;
        }

        private static IEnumerable<GafEntryInfo> GenerateGafSequences(Dictionary<ImageInfoKey, int> imageInfoLookup, Dictionary<ImageInfoKey, Point> originMappingLookup, List<GafSequenceJson> entries)
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