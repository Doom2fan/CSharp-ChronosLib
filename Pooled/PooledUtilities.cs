/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance.Buffers;

namespace ChronosLib.Pooled {
    public static class PooledUtils {
        internal static bool IsCompatibleObject<T> (object? value) {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (value == null && default (T) == null);
        }

        internal static void EnsureNotNull<T> (object? value, string paramName) {
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            if (!(default (T) == null) && value == null)
                throw new ArgumentNullException (paramName);
        }

        #region Span<char> to string

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString (this Span<char> chars) {
            return StringPool.Shared.GetOrAdd (chars);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString (this ReadOnlySpan<char> chars) {
            return StringPool.Shared.GetOrAdd (chars);
        }

        #endregion

        #region Span<Rune> to string

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString (this Span<Rune> runes) {
            using var chars = runes.GetPooledChars ();
            return StringPool.Shared.GetOrAdd (chars.Span);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString (this ReadOnlySpan<Rune> runes) {
            using var chars = runes.GetPooledChars ();
            return StringPool.Shared.GetOrAdd (chars.Span);
        }

        #endregion

        #region Span<char> to string - lowercase, invariant culture

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString_LowerInvariant (this Span<char> str) {
            return ((ReadOnlySpan<char>) str).GetPooledString_LowerInvariant ();
        }

        public static string GetPooledString_LowerInvariant (this ReadOnlySpan<char> str) {
            // Calculate the total length after turning it lowercase. Shouldn't change, but lol Unicode so there's no guarantees.
            // We also check if we even need to do it in the first place.
            int totalLen = 0;
            bool anyChanged = false;
            foreach (var rune in str.EnumerateRunes ()) {
                var runeLower = Rune.ToLowerInvariant (rune);
                totalLen += runeLower.Utf16SequenceLength;

                anyChanged |= runeLower != rune;
            }

            if (!anyChanged)
                return str.GetPooledString ();

            // Make the CVar name lowercase.
            using var strChars = PooledArray<char>.GetArray (totalLen);
            var strCharsSpan = strChars.Span;
            foreach (var rune in str.EnumerateRunes ()) {
                int len = Rune.ToLowerInvariant (rune).EncodeToUtf16 (strCharsSpan);
                strCharsSpan = strCharsSpan.Slice (len);
            }

            return strChars.Span.GetPooledString ();
        }

        #endregion

        #region Span<Rune> to string - lowercase, invariant culture

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString_LowerInvariant (this Span<Rune> runes) {
            using var chars = runes.GetPooledChars ();
            return GetPooledString_LowerInvariant (chars);
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static string GetPooledString_LowerInvariant (this ReadOnlySpan<Rune> runes) {
            using var chars = runes.GetPooledChars ();
            return GetPooledString_LowerInvariant (chars);
        }

        #endregion

        #region Span<Rune> to char array

        public static PooledArray<char> GetPooledChars (this ReadOnlySpan<Rune> runes) {
            int totalLen = 0;
            foreach (var rune in runes)
                totalLen += rune.Utf16SequenceLength;

            var ret = PooledArray<char>.GetArray (totalLen);
            var retSpan = ret.Span;
            foreach (var rune in runes) {
                int len = rune.EncodeToUtf16 (retSpan);
                retSpan = retSpan.Slice (len);
            }

            return ret;
        }

        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        public static PooledArray<char> GetPooledChars (this Span<Rune> runes) {
            return ((ReadOnlySpan<Rune>) runes).GetPooledChars ();
        }

        #endregion
    }
}
