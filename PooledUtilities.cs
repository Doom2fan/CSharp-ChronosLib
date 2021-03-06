﻿/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ChronosLib.Pooled {
    public enum CL_ClearMode {
        /// <summary>Reference types and value types that contain reference types are cleared when the internal arrays
        /// are returned to the pool. Value types that do not contain reference types are not cleared when returned to
        /// the pool.</summary>
        Auto = 0,
        /// <summary>Collections are always cleared.</summary>
        Always = 1,
        /// <summary>Collections are never cleared.</summary>
        Never = 2
    }

    internal static class PooledUtils {
        public static bool IsCompatibleObject<T> (object? value) {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (value == null && default (T) == null);
        }

        public static void EnsureNotNull<T> (object? value, string paramName) {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            if (!(default (T) == null) && value == null)
                throw new ArgumentNullException (paramName);
        }
    }

    public struct PooledArray<T> : IDisposable {
        public static PooledArray<T> Empty () {
            return new PooledArray<T> {
                Array = System.Array.Empty<T> (),
                arrayPool = null,
                RealLength = 0,
            };
        }

        public static PooledArray<T> GetArray (int length) {
            return GetArray (length, ArrayPool<T>.Shared);
        }

        public static PooledArray<T> GetArray (int length, ArrayPool<T> pool) {
            var newArr = new PooledArray<T> {
                arrayPool = pool,
                RealLength = length,
                Array = pool.Rent (length),
            };

            return newArr;
        }

        #region ================== Instance fields

        private ArrayPool<T>? arrayPool;

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

        #region ================== IDisposable support

        private bool disposedValue;

        public void Dispose () {
            if (!disposedValue) {
                arrayPool?.Return (Array);
                RealLength = 0;
                Array = System.Array.Empty<T> ();

                disposedValue = true;
            }
        }

        #endregion
    }

    public struct StructPooledList<T> : IList<T>, IReadOnlyList<T>, IList, IDisposable {
        #region ================== Constants

        private const int maxArrayLength = 0x7FEFFFFF;
        private const int defaultCapacity = 4;
        private static readonly T [] emptyArray = Array.Empty<T> ();

        #endregion

        #region ================== Instance fields

        private ArrayPool<T> pool;
        private object? syncRoot;

        private T [] items;
        private int size;
        private int version;
        private readonly bool clearOnFree;

        #endregion

        #region ================== Constructors

        public StructPooledList (CL_ClearMode clearMode)
            : this (clearMode, null) {
        }

        public StructPooledList (CL_ClearMode clearMode, ArrayPool<T>? customPool)
            : this () {
            clearOnFree = ShouldClear (clearMode);
            pool = customPool ?? ArrayPool<T>.Shared;
            items = emptyArray;
        }

        #endregion

        #region ================== Indexers

        public T this [int idx] {
            get {
                if ((uint) idx >= (uint) size)
                    throw new IndexOutOfRangeException ();

                return items [idx];
            }

            set {
                if ((uint) idx >= (uint) size)
                    throw new IndexOutOfRangeException ();

                version++;
                items [idx] = value;
            }
        }

        object? IList.this [int index] {
            get => this [index];

            set {
                PooledUtils.EnsureNotNull<T> (value, nameof (value));

                try {
                    this [index] = (T) value!;
                } catch (InvalidCastException) {
                    throw new ArgumentException ($"Wrong value type. Expected {typeof (T).ToString ()}, got '{value!.GetType ().Name}'");
                }
            }
        }

        #endregion

        #region ================== Instance properties

        /// <inheritdoc/>
        public int Capacity {
            get => items.Length;

            set {
                if (value < size)
                    throw new ArgumentOutOfRangeException (null, "The specified capacity is lower than the list's size");

                if (value != items.Length) {
                    if (value > 0) {
                        var newItems = pool.Rent (value);

                        if (size > 0)
                            Array.Copy (items, newItems, size);

                        ReturnArray ();

                        items = newItems;
                    } else {
                        ReturnArray ();
                        size = 0;
                    }
                }
            }
        }

        /// <inheritdoc/>
        public int Count => size;

        public Span<T> Span {
            get {
                if (items == null)
                    return emptyArray;

                return items.AsSpan (0, size);
            }
        }

        /// <summary>Returns the ClearMode behavior for the collection, denoting whether values are cleared from
        /// internal arrays before returning them to the pool.</summary>
        public CL_ClearMode ClearMode => clearOnFree ? CL_ClearMode.Always : CL_ClearMode.Never;

        #endregion

        #region ================== Instance methods

        private static bool ShouldClear (CL_ClearMode mode) {
            return mode == CL_ClearMode.Always
                || (mode == CL_ClearMode.Auto && RuntimeHelpers.IsReferenceOrContainsReferences <T> ());
        }

        private void ReturnArray () {
            if (items.Length == 0)
                return;

            // Clear the elements so that the gc can reclaim the references.
            pool.Return (items, clearArray: clearOnFree);

            items = emptyArray;
        }

        public void EnsureCapacity (int min) {
            if (items.Length < min) {
                int newCapacity = items.Length == 0 ? defaultCapacity : items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast

                if ((uint) newCapacity > maxArrayLength)
                    newCapacity = maxArrayLength;
                if (newCapacity < min)
                    newCapacity = min;

                Capacity = newCapacity;
            }
        }

        #region Item insertion

        /// <inheritdoc/>
        public void Add (T item) {
            EnsureCapacity (size + 1);

            items [size] = item;

            version++;
            size++;
        }

        /// <inheritdoc/>
        public void Add (ref T item) {
            EnsureCapacity (size + 1);

            items [size] = item;

            version++;
            size++;
        }

        /// <summary>Adds an item multiple times.</summary>
        /// <param name="item">The item to add.</param>
        /// <param name="count">The number of times to add.</param>
        public void Add (T item, int count) {
            EnsureCapacity (size + count);

            for (int i = 0; i < count; i++)
                items [size + i] = item;

            version++;
            size += count;
        }

        /// <summary>Adds an item multiple times.</summary>
        /// <param name="item">The item to add.</param>
        /// <param name="count">The number of times to add.</param>
        public void Add (ref T item, int count) {
            EnsureCapacity (size + count);

            for (int i = 0; i < count; i++)
                items [size + i] = item;

            version++;
            size += count;
        }

        /// <inheritdoc/>
        public void AddRange (StructPooledList<T> newItems) {
            int startPos = size;
            EnsureCapacity (size + newItems.Count);

            Array.Copy (newItems.items, 0, items, startPos, newItems.Count);

            version++;
            size += newItems.Count;
        }

        /// <inheritdoc/>
        public void AddRange (ReadOnlySpan<T> newItems) {
            int startPos = size;
            EnsureCapacity (size + newItems.Length);

            newItems.CopyTo (items.AsSpan (startPos, newItems.Length));

            version++;
            size += newItems.Length;
        }

        public Span<T> AddSpan (int count) {
            EnsureCapacity (size + count);

            var span = items.AsSpan (size, count);

            version++;
            size += count;

            return span;
        }

        /// <inheritdoc/>
        public void Insert (int index, T item) {
            // Note that insertions at the end are legal.
            if ((uint) index > (uint) size)
                throw new ArgumentOutOfRangeException (nameof (index), "Index is out of range.");

            if (size == items.Length)
                EnsureCapacity (size + 1);
            if (index < size)
                Array.Copy (items, index, items, index + 1, size - index);

            items [index] = item;
            size++;
            version++;
        }

        #endregion

        #region Item removal

        /// <inheritdoc/>
        public void Clear () {
            version++;

            if (size > 0 && clearOnFree) {
                // Clear the elements so that the gc can reclaim the references.
                Array.Clear (items, 0, size);
            }

            size = 0;
        }

        /// <inheritdoc/>
        public bool Remove (T item) {
            int index = IndexOf (item);
            if (index >= 0) {
                RemoveAt (index);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        public void RemoveAt (int index) {
            if ((uint) index >= (uint) size)
                throw new ArgumentOutOfRangeException (nameof (index), "Index is out of range.");

            size--;
            if (index < size)
                Array.Copy (items, index + 1, items, index, size - index);
            version++;

            if (clearOnFree) {
                // Clear the removed element so that the gc can reclaim the reference.
                items [size] = default!;
            }
        }

        public void RemoveEnd (int count) {
            if ((uint) count > (uint) size)
                throw new ArgumentOutOfRangeException (nameof (count), "Count is out of range.");

            size -= count;
            version++;

            if (clearOnFree) {
                // Clear the removed elements so that the gc can reclaim the reference.
                Array.Clear (items, size - count - 1, count);
            }
        }

        #endregion

        #region Search

        /// <inheritdoc/>
        public int IndexOf (T item)
            => Array.IndexOf (items, item, 0, size);

        /// <inheritdoc/>
        public bool Contains (T item)
            => size != 0 && IndexOf (item) != -1;

        #endregion

        #region Copying

        /// <summary>Copies this list to the specified span.</summary>
        /// <param name="span">The span to copy to.</param>
        public void CopyTo (Span<T> span) {
            if (span.Length < Count)
                throw new ArgumentException ("Destination span is shorter than the list being copied.");

            Span.CopyTo (span);
        }

        /// <inheritdoc/>
        public void CopyTo (T [] array, int arrayIndex) {
            Array.Copy (items, 0, array, arrayIndex, size);
        }

        #endregion

        #region Enumeration

        public Enumerator GetEnumerator () {
            return new Enumerator (this);
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator () {
            return new Enumerator (this);
        }

        IEnumerator IEnumerable.GetEnumerator () {
            return new Enumerator (this);
        }

        #endregion

        public T [] ToArray () {
            if (size == 0)
                return emptyArray;

            var arr = new T [size];
            CopyTo (arr, 0);

            return arr;
        }

        public PooledArray<T> ToPooledArray () {
            if (size == 0)
                return PooledArray<T>.Empty ();

            var arr = PooledArray<T>.GetArray (size);
            CopyTo (arr.Array, 0);

            return arr;
        }

        #endregion

        #region ================== Enumerator

        public struct Enumerator : IEnumerator<T>, IEnumerator {
            #region ================== Instance fields

            private readonly StructPooledList<T> list;
            private int index;
            private readonly int version;
            private T current;

            #endregion

            #region ================== Instance properties

            public T Current => current;

            object? IEnumerator.Current {
                get {
                    if (index == 0 || index == list.size + 1)
                        throw new InvalidOperationException ("Invalid enumerator state: enumeration cannot proceed.");

                    return Current;
                }
            }

            #endregion

            #region ================== Constructors

            internal Enumerator (StructPooledList<T> list) {
                this.list = list;
                index = 0;
                version = list.version;
                current = default!;
            }

            #endregion

            #region ================== Instance methods

            public bool MoveNext () {
                var localList = list;

                if (version == localList.version && ((uint) index < (uint) localList.size)) {
                    current = localList.items [index];
                    index++;
                    return true;
                }

                return MoveNextRare ();
            }

            private bool MoveNextRare () {
                if (version != list.version)
                    throw new InvalidOperationException ("Collection was modified during enumeration.");

                index = list.size + 1;
                current = default!;
                return false;
            }

            public void Reset () {
                if (version != list.version)
                    throw new InvalidOperationException ("Collection was modified during enumeration.");

                index = 0;
                current = default!;
            }

            public void Dispose () { }

            #endregion
        }

        #endregion

        #region ================== Interface implementations

        #region IList

        bool IList.IsFixedSize => false;

        bool IList.IsReadOnly => false;

        int IList.Add (object? item) {
            if (PooledUtils.IsCompatibleObject<T> (item)) {
                Add ((T) item!);
                return size - 1;
            }

            return -1;
        }

        void IList.Insert (int index, object? item) {
            if (PooledUtils.IsCompatibleObject<T> (item))
                Insert (index, (T) item!);
        }

        void IList.Remove (object? item) {
            if (PooledUtils.IsCompatibleObject<T> (item))
                Remove ((T) item!);
        }

        int IList.IndexOf (object? item) {
            if (PooledUtils.IsCompatibleObject<T> (item))
                return IndexOf ((T) item!);

            return -1;
        }

        bool IList.Contains (object? item) {
            if (PooledUtils.IsCompatibleObject<T> (item))
                return Contains ((T) item!);

            return false;
        }

        #endregion

        #region ICollection

        bool ICollection<T>.IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        // Synchronization root for this object.
        object ICollection.SyncRoot {
            get {
                Interlocked.CompareExchange<object?> (ref syncRoot, new object (), null);

                return syncRoot;
            }
        }

        void ICollection.CopyTo (Array array, int index) {
            if (array == null)
                throw new ArgumentNullException (nameof (array));
            if (array != null && array.Rank != 1)
                throw new ArgumentException ("Multidimensional arrays not supported.");

            try {
                // Array.Copy will check for NULL.
                Array.Copy (items, 0, array!, index, size);
            } catch (ArrayTypeMismatchException) {
                throw new ArgumentException ("Invalid array type.");
            }
        }

        #endregion

        #endregion

        #region ================== IDisposable Support

        public bool IsDisposed { get; private set; }

        public void Dispose () {
            if (!IsDisposed) {
                ReturnArray ();
                size = 0;

                IsDisposed = true;
            }
        }

        #endregion
    }

    public class CL_PooledList<T> : Collections.Pooled.PooledList<T> {
        public CL_PooledList () : base () { }
        public CL_PooledList (int count) : base (count) { }

        #region ================== IDisposable Support

        public bool IsDisposed {
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            set;
        }

        ~CL_PooledList () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of PooledList<{typeof (T).FullName}> has not been disposed.");
                Dispose (false);
            }
        }

        protected override void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing)
                    GC.SuppressFinalize (this);

                base.Dispose (disposing);

                IsDisposed = true;
            }
        }

        #endregion
    }

    public class CL_PooledListPool<T> : IDisposable {
        public static CL_PooledListPool<T> Shared {
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            get;
            private set;
        }

        #region ================== Instance fields

        protected List<CL_PooledList<T>> pooledLists;
        protected List<CL_PooledList<T>> rentedLists;

        #endregion

        #region ================== Constructor

        static CL_PooledListPool () {
            Shared = new CL_PooledListPool<T> ();

            StaticDisposables.AddDisposable (Shared);
        }

        public CL_PooledListPool () {
            pooledLists = new List<CL_PooledList<T>> ();
            rentedLists = new List<CL_PooledList<T>> ();
        }

        #endregion

        #region ================== Instance methods

        public CL_PooledList<T> Rent () {
            if (pooledLists.Count > 0) {
                var list = pooledLists [^1];

                Debug.Assert (!list.IsDisposed, "The list was disposed.");

                pooledLists.RemoveAt (pooledLists.Count - 1);
                rentedLists.Add (list);

                return list;
            } else {
                var list = new CL_PooledList<T> ();

                rentedLists.Add (list);

                return list;
            }
        }

        public void Return (CL_PooledList<T> list) {
            Debug.Assert (!list.IsDisposed, "Attempted to return a disposed list.");

            list.Clear ();
            list.Capacity = 0;

            var idx = rentedLists.IndexOf (list);

            Debug.Assert (idx >= 0, "A list has been returned more than once, or this is not a pooled list.");

            rentedLists [idx] = rentedLists [^1];
            rentedLists.RemoveAt (rentedLists.Count - 1);
            pooledLists.Add (list);
        }

        #endregion

        #region ================== IDisposable Support

        public bool IsDisposed { get; private set; }

        ~CL_PooledListPool () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
            }
        }

        protected void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing)
                    GC.SuppressFinalize (this);

                foreach (var list in pooledLists)
                    list?.Dispose ();

                Debug.Assert (rentedLists.Count < 1, "Rented lists have been leaked.");

                foreach (var list in rentedLists)
                    list?.Dispose ();

                pooledLists.Clear ();
                rentedLists.Clear ();

                IsDisposed = true;
            }
        }

        public void Dispose () {
            Dispose (true);
        }

        #endregion
    }
}
