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

namespace ChronosLib.Pooled {
    [DebuggerDisplay ("PooledArray<T> (Count = {RealLength})")]
    public struct PooledArray<T> : IDisposable {
        public static PooledArray<T> Empty () => new (null, false, System.Array.Empty<T> (), 0);

        public static PooledArray<T> GetArray (int length) => GetArray (length, CL_ClearMode.Auto, ArrayPool<T>.Shared);

        public static PooledArray<T> GetArray (int length, ArrayPool<T> pool)
            => GetArray (length, CL_ClearMode.Auto, pool);

        public static PooledArray<T> GetArray (int length, CL_ClearMode clearMode)
            => GetArray (length, clearMode, ArrayPool<T>.Shared);

        public static PooledArray<T> GetArray (int length, CL_ClearMode clearMode, ArrayPool<T> pool) {
            if (length == 0)
                return Empty ();

            return new (pool, clearMode, pool.Rent (length), length);
        }

        internal PooledArray (ArrayPool<T>? pool, CL_ClearMode clearMode, T [] array, int length)
            : this (pool, PooledUtils.ShouldClear<T> (clearMode), array, length) { }

        internal PooledArray (ArrayPool<T>? pool, bool clear, T [] array, int length) {
            Debug.Assert (length > 0);

            clearOnFree = clear;
            arrayPool = pool;
            RealLength = length;
            Array = array;

            disposedValue = false;
        }

        #region ================== Instance fields

        private bool clearOnFree;
        private ArrayPool<T>? arrayPool;

        #endregion

        #region ================== Instance properties

        public int RealLength { get; private set; }
        public T [] Array { get; private set; }

        public CL_ClearMode ClearMode => clearOnFree ? CL_ClearMode.Always : CL_ClearMode.Never;
        public Span<T> Span => Array.AsSpan (0, RealLength);

        #endregion

        #region ================== Casts

        public static explicit operator T [] (PooledArray<T> array) => array.Array;
        public static implicit operator Span<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.RealLength);
        public static implicit operator ReadOnlySpan<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.RealLength);

        #endregion

        #region ================== Instance methods

        public StructPooledList<T> MoveToStructPooledList () => MoveToStructPooledList (ClearMode);

        public StructPooledList<T> MoveToStructPooledList (CL_ClearMode clearMode) {
            var ret = new StructPooledList<T> (clearMode, arrayPool, Array, RealLength);

            this = Empty ();

            return ret.Move ();
        }

        #endregion

        #region ================== IDisposable support

        private bool disposedValue;

        public void Dispose () {
            if (disposedValue)
                return;

            if (clearOnFree)
                System.Array.Clear (Array, 0, RealLength);

            arrayPool?.Return (Array);
            RealLength = 0;
            Array = System.Array.Empty<T> ();

            disposedValue = true;
        }

        #endregion
    }
}
