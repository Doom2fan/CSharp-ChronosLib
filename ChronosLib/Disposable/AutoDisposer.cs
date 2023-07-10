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

public ref struct AutoDisposer<T> where T : IDisposable {
    private ref T disposable;

    public AutoDisposer (ref T val) => disposable = ref val;

    public void Dispose () => disposable.Dispose ();
}
