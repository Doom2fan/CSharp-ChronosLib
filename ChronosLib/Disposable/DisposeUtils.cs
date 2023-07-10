/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;

namespace ChronosLib.Disposable;

public static class CL_DisposeUtils {
    public static AutoDisposer<T> AutoDispose<T> (this ref T val) where T : struct, IDisposable => new (ref val);
}