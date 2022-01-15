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

namespace ChronosLib.Unmanaged {
    public unsafe interface IMemoryManager : IDisposable {
        #region ================== Properties

        /// <summary>Whether the manager has been disposed.</summary>
        bool IsDisposed { get; }

        #endregion

        #region ================== Methods

        /// <summary>Allocates memory.</summary>
        /// <param name="bytesCount">How much memory to allocate.</param>
        /// <returns>Returns a pointer to the allocated memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bytesCount is lower than 1.</exception>
        IntPtr GetMemory (nint bytesCount);

        /// <summary>Allocates memory.</summary>
        /// <param name="bytesCount">How much memory to allocate.</param>
        /// <returns>Returns a pointer to the allocated memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bytesCount is lower than 1.</exception>
        IntPtr GetMemory (int bytesCount) => GetMemory ((nint) bytesCount);

        /// <summary>Allocates memory aligned to the specified value.</summary>
        /// <param name="bytesCount">How much memory to allocate.</param>
        /// <param name="alignment">The value to align the memory to.</param>
        /// <returns>Returns a pointer to the allocated memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if bytesCount is lower than 1.</exception>
        IntPtr GetMemoryAligned (nint bytesCount, nint alignment);

        /// <summary>Allocates memory for the specified number of instances of T.</summary>
        /// <typeparam name="T">The type to allocate.</typeparam>
        /// <param name="count">The amount of instances to allocate.</param>
        /// <returns>Returns a pointer to the allocated memory.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if count is lower than 1.</exception>
        /// <exception cref="ArgumentException">Thrown if the size of <typeparamref name="T"/> has no size.</exception>
        T* GetMemory<T> (int count = 1)
            where T : unmanaged {
            if (count < 1)
                throw new ArgumentOutOfRangeException (nameof (count), "Count must be greater than 0.");
            if (sizeof (T) < 1)
                throw new ArgumentException (nameof (T), "T cannot be empty/have no size.");

            var ptr = GetMemory (sizeof (T) * count);

            return (T*) ptr;
        }

        /// <summary>Returns an allocated region of memory.</summary>
        /// <param name="pointer">The address of the memory region.</param>
        /// <exception cref="ArgumentException">Thrown when the memory is not owned by this memory pool, or was
        /// already returned.</exception>
        void ReturnMemory (void* pointer);

        /// <summary>Returns an allocated region of memory.</summary>
        /// <param name="pointer">The address of the memory region.</param>
        /// <exception cref="ArgumentException">Thrown when the memory is not owned by this memory pool, or was
        /// already returned.</exception>
        void ReturnMemory (IntPtr pointer) => ReturnMemory ((void*) pointer);

        #endregion
    }
}
