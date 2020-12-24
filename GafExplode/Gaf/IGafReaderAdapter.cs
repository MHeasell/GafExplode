namespace GafExplode.Gaf
{
    /// <summary>
    /// <para>
    /// Adapter for the <see cref="GafReader"/> class.
    /// </para>
    /// <para>
    /// Users should implement this interface
    /// to control how GAF data is processed during reading.
    /// </para>
    /// </summary>
    public interface IGafReaderAdapter
    {
        /// <summary>
        /// Indicates that reading of the GAF file has started.
        /// </summary>
        /// <param name="entryCount">The number of entries in the file.</param>
        void BeginRead(long entryCount);

        /// <summary>
        /// Indicates that the reader has begun reading a new GAF entry.
        /// </summary>
        /// <param name="name">The name of the entry.</param>
        /// <param name="frameCount">The number of frames in the entry.</param>
        void BeginEntry(ref Structures.GafSequenceHeader entry);

        /// <summary>
        /// Indicates that the reader has begun reading a new frame
        /// in the current entry.
        /// If we were already reading a frame,
        /// this new frame is a subframe of the current frame.
        /// </summary>
        /// <param name="x">The frame's X offset.</param>
        /// <param name="y">The frame's Y offset.</param>
        /// <param name="width">The width of the frame.</param>
        /// <param name="height">The height of the frame.</param>
        /// <param name="transparencyIndex">
        /// The color index used to indicate transparency.
        /// </param>
        /// <param name="subframeCount">
        /// The number of subframes contained within this frame.
        /// If this number is zero,
        /// expect <see cref="SetFrameData"/> to be called during this frame.
        /// </param>
        void BeginFrame(ref Structures.GaFrameListItem entry, ref Structures.GafFrameInfo data);

        /// <summary>
        /// Sets the image data for the current frame.
        /// This will be called only if the frame contains no subframes.
        /// </summary>
        /// <param name="data">The image data.</param>
        void SetFrameData(byte[] data);

        /// <summary>
        /// Indicates that the reader has finished reading the current frame.
        /// </summary>
        void EndFrame();

        /// <summary>
        /// Indicates that the reader has finished reading the current entry.
        /// </summary>
        void EndEntry();

        /// <summary>
        /// Indicates that the reader has finished reading the file.
        /// </summary>
        void EndRead();
    }
}