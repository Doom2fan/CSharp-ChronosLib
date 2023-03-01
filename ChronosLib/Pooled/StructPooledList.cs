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
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using CommunityToolkit.HighPerformance;

namespace ChronosLib.Pooled {
    [NonCopyable]
    [DebuggerDisplay ("StructPooledList<T> (Count = {size}, Capacity = {items.Length])")]
    public struct StructPooledList<T> : IList<T>, IReadOnlyList<T>, IDisposable {
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

        /// <summary>Used only for copying or moving the list.</summary>
        private StructPooledList (ref StructPooledList<T> other, bool move) {
            IsDisposed = false;

            pool = other.pool;
            clearOnFree = other.clearOnFree;

            if (move) {
                syncRoot = other.syncRoot;

                items = other.items;
                size = other.size;
                version = other.version;

                other.syncRoot = null;
                other.InternalDispose ();
            } else {
                syncRoot = null;

                items = emptyArray;
                size = 0;
                version = 0;

                AddRange (other.Span);
            }
        }

        public StructPooledList (CL_ClearMode clearMode) : this (clearMode, null) { }

        public StructPooledList (CL_ClearMode clearMode, ArrayPool<T>? customPool)
            : this () {
            clearOnFree = PooledUtils.ShouldClear<T> (clearMode);
            pool = customPool ?? ArrayPool<T>.Shared;
            items = emptyArray;
        }

        internal StructPooledList (CL_ClearMode clearMode, ArrayPool<T>? arrayPool, T [] array, int length)
            : this (clearMode, arrayPool) {
            items = array;
            size = length;
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
        public void AddRange (ref StructPooledList<T> newItems) => AddRange (newItems.Span);

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

            if (clearOnFree) {
                // Clear the removed elements so that the gc can reclaim the reference.
                Array.Clear (items, size - count, count);
            }

            size -= count;
            version++;
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

        public Enumerator GetEnumerator () => new Enumerator (ref this);

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

        public StructPooledList<T> Clone () => new StructPooledList<T> (ref this, false);

        public StructPooledList<T> Move () => new StructPooledList<T> (ref this, true);

        public PooledArray<T> MoveToArray () {
            var ret = new PooledArray<T> (pool, clearOnFree, items, size);

            InternalDispose ();

            return ret;
        }

        public void Reinit () {
            if (items != emptyArray)
                Dispose ();

            IsDisposed = false;
        }

        #endregion

        #region ================== Enumerator

        public ref struct Enumerator {
            #region ================== Instance fields

            private readonly Ref<StructPooledList<T>> list;
            private int index;
            private readonly int version;
            private T current;

            #endregion

            #region ================== Instance properties

            public T Current {
                get {
                    if (index == 0 || index == list.Value.size + 1)
                        throw new InvalidOperationException ("Invalid enumerator state: enumeration cannot proceed.");

                    return current;
                }
            }

            #endregion

            #region ================== Constructors

            internal Enumerator (ref StructPooledList<T> list) {
                this.list = new Ref<StructPooledList<T>> (ref list);
                index = 0;
                version = list.version;
                current = default!;
            }

            #endregion

            #region ================== Instance methods

            public bool MoveNext () {
                var localList = list;

                if (version == localList.Value.version && ((uint) index < (uint) localList.Value.size)) {
                    current = localList.Value.items [index];
                    index++;
                    return true;
                }

                return MoveNextRare ();
            }

            private bool MoveNextRare () {
                if (version != list.Value.version)
                    throw new InvalidOperationException ("Collection was modified during enumeration.");

                index = list.Value.size + 1;
                current = default!;
                return false;
            }

            public void Reset () {
                if (version != list.Value.version)
                    throw new InvalidOperationException ("Collection was modified during enumeration.");

                index = 0;
                current = default!;
            }

            public void Dispose () { }

            #endregion
        }

        #endregion

        #region ================== Interface implementations

        #region IEnumerable

        IEnumerator<T> IEnumerable<T>.GetEnumerator () => throw new NotSupportedException ();
        IEnumerator IEnumerable.GetEnumerator () => throw new NotSupportedException ();

        #endregion

        #region ICollection

        bool ICollection<T>.IsReadOnly => false;

        #endregion

        #endregion

        #region ================== IDisposable Support

        public bool IsDisposed { get; private set; }

        private void InternalDispose () {
            items = emptyArray;
            size = 0;
            version++;

            IsDisposed = true;
        }

        public void Dispose () {
            if (!IsDisposed) {
                ReturnArray ();
                size = 0;
                version++;

                IsDisposed = true;
            }
        }

        #endregion
    }
}
