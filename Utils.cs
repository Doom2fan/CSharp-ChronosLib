/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;

namespace ChronosLib {
    static internal class Utils {
        static internal class BitConversion {
            static internal class BigEndian {
                #region Signed

                static internal int ToInt32 (byte [] value, int startIndex = 0) {
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

                static internal short ToInt16 (byte [] value, int startIndex = 0) {
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

                static internal long ToInt64 (byte [] value, int startIndex = 0) {
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

                static internal uint ToUInt32 (byte [] value, int startIndex = 0) {
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

                static internal ushort ToUInt16 (byte [] value, int startIndex = 0) {
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

                static internal ulong ToUInt64 (byte [] value, int startIndex = 0) {
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

            static internal class LittleEndian {
                #region Signed

                static internal int ToInt32 (byte [] value, int startIndex = 0) {
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

                static internal short ToInt16 (byte [] value, int startIndex = 0) {
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

                static internal long ToInt64 (byte [] value, int startIndex = 0) {
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

                static internal uint ToUInt32 (byte [] value, int startIndex = 0) {
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

                static internal ushort ToUInt16 (byte [] value, int startIndex = 0) {
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

                static internal ulong ToUInt64 (byte [] value, int startIndex = 0) {
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
    }
}
