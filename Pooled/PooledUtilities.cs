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
    }
}
