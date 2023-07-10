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
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace ChronosLib.Pooled;

public class CL_PooledListPool<T> : IDisposable {
    public static CL_PooledListPool<T> Shared {
        [MethodImpl (MethodImplOptions.AggressiveInlining)]
        get;
        private set;
    }

    #region ================== Instance fields

    protected List<CL_PooledList<T>> pooledLists;
    protected List<CL_PooledList<T>> rentedLists;

    #endregion

    #region ================== Constructor

    static CL_PooledListPool () {
        Shared = new CL_PooledListPool<T> ();

        StaticDisposables.AddDisposable (Shared);
    }

    public CL_PooledListPool () {
        pooledLists = new List<CL_PooledList<T>> ();
        rentedLists = new List<CL_PooledList<T>> ();
    }

    #endregion

    #region ================== Instance methods

    public CL_PooledList<T> Rent () {
        if (pooledLists.Count > 0) {
            var list = pooledLists [^1];

            Debug.Assert (!list.IsDisposed, "The list was disposed.");

            pooledLists.RemoveAt (pooledLists.Count - 1);
            rentedLists.Add (list);

            return list;
        } else {
            var list = new CL_PooledList<T> ();

            rentedLists.Add (list);

            return list;
        }
    }

    public void Return (CL_PooledList<T> list) {
        Debug.Assert (!list.IsDisposed, "Attempted to return a disposed list.");

        list.Clear ();
        list.Capacity = 0;

        var idx = rentedLists.IndexOf (list);

        Debug.Assert (idx >= 0, "A list has been returned more than once, or this is not a pooled list.");

        rentedLists [idx] = rentedLists [^1];
        rentedLists.RemoveAt (rentedLists.Count - 1);
        pooledLists.Add (list);
    }

    #endregion

    #region ================== IDisposable Support

    public bool IsDisposed { get; private set; }

    ~CL_PooledListPool () {
        if (!IsDisposed) {
            Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
            Dispose (false);
        }
    }

    protected void Dispose (bool disposing) {
        if (!IsDisposed) {
            if (disposing)
                GC.SuppressFinalize (this);

            foreach (var list in pooledLists)
                list?.Dispose ();

            Debug.Assert (rentedLists.Count < 1, "Rented lists have been leaked.");

            foreach (var list in rentedLists)
                list?.Dispose ();

            pooledLists.Clear ();
            rentedLists.Clear ();

            IsDisposed = true;
        }
    }

    public void Dispose () {
        Dispose (true);
    }

    #endregion
}
