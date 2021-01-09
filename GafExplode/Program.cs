using System.Collections.Generic;
using System.Drawing;
using System.IO;

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
                UnexplodeGaf(directoryName, filename);
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

        public static void UnexplodeGaf(string directoryName, string filename)
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
                var source = new DirectoryGafSource(directoryName, color => reversePalette[color]);
                var gafWriter = new Gaf.GafWriter(writer, source);
                gafWriter.Write();
            }
        }
    }
}

