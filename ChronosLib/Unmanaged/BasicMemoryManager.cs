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
using System.Collections.Generic;
using System.Diagnostics;
using static TerraFX.Interop.Mimalloc;

namespace ChronosLib.Unmanaged {
    public unsafe sealed class BasicMemoryManager : IMemoryManager {
        #region ================== Instance fields

        private List<IntPtr> ownedMemory;

        #endregion

        #region ================== Instance properties

        public bool IsDisposed { get; private set; }

        #endregion

        #region ================== Constructors

        public BasicMemoryManager () {
            ownedMemory = new List<IntPtr> ();
        }

        #endregion

        #region ================== Instance methods

        public int FindOwnedMemory (IntPtr pointer)
            => ownedMemory.BinarySearch (pointer, IntPtrComparer.Instance);

        public IntPtr GetMemory (nint bytesCount) {
            CheckDisposed ();

            if (bytesCount < 1)
                throw new ArgumentOutOfRangeException (nameof (bytesCount), "The number of bytes to allocate must be greater than 0.");

            var mem = AllocateMemory ((nuint) bytesCount);

            var memIdx = FindOwnedMemory (mem);
            Debug.Assert (memIdx < 0, "This should never happen.");
            ownedMemory.Insert (~memIdx, mem);

            return mem;
        }

        public IntPtr GetMemoryAligned (nint bytesCount, nint alignment) {
            CheckDisposed ();

            if (bytesCount < 1)
                throw new ArgumentOutOfRangeException (nameof (bytesCount), "The number of bytes to allocate must be greater than 0.");

            var mem = AllocateMemoryAligned ((nuint) bytesCount, (nuint) alignment);

            var memIdx = FindOwnedMemory (mem);
            Debug.Assert (memIdx < 0, "This should never happen.");
            ownedMemory.Insert (~memIdx, mem);

            return mem;
        }

        public void ReturnMemory (void* pointer) {
            CheckDisposed ();

            var memIdx = FindOwnedMemory ((IntPtr) pointer);

            if (memIdx < 0)
                throw new ArgumentException (nameof (pointer), "The given memory is not owned by this manager.");

            FreeMemory ((IntPtr) pointer);
            ownedMemory.RemoveAt (memIdx);
        }

        private void CheckDisposed () {
            if (IsDisposed)
                throw new ObjectDisposedException (GetType ().Name);
        }

        private IntPtr AllocateMemory (nuint bytesCount) => (IntPtr) mi_malloc (bytesCount)!;

        private IntPtr AllocateMemoryAligned (nuint bytesCount, nuint alignment) => (IntPtr) mi_malloc_aligned (bytesCount, alignment)!;

        private void FreeMemory (IntPtr ptr) => mi_free ((void*) ptr);

        #endregion

        #region ================== IDisposable support

        ~BasicMemoryManager () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
            }
        }

        private void Dispose (bool disposing) {
            if (IsDisposed)
                return;

            if (disposing)
                GC.SuppressFinalize (this);

            if (ownedMemory != null) {
                foreach (var mem in ownedMemory)
                    FreeMemory (mem);

                ownedMemory.Clear ();
                ownedMemory.Capacity = 0;

                ownedMemory = null!;
            }
        }

        public void Dispose () => Dispose (true);

        #endregion
    }
}