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
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Collections.Pooled;

namespace ChronosLib.Pooled {
    public class CL_PooledList<T> : PooledList<T> {
        public CL_PooledList () : base () { }
        public CL_PooledList (int count) : base (count) { }

        #region ================== IDisposable Support

        public bool IsDisposed {
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            set;
        }

        ~CL_PooledList () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of PooledList<{typeof (T).FullName}> has not been disposed.");
                Dispose (false);
            }
        }

        protected override void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing)
                    GC.SuppressFinalize (this);

                base.Dispose (disposing);

                IsDisposed = true;
            }
        }

        #endregion
    }
}
