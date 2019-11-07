/*
 *  ChronosLib - A collection of useful things
 *  Copyright (C) 2018-2019 Chronos "phantombeta" Ouroboros
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ChronosLib.Doom.WAD {
    public class WADException : Exception {
        public WADException (string message, Exception innerException = null) : base (message, innerException) { }
    }
    public class WADLoadException : WADException {
        public enum ErrorType {
            /// <summary>
            /// The file contained in the stream is not a WAD.
            /// </summary>
            NotWAD = 0,
            /// <summary>
            /// The WAD's directory is invalid.
            /// </summary>
            InvalidDirectory,
            /// <summary>
            /// The WAD is invalid or malformed.
            /// </summary>
            InvalidWAD,
        }
        public ErrorType Error { get; protected set; }

        public WADLoadException (string message, ErrorType err, Exception innerException = null) : base (message, innerException) { Error = err; }
    }

    public class WADLump {
        #region Properties

        public bool IsValid { get; protected internal set; }
        public WAD Source { get; protected internal set; }

        public int FilePos { get; protected internal set; }
        public string Name { get; protected internal set; }
        public int Size { get; protected internal set; }

        #endregion

        #region Methods

        /// <summary>
        /// Reads the lump to a stream.
        /// </summary>
        /// <returns>A stream containing the lump's data.</returns>
        public Stream ReadLump () {
            if (Source is null)
                throw new WADException ("Missing Source on WADLump instance");
            if (Source.IsDisposed)
                throw new ObjectDisposedException ("WAD", "The lump's source WAD was disposed");
            if (Source.WADStream is null)
                throw new WADException ("The source WAD's stream was disposed");

            byte [] buffer = new byte [Size];
            Source.WADStream.Seek (FilePos, SeekOrigin.Begin);
            Source.WADStream.Read (buffer, 0, Size);

            return new MemoryStream (buffer);
        }

        /// <summary>
        /// Reads the lump to a byte array.
        /// </summary>
        /// <param name="buffer">An array of bytes.</param>
        /// <returns>The total number of bytes read into the buffer.</returns>
        public int ReadLump (byte [] buffer) {
            if (Source is null)
                throw new WADException ("Missing Source on WADLump instance");
            if (Source.IsDisposed)
                throw new ObjectDisposedException ("WAD", "The lump's source WAD was disposed");
            if (Source.WADStream is null)
                throw new WADException ("The source WAD's stream was disposed");

            Source.WADStream.Seek (FilePos, SeekOrigin.Begin);
            return Source.WADStream.Read (buffer, 0, Size);
        }

        #endregion
    }

    public class WADLumpCollection : ICollection<WADLump> {
        protected List<WADLump> lumps;

        #region Properties

        public int Count { get; set; }
        public bool IsReadOnly => true;

        #endregion

        #region Constructors

        protected internal WADLumpCollection (int capacity = 1000) {
            lumps = new List<WADLump> (capacity);
        }

        #endregion

        #region Methods

        protected internal void AddLump (WADLump item) => lumps.Add (item);
        protected internal void RemoveLump (WADLump item) => lumps.Remove (item);
        protected internal void ClearList () => lumps.Clear ();

        #endregion

        #region Interface

        public WADLump this [int i] {
            get => lumps [i];
            protected internal set => lumps [i] = value;
        }

        public bool Contains (WADLump item) => lumps.Contains (item);
        public IEnumerator<WADLump> GetEnumerator () => lumps.GetEnumerator ();
        IEnumerator IEnumerable.GetEnumerator () => lumps.GetEnumerator ();

        public void Add (WADLump item) => throw new NotImplementedException ();
        public void Clear () => throw new NotImplementedException ();
        public void CopyTo (WADLump [] array, int arrayIndex) => throw new NotImplementedException ();
        public bool Remove (WADLump item) => throw new NotImplementedException ();

        #endregion
    }

    public class WAD : IEnumerable<WADLump>, IDisposable {
        #region Constants

        public const int HEADERSIZE = 4 * 3;
        public const int LUMPINFOSIZE = 4 * 4;

        #endregion

        #region Variables

        protected internal Stream WADStream { get; set; }

        #endregion

        #region Properties

        public bool IsIWAD { get; protected internal set; } = false;
        public WADLumpCollection Lumps { get; protected internal set; }
        public bool IsDisposed { get; private set; } = false;

        #endregion

        #region Constructors

        private WAD () { }

        #endregion

        #region Methods

        /// <summary>
        /// Loads a WAD file.
        /// </summary>
        /// <param name="stream">The stream to read the WAD from.</param>
        /// <param name="loadIntoRAM">If true, the entire stream will be loaded into RAM.</param>
        /// <returns>An instance of the WAD class.</returns>
        /// <remarks>Unless loadIntoRAM is true, the stream will be held onto by the instance of the WAD class, and it'll dispose of the stream when it's disposed of.</remarks>
        public static WAD LoadWAD (Stream stream, bool loadIntoRAM = false) {
            if (stream is null)
                throw new ArgumentNullException ("stream");
            if (!stream.CanRead)
                throw new ArgumentException ("The stream cannot be read.", "stream");
            if (!stream.CanSeek)
                throw new ArgumentException ("The stream cannot be seeked.", "stream");

            var wad = new WAD ();

            stream.Seek (0, SeekOrigin.Begin);

            var headerBytes = new byte [HEADERSIZE];

            if (stream.Read (headerBytes, 0, HEADERSIZE) != HEADERSIZE)
                throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidWAD);

            string id = Encoding.ASCII.GetString (headerBytes, 0, 4);

            if (!id.Equals ("IWAD") && !id.Equals ("PWAD"))
                throw new WADLoadException ("Not a WAD file", WADLoadException.ErrorType.NotWAD);

            wad.IsIWAD = id.Equals ("IWAD");

            int numLumps = Utils.BitConversion.LittleEndian.ToInt32 (headerBytes, 4);
            int infoTableOfs = Utils.BitConversion.LittleEndian.ToInt32 (headerBytes, 8);

            if (numLumps < 0)
                throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidWAD);
            if (infoTableOfs < 0 || infoTableOfs > stream.Length || (infoTableOfs + (numLumps * LUMPINFOSIZE) > stream.Length))
                throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidDirectory);

            if (loadIntoRAM) {
                var ramStream = new MemoryStream ((int) stream.Length);
                stream.Seek (0, SeekOrigin.Begin);
                stream.CopyTo (ramStream);
                stream = ramStream;
            }

            wad.WADStream = stream;

            byte [] directoryBytes = new byte [numLumps * LUMPINFOSIZE];

            stream.Seek (infoTableOfs, SeekOrigin.Begin);
            stream.Read (directoryBytes, 0, numLumps * LUMPINFOSIZE);
            wad.Lumps = new WADLumpCollection (numLumps);

            for (int i = 0; i < numLumps; i++) {
                var lmp = new WADLump ();
                int lmpStart = i * LUMPINFOSIZE;

                lmp.Source = wad;

                lmp.FilePos = Utils.BitConversion.LittleEndian.ToInt32 (directoryBytes, lmpStart);
                lmp.Size = Utils.BitConversion.LittleEndian.ToInt32 (directoryBytes, lmpStart + 4);
                lmp.Name = Encoding.ASCII.GetString (directoryBytes, lmpStart + 8, 8);

                lmp.IsValid = (lmp.FilePos <= stream.Length) && ((lmp.FilePos + lmp.Size) <= stream.Length);

                wad.Lumps.AddLump (lmp);
            }

            return wad;
        }

        #endregion

        #region Interface

        public WADLump this [int i] {
            get => Lumps [i];
            protected internal set => Lumps [i] = value;
        }

        public IEnumerator<WADLump> GetEnumerator () => Lumps.GetEnumerator ();
        IEnumerator IEnumerable.GetEnumerator () => Lumps.GetEnumerator ();

        #region IDisposable
        protected virtual void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing) {
                    if (WADStream != null)
                        WADStream.Dispose ();

                    Lumps.ClearList ();
                }

                Lumps = null;

                IsDisposed = true;
            }
        }

        public void Dispose () {
            Dispose (true);
        }
        #endregion

        #endregion
    }
}
