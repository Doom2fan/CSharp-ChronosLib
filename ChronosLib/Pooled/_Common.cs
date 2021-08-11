/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

namespace ChronosLib.Pooled {
    public enum CL_ClearMode {
        /// <summary>Reference types and value types that contain reference types are cleared when the internal arrays
        /// are returned to the pool. Value types that do not contain reference types are not cleared when returned to
        /// the pool.</summary>
        Auto = 0,
        /// <summary>Collections are always cleared.</summary>
        Always = 1,
        /// <summary>Collections are never cleared.</summary>
        Never = 2
    }
}
