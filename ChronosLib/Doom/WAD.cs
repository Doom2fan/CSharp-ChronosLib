/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ChronosLib.Pooled;

namespace ChronosLib.Doom.WAD;

[DebuggerDisplay ("{Message, nq}")]
public class WADException : Exception {
    public WADException (string message, Exception innerException = null) : base (message, innerException) { }
}

[DebuggerDisplay ("{Message, nq}")]
public class WADLoadException : WADException {
    public enum ErrorType {
        /// <summary>The file contained in the stream is not a WAD.</summary>
        NotWAD = 0,
        /// <summary>The WAD's directory is invalid.</summary>
        InvalidDirectory,
        /// <summary>The WAD is invalid or malformed.</summary>
        InvalidWAD,
    }
    public ErrorType Error { get; protected set; }

    public WADLoadException (string message, ErrorType err, Exception innerException = null) : base (message, innerException) { Error = err; }
}

[DebuggerDisplay ("WADLump (Name = {Name}, Size = {Size})")]
public struct WADLump {
    #region ================== Instance properties

    /// <summary>Indicates whether the WAD lump is valid.</summary>
    public bool IsValid { get; internal set; }
    /// <summary>The WAD the lump belongs to.</summary>
    public WAD Source { get; internal set; }

    /// <summary>The location of the lump's contents in the WAD file.</summary>
    public int FilePos { get; internal set; }
    /// <summary>The name of the lump.</summary>
    public string Name { get; internal set; }
    /// <summary>The file size of the IWAD in bytes.</summary>
    public int Size { get; internal set; }

    #endregion

    #region ================== Instance methods

    /// <summary>Reads the lump to a byte array.</summary>
    /// <param name="buffer">An array of bytes.</param>
    /// <returns>The total number of bytes read into the buffer.</returns>
    public int ReadLump (Span<byte> buffer) {
        if (Source is null)
            throw new WADException ("Missing Source on WADLump instance");
        if (Source.IsDisposed)
            throw new ObjectDisposedException ("WAD", "The lump's source WAD was disposed");
        if (Source.WADStream is null)
            throw new WADException ("The source WAD's stream was disposed");

        if (buffer.Length > Size)
            buffer = buffer [..Size];

        Source.WADStream.Seek (FilePos, SeekOrigin.Begin);
        return Source.WADStream.Read (buffer);
    }

    #endregion
}

[DebuggerDisplay ("WADLumpCollection (Count = {Count})")]
public sealed class WADLumpCollection : ICollection<WADLump> {
    #region ================== Instance fields

    private List<WADLump> lumps;

    #endregion

    #region ================== Instance properties

    public int Count { get; set; }
    public bool IsReadOnly => true;

    #endregion

    #region ================== Constructors

    internal WADLumpCollection (int capacity = 1000) {
        lumps = new List<WADLump> (capacity);
    }

    #endregion

    #region ================== Instance methods

    internal void AddLump (WADLump item) => lumps.Add (item);
    internal void RemoveLump (WADLump item) => lumps.Remove (item);
    internal void ClearList () => lumps.Clear ();

    #endregion

    #region ================== Indexers

    public WADLump this [int i] {
        get => lumps [i];
        internal set => lumps [i] = value;
    }

    #endregion

    #region ================== Interfaces

    public bool Contains (WADLump item) => lumps.Contains (item);

    public List<WADLump>.Enumerator GetEnumerator () => lumps.GetEnumerator ();
    IEnumerator<WADLump> IEnumerable<WADLump>.GetEnumerator () => lumps.GetEnumerator ();
    IEnumerator IEnumerable.GetEnumerator () => lumps.GetEnumerator ();

    public void Add (WADLump item) => throw new NotImplementedException ();
    public void Clear () => throw new NotImplementedException ();
    public void CopyTo (WADLump [] array, int arrayIndex) => throw new NotImplementedException ();
    public bool Remove (WADLump item) => throw new NotImplementedException ();

    #endregion
}

[DebuggerDisplay ("WAD (IWAD = {IsIWAD}, Lump count = {Lumps.Count})")]
public class WAD : IEnumerable<WADLump>, IDisposable {
    #region ================== Constants

    public const int HEADERSIZE = 4 * 3;
    public const int LUMPINFOSIZE = 4 * 4;

    #endregion

    #region ================== Instance fields

    protected internal Stream WADStream { get; set; }

    #endregion

    #region ================== Instance properties

    /// <summary>Whether the WAD is an IWAD.</summary>
    public bool IsIWAD { get; protected internal set; } = false;
    /// <summary>A collection containing all of the WAD's lumps.</summary>
    public WADLumpCollection Lumps { get; protected internal set; }
    /// <summary>Whether the WAD instance has been disposed.</summary>
    public bool IsDisposed { get; private set; } = false;

    #endregion

    #region ================== Constructors

    private WAD () { }

    #endregion

    #region ================== Static functions

    /// <summary>Loads a WAD file.</summary>
    /// <param name="stream">The stream to read the WAD from.</param>
    /// <param name="loadIntoRAM">If true, the entire stream will be loaded into RAM.</param>
    /// <returns>An instance of the WAD class.</returns>
    /// <remarks>Unless loadIntoRAM is true, the stream will be held onto by the instance of the WAD class, and it'll dispose of the stream when it's disposed of.</remarks>
    public static WAD LoadWAD (Stream stream, bool loadIntoRAM = false) {
        if (stream is null)
            throw new ArgumentNullException (nameof (stream));
        if (!stream.CanRead)
            throw new ArgumentException ("The stream cannot be read.", nameof (stream));
        if (!stream.CanSeek)
            throw new ArgumentException ("The stream cannot be seeked.", nameof (stream));

        stream.Seek (0, SeekOrigin.Begin);

        Span<byte> headerBytes = stackalloc byte [HEADERSIZE];

        if (stream.Read (headerBytes) != HEADERSIZE)
            throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidWAD);

        Span<char> id = stackalloc char [4];
        Encoding.ASCII.GetChars (headerBytes [..4], id);

        var isIWAD = "IWAD".AsSpan ().Equals (id, StringComparison.Ordinal);

        if (!isIWAD && !"PWAD".AsSpan ().Equals (id, StringComparison.Ordinal))
            throw new WADLoadException ("Not a WAD file", WADLoadException.ErrorType.NotWAD);

        var numLumps = BitConversion.LittleEndian.ToInt32 (headerBytes [4..]);
        var infoTableOfs = BitConversion.LittleEndian.ToInt32 (headerBytes [8..]);

        if (numLumps < 0)
            throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidWAD);
        if (infoTableOfs < 0 || (infoTableOfs + (numLumps * LUMPINFOSIZE) > stream.Length))
            throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidDirectory);

        if (loadIntoRAM) {
            var ramStream = new MemoryStream ((int) stream.Length);
            stream.Seek (0, SeekOrigin.Begin);
            stream.CopyTo (ramStream);
            stream = ramStream;
        }

        using var directoryBytesArr = PooledArray<byte>.GetArray (numLumps * LUMPINFOSIZE);
        var directoryBytes = directoryBytesArr.Span;

        stream.Seek (infoTableOfs, SeekOrigin.Begin);
        if (stream.Read (directoryBytes) != directoryBytes.Length)
            throw new WADLoadException ("Invalid WAD file", WADLoadException.ErrorType.InvalidWAD);
        var lumps = new WADLumpCollection (numLumps);

        var wad = new WAD ();

        for (int i = 0; i < numLumps; i++) {
            var lmp = new WADLump ();
            var lmpStart = i * LUMPINFOSIZE;

            lmp.Source = wad;

            lmp.FilePos = BitConversion.LittleEndian.ToInt32 (directoryBytes [lmpStart..]);
            lmp.Size = BitConversion.LittleEndian.ToInt32 (directoryBytes [(lmpStart + 4)..]);

            var nameSpan = directoryBytes.Slice (lmpStart + 8, 8);
            var nulIdx = nameSpan.IndexOf ((byte) '\0');
            if (nulIdx > -1)
                nameSpan = nameSpan.Slice (0, nulIdx);

            lmp.Name = Encoding.ASCII.GetString (nameSpan);

            lmp.IsValid = (lmp.FilePos <= stream.Length) && ((lmp.FilePos + lmp.Size) <= stream.Length);

            lumps.AddLump (lmp);
        }

        wad.IsIWAD = isIWAD;
        wad.WADStream = stream;
        wad.Lumps = lumps;

        return wad;
    }

    #endregion

    #region ================== Interfaces

    public WADLump this [int i] {
        get => Lumps [i];
        protected internal set => Lumps [i] = value;
    }

    public List<WADLump>.Enumerator GetEnumerator () => Lumps.GetEnumerator ();
    IEnumerator<WADLump> IEnumerable<WADLump>.GetEnumerator () => Lumps.GetEnumerator ();
    IEnumerator IEnumerable.GetEnumerator () => Lumps.GetEnumerator ();

    #endregion

    #region ================== IDisposable support

    protected virtual void Dispose (bool disposing) {
        if (IsDisposed)
            return;

        if (disposing)
            GC.SuppressFinalize (this);

        WADStream?.Dispose ();

        if (!disposing) {
            Lumps?.ClearList ();
        }

        Lumps = null;

        IsDisposed = true;
    }

    ~WAD () {
        if (!IsDisposed) {
            Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
            Dispose (false);
        }
    }

    public void Dispose () {
        Dispose (true);
    }

    #endregion
}
