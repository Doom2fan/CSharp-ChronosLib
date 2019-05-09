/*
 *  GZDoomLib - A library for using GZDoom's file formats in C#
 *  Copyright (C) 2018-2019 Chronos "phantombeta" Ouroboros
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;

namespace GZDoomLib {
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
