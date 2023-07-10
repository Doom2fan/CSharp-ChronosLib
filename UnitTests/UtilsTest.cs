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
using ChronosLib;
using Xunit;

namespace UnitTests;

public class UtilsTests {
    //[Fact]
    public void TestSpanSplit () {
        var mapText = "1 2   3".AsSpan ();

        using var parseResults = Utils.SplitSpan (mapText, " ");
        Assert.Equal (3, parseResults.Length);
        Assert.True (mapText [parseResults.Span [0]].Equals ("1", StringComparison.OrdinalIgnoreCase));
        Assert.True (mapText [parseResults.Span [1]].Equals ("2", StringComparison.OrdinalIgnoreCase));
        Assert.True (mapText [parseResults.Span [2]].Equals ("3", StringComparison.OrdinalIgnoreCase));
    }
}
