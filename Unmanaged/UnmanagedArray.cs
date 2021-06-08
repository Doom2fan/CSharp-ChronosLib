/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;
using static TerraFX.Interop.Mimalloc;

namespace ChronosLib.Unmanaged {
    public unsafe struct UnmanagedArray<T> : IDisposable
        where T : unmanaged {
        public static UnmanagedArray<T> Empty () {
            return new UnmanagedArray<T> {
                Pointer = null,
                Length = 0,
            };
        }

        public static UnmanagedArray<T> GetArray (int length) {
            var newArr = new UnmanagedArray<T> {
                Pointer = (T*) mi_malloc ((nuint) (length * sizeof (T))),
                Length = length,
            };

            return newArr;
        }

        #region ================== Instance properties

        public T* Pointer { get; private set; }
        public int Length { get; private set; }

        public Span<T> Span => new Span<T> (Pointer, Length);

        #endregion

        #region ================== Casts

        public static implicit operator Span<T> (UnmanagedArray<T> array) => array.Span;
        public static implicit operator ReadOnlySpan<T> (UnmanagedArray<T> array) => array.Span;

        #endregion

        #region ================== IDisposable support

        private bool disposedValue;

        public void Dispose () {
            if (!disposedValue) {
                mi_free (Pointer);
                Length = 0;
                Pointer = null;

                disposedValue = true;
            }
        }

        #endregion
    }
}
