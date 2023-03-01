/*
 * Copyright 2013-2021 João Portela
 * Copyright 2021 Chronos "phantombeta" Ouroboros
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Diagnostics;

namespace ChronosLib.Collections {
    /// <summary>Circular buffer.
    /// When writing to a full buffer:
    /// PushBack -> removes this [0]/Front ()
    /// PushFront -> removes this [Size-1]/Back ()</summary>
    [DebuggerDisplay ("CircularBuffer (Count = {Size}, Capacity = {Capacity})")]
    public class CircularBuffer<T> {
        private readonly T [] buffer;

        /// <summary>The index of the first element in buffer.</summary>
        private int start;

        /// <summary>The index after the last element in the buffer.</summary>
        private int end;

        /// <summary>The buffer's size.</summary>
        private int size;

        /// <summary>Initializes a new instance of the <see cref="CircularBuffer{T}"/> class.</summary>
        /// <param name="capacity">Buffer capacity. Must be positive.</param>
        public CircularBuffer (int capacity) {
            if (capacity < 1) {
                throw new ArgumentException (
                    "Circular buffer cannot have negative or zero capacity.", nameof (capacity));
            }

            buffer = new T [capacity];

            start = 0;
            end = size == capacity ? 0 : size;
        }

        /// <summary>Maximum capacity of the buffer. Elements pushed into the buffer after maximum capacity is reached
        /// (IsFull = true), will remove an element.</summary>
        public int Capacity => buffer.Length;

        /// <summary>Boolean indicating if Circular is at full capacity. Adding more elements when the buffer is full
        /// will cause elements to be removed from the other end of the buffer.</summary>
        public bool IsFull => Size == Capacity;

        /// <summary>True if it has no elements.</summary>
        public bool IsEmpty => Size == 0;

        /// <summary>Current buffer size. (the number of elements that the buffer has)</summary>
        public int Size => size;

        /// <summary>Element at the front of the buffer - this [0].</summary>
        /// <returns>The value of the element of type T at the front of the buffer.</returns>
        public T Front () {
            ThrowIfEmpty ();
            return buffer [start];
        }

        /// <summary>Element at the back of the buffer - this [Size - 1].</summary>
        /// <returns>The value of the element of type T at the back of the buffer.</returns>
        public T Back () {
            ThrowIfEmpty ();
            return buffer [(end != 0 ? end : Capacity) - 1];
        }

        /// <summary>Index access to elements in buffer. Index does not loop around like when adding elements, valid
        /// interval is [0; Size)</summary>
        /// <param name="index">Index of element to access.</param>
        /// <exception cref="IndexOutOfRangeException">Thrown when index is outside of [; Size) interval.</exception>
        public T this [int index] {
            get {
                if (IsEmpty)
                    throw new IndexOutOfRangeException (string.Format ("Cannot access index {0}. Buffer is empty", index));
                if (index >= size)
                    throw new IndexOutOfRangeException (string.Format ("Cannot access index {0}. Buffer size is {1}", index, size));

                int actualIndex = InternalIndex (index);
                return buffer [actualIndex];
            }
            set {
                if (IsEmpty)
                    throw new IndexOutOfRangeException (string.Format ("Cannot access index {0}. Buffer is empty", index));
                if (index >= size)
                    throw new IndexOutOfRangeException (string.Format ("Cannot access index {0}. Buffer size is {1}", index, size));
                int actualIndex = InternalIndex (index);
                buffer [actualIndex] = value;
            }
        }

        /// <summary>Pushes a new element to the back of the buffer. Back ()/this [Size-1] will now return this element.
        ///
        /// When the buffer is full, the element at Front ()/this [0] will be popped to allow for this new element to
        /// fit.</summary>
        /// <param name="item">The item to push to the back of the buffer.</param>
        public void PushBack (T item) {
            if (IsFull) {
                buffer [end] = item;
                Increment (ref end);
                start = end;
            } else {
                buffer [end] = item;
                Increment (ref end);
                ++size;
            }
        }

        /// <summary>Pushes a new element to the front of the buffer. Front ()/this [0] will now return this element.
        ///
        /// When the buffer is full, the element at Back ()/this [Size-1] will be popped to allow for this new element
        /// to fit.</summary>
        /// <param name="item">The item to push to the front of the buffer.</param>
        public void PushFront (T item) {
            if (IsFull) {
                Decrement (ref start);
                end = start;
                buffer [start] = item;
            } else {
                Decrement (ref start);
                buffer [start] = item;
                ++size;
            }
        }

        /// <summary>Removes the element at the back of the buffer, decreasing the buffer size by 1.</summary>
        public void PopBack () {
            ThrowIfEmpty ("Cannot take elements from an empty buffer.");
            Decrement (ref end);
            buffer [end] = default (T);
            --size;
        }

        /// <summary>Removes the element at the front of the buffer, decreasing the buffer size by 1.</summary>
        public void PopFront () {
            ThrowIfEmpty ("Cannot take elements from an empty buffer.");
            buffer [start] = default (T);
            Increment (ref start);
            --size;
        }

        public void Clear () {
            ArrayOne ().Fill (default (T));
            ArrayTwo ().Fill (default (T));

            size = start = end = 0;
        }

        /// <summary>Copies the buffer contents to an array, according to the logical contents of the buffer (i.e.
        /// independent of the internal order/contents)</summary>
        /// <returns>A new array with a copy of the buffer contents.</returns>
        public T [] ToArray () {
            var segment1 = ArrayOne ();
            var segment2 = ArrayTwo ();

            T [] newArray = new T [Size];

            segment1.CopyTo (newArray.AsSpan (0, segment1.Length));
            segment2.CopyTo (newArray.AsSpan (segment1.Length, segment2.Length));

            return newArray;
        }

        private void ThrowIfEmpty (string message = "Cannot access an empty buffer.") {
            if (IsEmpty)
                throw new InvalidOperationException (message);
        }

        /// <summary>Increments the provided index variable by one, wrapping around if necessary.</summary>
        /// <param name="index"></param>
        private void Increment (ref int index) {
            if (++index == Capacity)
                index = 0;
        }

        /// <summary>Decrements the provided index variable by one, wrapping around if necessary.</summary>
        /// <param name="index"></param>
        private void Decrement (ref int index) {
            if (index == 0)
                index = Capacity;

            index--;
        }

        /// <summary>Converts the index in the argument to an index in <code>_buffer</code></summary>
        /// <returns>The transformed index.</returns>
        /// <param name='index'>External index.</param>
        private int InternalIndex (int index) {
            return start + (index < (Capacity - start) ? index : index - Capacity);
        }

        private Span<T> ArrayOne () {
            if (IsEmpty)
                return Span<T>.Empty;
            else if (start < end)
                return buffer.AsSpan (start, end - start);
            else
                return buffer.AsSpan (start, buffer.Length - start);
        }

        private Span<T> ArrayTwo () {
            if (IsEmpty)
                return Span<T>.Empty;
            else if (start < end)
                return buffer.AsSpan (end, 0);
            else
                return buffer.AsSpan (0, end);
        }
    }
}
