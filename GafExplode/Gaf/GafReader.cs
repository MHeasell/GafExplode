namespace GafExplode.Gaf
{
    using System;
    using System.IO;

    /// <summary>
    /// Class for reading GAF format files.
    /// </summary>
    public class GafReader : IDisposable
    {
        private readonly BinaryReader reader;

        private readonly IGafReaderAdapter adapter;

        /// <summary>
        /// Initializes a new instance of the <see cref="GafReader"/> class.
        /// </summary>
        /// <param name="filename">The path of the GAF file to read.</param>
        /// <param name="adapter">The adapter to pass read data to.</param>
        public GafReader(string filename, IGafReaderAdapter adapter)
            : this(File.OpenRead(filename), adapter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GafReader"/> class.
        /// </summary>
        /// <param name="s">The stream to read from.</param>
        /// <param name="adapter">The adapter to pass read data to.</param>
        public GafReader(Stream s,  IGafReaderAdapter adapter)
            : this(new BinaryReader(s), adapter)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GafReader"/> class.
        /// </summary>
        /// <param name="reader">The reader to read from.</param>
        /// <param name="adapter">The adapter to pass read data to.</param>
        public GafReader(BinaryReader reader, IGafReaderAdapter adapter)
        {
            this.reader = reader;
            this.adapter = adapter;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="GafReader"/> class.
        /// </summary>
        ~GafReader()
        {
            this.Dispose(false);
        }

        /// <summary>
        /// Reads the GAF data from the input stream.
        /// </summary>
        public void Read()
        {
            // read in header
            Structures.GafHeader header = new Structures.GafHeader();
            Structures.GafHeader.Read(this.reader, ref header);

            this.adapter.BeginRead(header.SequenceCount);

            // read in pointers to entries
            int[] pointers = new int[header.SequenceCount];
            for (int i = 0; i < header.SequenceCount; i++)
            {
                pointers[i] = this.reader.ReadInt32();
            }

            // read in the actual entries themselves
            for (int i = 0; i < header.SequenceCount; i++)
            {
                this.reader.BaseStream.Seek(pointers[i], SeekOrigin.Begin);
                this.ReadGafEntry();
            }

            this.adapter.EndRead();
        }

        /// <summary>
        /// Disposes the object.
        /// See <see cref="IDisposable.Dispose"/>.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the object.
        /// </summary>
        /// <param name="disposing">
        /// Indicates whether to dispose of managed resources.
        /// This should be true when explicitly disposing
        /// and false when being disposed due to garbage collection.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Dispose();
            }
        }

        private void ReadGafEntry()
        {
            // read the entry header
            Structures.GafSequenceHeader entry = new Structures.GafSequenceHeader();
            Structures.GafSequenceHeader.Read(this.reader, ref entry);

            this.adapter.BeginEntry(ref entry);

            // read in all the frame entry pointers
            Structures.GaFrameListItem[] frameEntries = new Structures.GaFrameListItem[entry.FrameCount];
            for (int i = 0; i < entry.FrameCount; i++)
            {
                Structures.GaFrameListItem.Read(this.reader, ref frameEntries[i]);
            }

            // read in the corresponding frames
            for (int i = 0; i < entry.FrameCount; i++)
            {
                this.reader.BaseStream.Seek(frameEntries[i].PtrFrameInfo, SeekOrigin.Begin);
                this.LoadFrame(ref frameEntries[i]);
            }

            this.adapter.EndEntry();
        }

        private void LoadFrame(ref Structures.GaFrameListItem entry)
        {
            // read in the frame data table
            Structures.GafFrameInfo d = new Structures.GafFrameInfo();
            Structures.GafFrameInfo.Read(this.reader, ref d);

            this.adapter.BeginFrame(ref entry, ref d);

            // read the actual frame image
            this.reader.BaseStream.Seek(d.PtrFrameData, SeekOrigin.Begin);

            if (d.LayerCount > 0)
            {
                // read in the pointers
                uint[] framePointers = new uint[d.LayerCount];
                for (int i = 0; i < d.LayerCount; i++)
                {
                    framePointers[i] = this.reader.ReadUInt32();
                }

                // read in each frame
                for (int i = 0; i < d.LayerCount; i++)
                {
                    this.reader.BaseStream.Seek(framePointers[i], SeekOrigin.Begin);
                    var dummyEntry = new Structures.GaFrameListItem { PtrFrameInfo = 0, Duration = 0 };
                    this.LoadFrame(ref dummyEntry);
                }
            }
            else
            {
                byte[] data;
                if (d.Compressed)
                {
                    data = CompressedFrameReader.ReadCompressedImage(this.reader, d.Width, d.Height, d.TransparencyIndex);
                }
                else
                {
                    data = this.ReadUncompressedImage(d.Width, d.Height);
                }

                this.adapter.SetFrameData(data);
            }

            this.adapter.EndFrame();
        }

        private byte[] ReadUncompressedImage(int width, int height)
        {
            return this.reader.ReadBytes(width * height);
        }
    }
}
