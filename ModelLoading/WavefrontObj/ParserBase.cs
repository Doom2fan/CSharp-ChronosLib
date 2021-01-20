/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Globalization;
using System.Numerics;

namespace ChronosLib.ModelLoading.WavefrontObj {
    public abstract class ParserBase {
        protected readonly string parserWhitespaceChars;
        protected int currentLine;

        protected ParserBase (string whiteChars) {
            parserWhitespaceChars = whiteChars;
        }

        protected ReadOnlySpan<char> ReadIgnoreWhitespace (ref ReadOnlySpan<char> source) {
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

        protected Vector3 ParseVector3 (ref ReadOnlySpan<char> source, string location) {
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

        protected Vector2 ParseVector2 (ref ReadOnlySpan<char> source, string location) {
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

        protected int ParseInt (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadIgnoreWhitespace (ref source);

            try {
                int i = int.Parse (valStr, provider: CultureInfo.InvariantCulture);
                return i;
            } catch (FormatException fe) {
                throw CreateParseException (location, fe);
            }
        }

        protected float ParseFloat (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadIgnoreWhitespace (ref source);

            try {
                float f = float.Parse (valStr, provider: CultureInfo.InvariantCulture);
                return f;
            } catch (FormatException fe) {
                throw CreateParseException (location, fe);
            }
        }

        protected ReadOnlySpan<char> ParseText (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadIgnoreWhitespace (ref source);

            if (valStr.Length < 1)
                throw CreateParseException (location, null);

            return valStr;
        }

        protected abstract Exception CreateParseException (string location, Exception e);
    }
}
