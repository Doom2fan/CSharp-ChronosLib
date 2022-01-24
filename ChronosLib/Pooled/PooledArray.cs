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
    public struct PooledArray<T> : IDisposable {
        public static PooledArray<T> Empty () {
            return new PooledArray<T> {
                Array = System.Array.Empty<T> (),
                arrayPool = null,
                RealLength = 0,
            };
        }

        public static PooledArray<T> GetArray (int length) => GetArray (length, ArrayPool<T>.Shared);

        public static PooledArray<T> GetArray (int length, ArrayPool<T> pool) {
            if (length == 0)
                return Empty ();

            var newArr = new PooledArray<T> {
                arrayPool = pool,
                RealLength = length,
                Array = pool.Rent (length),
            };

            return newArr;
        }

        internal PooledArray (ArrayPool<T> pool, T [] array, int length) {
            Debug.Assert (length > 0);

            arrayPool = pool;
            RealLength = length;
            Array = array;

            disposedValue = false;
        }

        #region ================== Instance fields

        private ArrayPool<T> arrayPool;

        #endregion

        #region ================== Instance properties

        public int RealLength { get; private set; }
        public T [] Array { get; private set; }

        public Span<T> Span => Array.AsSpan (0, RealLength);

        #endregion

        #region ================== Casts

        public static explicit operator T [] (PooledArray<T> array) => array.Array;
        public static implicit operator Span<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.RealLength);
        public static implicit operator ReadOnlySpan<T> (PooledArray<T> array) => array.Array.AsSpan (0, array.RealLength);

        #endregion

        #region ================== Instance methods

        public StructPooledList<T> MoveToStructPooledList (CL_ClearMode clearMode)
            => new StructPooledList<T> (clearMode, arrayPool, Array, RealLength);

        #endregion

        #region ================== IDisposable support

        private bool disposedValue;

        public void Dispose () {
            if (disposedValue)
                return;

            arrayPool?.Return (Array);
            RealLength = 0;
            Array = System.Array.Empty<T> ();

            disposedValue = true;
        }

        #endregion
    }
}
