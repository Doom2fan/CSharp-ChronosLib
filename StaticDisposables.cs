﻿/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.Text;

namespace ChronosLib {
    public static class StaticDisposables {
        private static List<IDisposable?> disposables = new List<IDisposable?> ();

        public static void AddDisposable (IDisposable? obj) {
            disposables.Add (obj);
        }

        public static void Dispose () {
            foreach (var obj in disposables)
                obj?.Dispose ();
        }
    }
}
