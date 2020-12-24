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

        public static void ExplodeGaf(string filename, string directoryName)
        {
            var adapter = new GafReaderAdapter();
            adapter.OutputDirectory = directoryName;
            using (var reader = new Gaf.GafReader(filename, adapter))
            {
                reader.Read();
            }
        }

        public static void UnexplodeGaf(string directoryName, string filename)
        {
            using (var writer = new BinaryWriter(File.OpenWrite(filename)))
            {
                var source = new DirectoryGafSource(directoryName);
                var gafWriter = new Gaf.GafWriter(writer, source);
                gafWriter.Write();
            }
        }
    }
}

