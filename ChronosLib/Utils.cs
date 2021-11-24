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
using ChronosLib.Pooled;

namespace ChronosLib {
    internal static class Utils {
        internal static class BitConversion {
            internal static class BigEndian {
                #region Signed

                internal static int ToInt32 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 4 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [4];
                    Array.Copy (value, startIndex, buffer, 0, 4);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt32 (buffer, 0);
                }

                internal static short ToInt16 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 2 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [2];
                    Array.Copy (value, startIndex, buffer, 0, 2);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt16 (buffer, 0);
                }

                internal static long ToInt64 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 8 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [8];
                    Array.Copy (value, startIndex, buffer, 0, 8);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt64 (buffer, 0);
                }

                #endregion

                #region Unsigned

                internal static uint ToUInt32 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 4 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [4];
                    Array.Copy (value, startIndex, buffer, 0, 4);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt32 (buffer, 0);
                }

                internal static ushort ToUInt16 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 2 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [2];
                    Array.Copy (value, startIndex, buffer, 0, 2);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt16 (buffer, 0);
                }

                internal static ulong ToUInt64 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 8 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [8];
                    Array.Copy (value, startIndex, buffer, 0, 8);

                    if (BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt64 (buffer, 0);
                }

                #endregion
            }

            internal static class LittleEndian {
                #region Signed

                internal static int ToInt32 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 4 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [4];
                    Array.Copy (value, startIndex, buffer, 0, 4);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt32 (buffer, 0);
                }

                internal static short ToInt16 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 2 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [2];
                    Array.Copy (value, startIndex, buffer, 0, 2);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt16 (buffer, 0);
                }

                internal static long ToInt64 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 8 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [8];
                    Array.Copy (value, startIndex, buffer, 0, 8);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToInt64 (buffer, 0);
                }

                #endregion

                #region Unsigned

                internal static uint ToUInt32 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 4 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [4];
                    Array.Copy (value, startIndex, buffer, 0, 4);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt32 (buffer, 0);
                }

                internal static ushort ToUInt16 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 2 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [2];
                    Array.Copy (value, startIndex, buffer, 0, 2);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt16 (buffer, 0);
                }

                internal static ulong ToUInt64 (byte [] value, int startIndex = 0) {
                    if (value is null)
                        throw new ArgumentNullException ("value");
                    if (startIndex + 8 > value.Length)
                        throw new ArgumentOutOfRangeException ("startIndex");

                    var buffer = new byte [8];
                    Array.Copy (value, startIndex, buffer, 0, 8);

                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse (buffer);

                    return BitConverter.ToUInt64 (buffer, 0);
                }

                #endregion
            }
        }

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
