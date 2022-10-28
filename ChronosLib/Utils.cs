/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ChronosLib.Pooled;

namespace ChronosLib {
    public static class BitConversion {
        public static class BigEndian {
            private unsafe static T ToX<T> (ReadOnlySpan<byte> bytes) where T : unmanaged {
                Span<byte> buffer = stackalloc byte [sizeof (T)];
                bytes [..sizeof (T)].CopyTo (buffer);

                if (BitConverter.IsLittleEndian)
                    buffer.Reverse ();

                return Unsafe.ReadUnaligned<T> (ref buffer [0]);
            }

            private unsafe static void FromX<T> (T value, Span<byte> bytes) where T : unmanaged {
                bytes = bytes [..sizeof (T)];

                Unsafe.WriteUnaligned (ref bytes [0], value);

                if (BitConverter.IsLittleEndian)
                    bytes.Reverse ();
            }

            #region From bytes, signed

            public static int ToInt32 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (int))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<int> (bytes);
            }

            public static short ToInt16 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (short))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<short> (bytes);
            }

            public static long ToInt64 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (long))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<long> (bytes);
            }

            #endregion

            #region From bytes, unsigned

            public static uint ToUInt32 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (uint))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<uint> (bytes);
            }

            public static ushort ToUInt16 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (ushort))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<ushort> (bytes);
            }

            public static ulong ToUInt64 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (ulong))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<ulong> (bytes);
            }

            #endregion

            #region To bytes, signed

            public static void ToBytes (int val, Span<byte> bytes) {
                if (bytes.Length < sizeof (int))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (short val, Span<byte> bytes) {
                if (bytes.Length < sizeof (short))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (long val, Span<byte> bytes) {
                if (bytes.Length < sizeof (long))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            #endregion

            #region To bytes, unsigned

            public static void ToBytes (uint val, Span<byte> bytes) {
                if (bytes.Length < sizeof (uint))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (ushort val, Span<byte> bytes) {
                if (bytes.Length < sizeof (ushort))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (ulong val, Span<byte> bytes) {
                if (bytes.Length < sizeof (ulong))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            #endregion
        }

        public static class LittleEndian {
            private unsafe static T ToX<T> (ReadOnlySpan<byte> bytes) where T : unmanaged {
                Span<byte> buffer = stackalloc byte [sizeof (T)];
                bytes [..sizeof (T)].CopyTo (buffer);

                if (!BitConverter.IsLittleEndian)
                    buffer.Reverse ();

                return Unsafe.ReadUnaligned<T> (ref buffer [0]);
            }

            private unsafe static void FromX<T> (T value, Span<byte> bytes) where T : unmanaged {
                bytes = bytes [..sizeof (T)];

                Unsafe.WriteUnaligned (ref bytes [0], value);

                if (!BitConverter.IsLittleEndian)
                    bytes.Reverse ();
            }

            #region From bytes, signed

            public static int ToInt32 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (int))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<int> (bytes);
            }

            public static short ToInt16 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (short))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<short> (bytes);
            }

            public static long ToInt64 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (long))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<long> (bytes);
            }

            #endregion

            #region From bytes, unsigned

            public static uint ToUInt32 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (uint))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<uint> (bytes);
            }

            public static ushort ToUInt16 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (ushort))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<ushort> (bytes);
            }

            public static ulong ToUInt64 (ReadOnlySpan<byte> bytes) {
                if (bytes.Length < sizeof (ulong))
                    throw new ArgumentException (null, nameof (bytes));

                return ToX<ulong> (bytes);
            }

            #endregion

            #region To bytes, signed

            public static void ToBytes (int val, Span<byte> bytes) {
                if (bytes.Length < sizeof (int))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (short val, Span<byte> bytes) {
                if (bytes.Length < sizeof (short))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (long val, Span<byte> bytes) {
                if (bytes.Length < sizeof (long))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            #endregion

            #region To bytes, unsigned

            public static void ToBytes (uint val, Span<byte> bytes) {
                if (bytes.Length < sizeof (uint))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (ushort val, Span<byte> bytes) {
                if (bytes.Length < sizeof (ushort))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            public static void ToBytes (ulong val, Span<byte> bytes) {
                if (bytes.Length < sizeof (ulong))
                    throw new ArgumentException (null, nameof (bytes));

                FromX (val, bytes);
            }

            #endregion
        }
    }

    internal static class Utils {
        internal static int Rehash (int hash) {
            const int a = 6;
            const int b = 13;
            const int c = 25;

            uint uhash = (uint) hash * 982451653u;

            var rehashed =
                ((uhash << a) | (uhash >> (32 - a))) ^
                ((uhash << b) | (uhash >> (32 - b))) ^
                ((uhash << c) | (uhash >> (32 - c)));

            return (int) rehashed;
        }

        internal static PooledArray<Range> SplitSpan<T> (ReadOnlySpan<T> span, ReadOnlySpan<T> splitters, bool includeEmpty = false)
            where T : IEquatable<T> {
            using var splitsArr = new StructPooledList<Range> (CL_ClearMode.Auto);

            var currentPoint = 0;
            while (currentPoint < span.Length) {
                var hasSplit = false;
                for (int i = 0; i < splitters.Length; i++) {
                    var idx = span [currentPoint..].IndexOf (splitters [i..(i+1)]);
                    if (idx < 0)
                        continue;

                    idx += currentPoint;

                    if (includeEmpty || idx - currentPoint > 0)
                        splitsArr.Add (new Range (currentPoint, idx));
                    currentPoint = idx + 1;
                    hasSplit = true;

                    break;
                }

                if (!hasSplit) {
                    if (span.Length - currentPoint > 0)
                        splitsArr.Add (new Range (currentPoint, span.Length));

                    break;
                }
            }

            return splitsArr.MoveToArray ();
        }
    }

    internal readonly struct RehashedValue<T>
        : IComparable<T>, IComparable<RehashedValue<T>>, IEquatable<T>, IEquatable<RehashedValue<T>>
        where T : IEquatable<T>, IComparable<T> {
        #region ================== Instance fields

        public readonly T Value;

        #endregion

        #region ================== Constructors

        public RehashedValue (T val) {
            Value = val;
        }

        #endregion

        #region ================== Instance methods

        public override bool Equals (object obj) {
            if (obj is RehashedValue<T>) {
                var other = ((RehashedValue<T>) obj).Value;
                return Value.Equals (other);
            }

            return Value.Equals (obj);
        }

        public bool Equals (T other) {
            return Value.Equals (other);
        }

        public bool Equals (RehashedValue<T> other) {
            return Equals (other.Value);
        }

        public int CompareTo (T other) {
            return Value.CompareTo (other);
        }

        public int CompareTo (RehashedValue<T> other) {
            return CompareTo (other.Value);
        }

        public override int GetHashCode () {
            return Utils.Rehash (Value.GetHashCode ());
        }

        #endregion
    }

    internal class IntPtrComparer : IComparer<IntPtr> {
        public static IntPtrComparer Instance = new IntPtrComparer ();

        public unsafe int Compare (IntPtr a, IntPtr b) {
            var aPtr = (void*) a;
            var bPtr = (void*) b;

            if (aPtr > bPtr)
                return 1;
            else if (aPtr < bPtr)
                return -1;
            else
                return 0;
        }
    }
}
