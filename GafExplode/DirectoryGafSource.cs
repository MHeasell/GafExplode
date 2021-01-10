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

    class ImageInfoDatabase : ItemDatabase<string, GafImageInfo>
    {
    }

    class ItemDatabase<TKey, T>
    {
        public List<T> Items { get; set; }
        public Dictionary<TKey, int> ItemsByKey { get; set; }
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

        private static ImageInfoDatabase GenerateImageInfoDatabase(string directoryName, List<GafSequenceJson> entries, Func<Color, byte> paletteLookup)
        {
            var (images, imagesByFilename) = Deduplicate(GenerateImageInfos(directoryName, entries, paletteLookup), new GafImageInfoComparer());
            return new ImageInfoDatabase { Items = images, ItemsByKey = imagesByFilename };
        }

        private static GafLayerInfo GenerateLayerInfoFromFrame(Dictionary<string, int> imageInfoLookup, GafLayerJson layer)
        {
            return new GafLayerInfo {
                ImageIndex = imageInfoLookup[layer.ImageFileName],
                PosX = layer.OriginX,
                PosY = layer.OriginY,
                Unknown3 = layer.Unknown3,
            };
        }

        private static GafFrameInfo GenerateLayerInfosFromFrame(Dictionary<string, int> imageInfoLookup, GafFrameJson frame)
        {
            var info = new GafFrameInfo
            {
                Duration = frame.Duration,
                Unknown3 = frame.Unknown3,
            };

            if (frame.ImageFileName == null)
            {
                info.Layers = frame.Layers.Select(x => GenerateLayerInfoFromFrame(imageInfoLookup, x)).ToList();
            }
            else
            {
                info.PosX = frame.OriginX;
                info.PosY = frame.OriginY;
                info.ImageIndex = imageInfoLookup[frame.ImageFileName];
            }

            return info;
        }

        private static IEnumerable<GafEntryInfo> GenerateGafSequences(Dictionary<string, int> imageInfoLookup, List<GafSequenceJson> entries)
        {
            return entries.Select(entry => new GafEntryInfo {
                Name = entry.Name,
                FrameInfos = entry.Frames.Select(frame => GenerateLayerInfosFromFrame(imageInfoLookup, frame)).ToList(),
            });
        }

        private readonly List<GafSequenceJson> sequences;
        private readonly ImageInfoDatabase imageDb;

        public DirectoryGafSource(string directoryName, Func<Color, byte> paletteLookup)
        {
            this.sequences = JsonConvert.DeserializeObject<List<GafSequenceJson>>(File.ReadAllText(Path.Combine(directoryName, "gaf.json")));
            this.imageDb = GenerateImageInfoDatabase(directoryName, this.sequences, paletteLookup);
        }

        public IEnumerable<GafEntryInfo> EnumerateSequences()
        {
            return GenerateGafSequences(this.imageDb.ItemsByKey, this.sequences);
        }

        public IEnumerable<GafImageInfo> EnumerateImageInfos()
        {
            return this.imageDb.Items;
        }
    }
}