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
using System.Globalization;
using System.Runtime.InteropServices;
using MurmurHash.Net;

namespace ChronosLib.StringPooling;

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
