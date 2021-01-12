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

        protected ReadOnlySpan<char> ReadUntil (ref ReadOnlySpan<char> source, ReadOnlySpan<char> values) {
            int charIdx = -1;
            foreach (var value in values)
                charIdx = Math.Max (charIdx, source.IndexOf (value));

            if (charIdx == -1)
                charIdx = source.Length;

            var text = source.Slice (0, charIdx);

            int endIdx = charIdx;
            while (endIdx < source.Length) {
                bool foundVal = false;
                foreach (var value in values) {
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

        protected Vector3 ParseVector3 (ref ReadOnlySpan<char> source, string location) {
            var xStr = ReadUntil (ref source, parserWhitespaceChars);
            var yStr = ReadUntil (ref source, parserWhitespaceChars);
            var zStr = ReadUntil (ref source, parserWhitespaceChars);

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
            var xStr = ReadUntil (ref source, parserWhitespaceChars);
            var yStr = ReadUntil (ref source, parserWhitespaceChars);

            try {
                float x = float.Parse (xStr, provider: CultureInfo.InvariantCulture);
                float y = float.Parse (yStr, provider: CultureInfo.InvariantCulture);

                return new Vector2 (x, y);
            } catch (FormatException fe) {
                throw CreateParseException (location, fe);
            }
        }

        protected int ParseInt (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadUntil (ref source, parserWhitespaceChars);

            try {
                int i = int.Parse (valStr, provider: CultureInfo.InvariantCulture);
                return i;
            } catch (FormatException fe) {
                throw CreateParseException (location, fe);
            }
        }

        protected float ParseFloat (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadUntil (ref source, parserWhitespaceChars);

            try {
                float f = float.Parse (valStr, provider: CultureInfo.InvariantCulture);
                return f;
            } catch (FormatException fe) {
                throw CreateParseException (location, fe);
            }
        }

        protected ReadOnlySpan<char> ParseText (ref ReadOnlySpan<char> source, string location) {
            var valStr = ReadUntil (ref source, parserWhitespaceChars);

            if (valStr.Length < 1)
                throw CreateParseException (location, null);

            return valStr;
        }

        protected abstract Exception CreateParseException (string location, Exception e);
    }
}
