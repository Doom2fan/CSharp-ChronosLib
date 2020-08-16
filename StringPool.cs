/*
 *  ChronosLib - A collection of useful things
 *  Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
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
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using MurmurHash.Net;

namespace ChronosLib {
    public class StringPool {
        #region ================== Constants

        public const StringComparison ComparisonMode = StringComparison.InvariantCultureIgnoreCase;
        public readonly CultureInfo UsedCulture = CultureInfo.InvariantCulture;

        #endregion

        #region ================== Instance fields

        protected List<(string text, uint hash)> stringTable;

        #endregion

        #region ================== Constructors

        public StringPool () {
            stringTable = new List<(string text, uint hash)> ();
        }

        #endregion

        #region ================== Instance methods

        public string GetOrCreate (ReadOnlySpan<char> text) {
            Span<char> textLower = stackalloc char [text.Length];
            text.ToLower (textLower, UsedCulture);

            var textBytes = MemoryMarshal.Cast<char, byte> (textLower);
            uint textHash = MurmurHash3.Hash32 (textBytes, 0);

            foreach (var str in stringTable) {
                if (str.hash == textHash && str.text.AsSpan ().Equals (textLower, ComparisonMode))
                    return str.text;
            }

            var newStr = new string (textLower);
            stringTable.Add ((newStr, textHash));
            return newStr;
        }

        public void Clear () {
            stringTable.Clear ();
        }

        #endregion
    }
}
