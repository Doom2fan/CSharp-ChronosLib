/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.Numerics;

namespace ChronosLib.FileLoading;

public abstract class SimpleParserBase {
    #region ================== Instance fields

    protected readonly string parserWhitespaceChars;
    protected int currentLine;

    #endregion

    #region ================== Constructors

    protected SimpleParserBase (string whiteChars) {
        parserWhitespaceChars = whiteChars;
    }

    #endregion

    #region ================== Instance methods

    protected void SkipWhitespace (ref ReadOnlySpan<char> source) {
        int i = 0;
        while (i < source.Length) {
            bool foundWhitespace = false;

            var c = source [i];
            foreach (var ws in parserWhitespaceChars) {
                if (c == ws) {
                    foundWhitespace = true;
                    break;
                }
            }

            if (!foundWhitespace)
                break;

            i++;
        }

        source = source.Slice (i);
    }

    protected ReadOnlySpan<char> ReadIgnoreWhitespace (ref ReadOnlySpan<char> source) {
        SkipWhitespace (ref source);

        int charIdx = -1;
        foreach (var value in parserWhitespaceChars)
            charIdx = Math.Max (charIdx, source.IndexOf (value));

        if (charIdx == -1)
            charIdx = source.Length;

        var text = source.Slice (0, charIdx);

        int endIdx = charIdx;
        while (endIdx < source.Length) {
            bool foundVal = false;
            foreach (var value in parserWhitespaceChars) {
                if (source [endIdx] == value) {
                    foundVal = true;
                    break;
                }
            }

            if (!foundVal)
                break;

            endIdx++;
        }

        source = source.Slice (endIdx);

        return text;
    }

    protected ReadOnlySpan<char> ReadUntil (ref ReadOnlySpan<char> source, char value) {
        int charIdx = source.IndexOf (value);

        if (charIdx == -1)
            charIdx = source.Length;

        var text = source.Slice (0, charIdx);
        source = source.Slice (Math.Min (charIdx + 1, source.Length));

        return text;
    }

    protected Vector3 ParseObjVector3 (ref ReadOnlySpan<char> source, string location) {
        var xStr = ReadIgnoreWhitespace (ref source);
        var yStr = ReadIgnoreWhitespace (ref source);
        var zStr = ReadIgnoreWhitespace (ref source);

        try {
            float x = float.Parse (xStr, provider: CultureInfo.InvariantCulture);
            float y = float.Parse (yStr, provider: CultureInfo.InvariantCulture);
            float z = float.Parse (zStr, provider: CultureInfo.InvariantCulture);

            return new Vector3 (x, y, z);
        } catch (FormatException fe) {
            throw CreateParseException (location, fe);
        }
    }

    protected Vector2 ParseObjVector2 (ref ReadOnlySpan<char> source, string location) {
        var xStr = ReadIgnoreWhitespace (ref source);
        var yStr = ReadIgnoreWhitespace (ref source);

        try {
            float x = float.Parse (xStr, provider: CultureInfo.InvariantCulture);
            float y = float.Parse (yStr, provider: CultureInfo.InvariantCulture);

            return new Vector2 (x, y);
        } catch (FormatException fe) {
            throw CreateParseException (location, fe);
        }
    }

    private ReadOnlySpan<char> ParseInt_ReadInt (ref ReadOnlySpan<char> source) {
        SkipWhitespace (ref source);

        int i = 0;
        while (i < source.Length) {
            var c = source [i];
            if (
                !(c >= '0' && c <= '9') &&
                c != '-' &&
                c != '+'
            ) {
                break;
            }

            i++;
        }

        var ret = source.Slice (0, i);
        source = source.Slice (i);

        return ret;
    }

    protected int ParseInt (ref ReadOnlySpan<char> source, string location) {
        var valStr = ParseInt_ReadInt (ref source);

        try {
            int i = int.Parse (valStr, provider: CultureInfo.InvariantCulture);
            return i;
        } catch (FormatException fe) {
            throw CreateParseException (location, fe);
        }
    }

    private ReadOnlySpan<char> ParseFloat_ReadFloat (ref ReadOnlySpan<char> source) {
        SkipWhitespace (ref source);

        int i = 0;
        while (i < source.Length) {
            var c = source [i];
            if (
                !(c >= '0' && c <= '9') &&
                c != '.' &&
                c != '+' &&
                c != '-'
            ) {
                break;
            }

            i++;
        }

        var ret = source.Slice (0, i);
        source = source.Slice (i);

        return ret;
    }

    protected float ParseFloat (ref ReadOnlySpan<char> source, string location) {
        var valStr = ParseFloat_ReadFloat (ref source);

        try {
            float f = float.Parse (valStr, provider: CultureInfo.InvariantCulture);
            return f;
        } catch (FormatException fe) {
            throw CreateParseException (location, fe);
        }
    }

    protected ReadOnlySpan<char> ParseIdentifier (ref ReadOnlySpan<char> source, string location) {
        SkipWhitespace (ref source);

        int i = 0;
        while (i < source.Length) {
            var c = source [i];
            if (
                !(c >= 'A' && c <= 'Z') &&
                !(c >= 'a' && c <= 'z') &&
                !(c >= '0' && c <= '9') &&
                c != '_'
            ) {
                break;
            }

            i++;
        }

        if (i == 0)
            throw CreateParseException (location, null);

        var ret = source.Slice (0, i);
        source = source.Slice (i);

        return ret;
    }

    protected ReadOnlySpan<char> ParseText (ref ReadOnlySpan<char> source, string location) {
        var valStr = ReadIgnoreWhitespace (ref source);

        if (valStr.Length < 1)
            throw CreateParseException (location, null);

        return valStr;
    }

    protected ReadOnlySpan<char> ParseString (ref ReadOnlySpan<char> source, string location) {
        SkipWhitespace (ref source);

        if (source.Length < 2 || source [0] != '"')
            throw CreateParseException (location, null);

        source = source.Slice (1);

        int endIdx = 0;
        while (endIdx < source.Length && source [endIdx] != '"')
            endIdx++;

        if (endIdx >= source.Length || source [endIdx] != '"')
            throw CreateParseException (location, null);

        var valStr = source.Slice (0, endIdx);
        source = source.Slice (endIdx + 1);

        return valStr;
    }

    protected abstract Exception CreateParseException (string location, Exception e);

    #endregion
}
