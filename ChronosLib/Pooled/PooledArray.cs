/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;
using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChronosLib.Pooled;

[DebuggerDisplay ("PooledArray<T> (Count = {Length})")]
public struct PooledArray<T> : IDisposable {
    #region ================== Constants

    private static readonly bool hasRefs = RuntimeHelpers.IsReferenceOrContainsReferences<T> ();

    #endregion

    internal PooledArray (ArrayPool<T>? pool, CL_ClearMode clearMode, T [] array, int length) {
        Debug.Assert (length > 0);

        clearOnFree = clearMode == CL_ClearMode.Always || (clearMode == CL_ClearMode.Auto && hasRefs);
        arrayPool = pool;
        Length = length;
        Array = array;

        disposedValue = false;
    }

    internal PooledArray (ArrayPool<T>? pool, bool clear, T [] array, int length)
        : this (pool, clear ? CL_ClearMode.Always : CL_ClearMode.Never, array, length) { }

    #region ================== Instance fields

    private bool clearOnFree;
    private ArrayPool<T>? arrayPool;

    #endregion

    #region ================== Instance properties

    public int Length { get; private set; }
    public T [] Array { get; private set; }

    public readonly CL_ClearMode ClearMode => clearOnFree ? CL_ClearMode.Always : CL_ClearMode.Never;
    public readonly Span<T> Span => Array.AsSpan (0, Length);

    #endregion

    #region ================== Casts

    public static explicit operator T [] (PooledArray<T> array) => array.Array;
    public static implicit operator Span<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.Length);
    public static implicit operator ReadOnlySpan<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.Length);

    #endregion

    #region ================== Static methods

    public static PooledArray<T> Empty () => new (null, false, System.Array.Empty<T> (), 0);

    public static PooledArray<T> GetArray (int length) => GetArray (length, CL_ClearMode.Auto, ArrayPool<T>.Shared);

    public static PooledArray<T> GetArray (int length, ArrayPool<T> pool) => GetArray (length, CL_ClearMode.Auto, pool);

    public static PooledArray<T> GetArray (int length, CL_ClearMode clearMode) => GetArray (length, clearMode, ArrayPool<T>.Shared);

    public static PooledArray<T> GetArray (int length, CL_ClearMode clearMode, ArrayPool<T> pool) {
        if (length == 0)
            return Empty ();

        return new (pool, clearMode, pool.Rent (length), length);
    }

    #endregion

    #region ================== Instance methods

    public StructPooledList<T> MoveToStructPooledList () => MoveToStructPooledList (ClearMode);

    public StructPooledList<T> MoveToStructPooledList (CL_ClearMode clearMode) {
        var ret = new StructPooledList<T> (clearMode, arrayPool, Array, Length);

        this = Empty ();

        return ret.Move ();
    }

    public readonly Span<T>.Enumerator GetEnumerator () => Span.GetEnumerator ();

    #endregion

    #region ================== IDisposable support

    private bool disposedValue;

    public void Dispose () {
        if (disposedValue)
            return;

        if (clearOnFree)
            System.Array.Clear (Array, 0, Length);

        arrayPool?.Return (Array);
        Length = 0;
        Array = System.Array.Empty<T> ();

        disposedValue = true;
    }

    #endregion
}
