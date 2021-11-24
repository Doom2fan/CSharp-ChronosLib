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
using ChronosLib.Quake.MAP;
using Xunit;

namespace UnitTests {
    public class QuakeMAPTests {
        [Fact]
        public void TestQuake () {
            string mapText = @"
            // Game: Quake
            // Format: Standard
            // entity 0
            {
                ""classname"" ""worldspawn""
                // brush 0
                {
                    ( -16 -64 -16 ) ( -16 -63 -16 ) ( -16 -64 -15 ) __TB_empty -0 -0 -0 1 1
                    ( -64 -16 -16 ) ( -64 -16 -15 ) ( -63 -16 -16 ) __TB_empty -0 -0 -0 1 1
                    ( -64 -64 -16 ) ( -63 -64 -16 ) ( -64 -63 -16 ) __TB_empty 0 0 0 1 1
                    ( 64 64 16 ) ( 64 65 16 ) ( 65 64 16 ) __TB_empty 0 0 0 1 1
                    ( 64 16 16 ) ( 65 16 16 ) ( 64 16 17 ) __TB_empty -0 -0 -0 1 1
                    ( 16 64 16 ) ( 16 64 17 ) ( 16 65 16 ) __TB_empty -0 -0 -0 1 1
                }
            }
            ";

            var parseResults = QuakeMAP.Parse (mapText.AsMemory ());
            Assert.True (parseResults.Item1.HasValue);
            Assert.Empty (parseResults.Item2);
        }

        [Fact]
        public void TestValve220 () {
            string mapText = @"
            // Game: Quake
            // Format: Valve
            // entity 0
            {
                ""classname"" ""worldspawn""
                ""mapversion"" ""220""
                // brush 0
                {
                    ( -16 -64 -16 ) ( -16 -63 -16 ) ( -16 -64 -15 ) __TB_empty [ 0 -1 0 -0 ] [ 0 0 -1 -0 ] -0 1 1
                    ( -64 -16 -16 ) ( -64 -16 -15 ) ( -63 -16 -16 ) __TB_empty [ 1 0 0 -0 ] [ 0 0 -1 -0 ] -0 1 1
                    ( -64 -64 -16 ) ( -63 -64 -16 ) ( -64 -63 -16 ) __TB_empty [ -1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1
                    ( 64 64 16 ) ( 64 65 16 ) ( 65 64 16 ) __TB_empty [ 1 0 0 0 ] [ 0 -1 0 0 ] 0 1 1
                    ( 64 16 16 ) ( 65 16 16 ) ( 64 16 17 ) __TB_empty [ -1 0 0 -0 ] [ 0 0 -1 -0 ] -0 1 1
                    ( 16 64 16 ) ( 16 64 17 ) ( 16 65 16 ) __TB_empty [ 0 1 0 -0 ] [ 0 0 -1 -0 ] -0 1 1
                }
            }
            ";

            var parseResults = QuakeMAP.Parse (mapText.AsMemory ());
            Assert.True (parseResults.Item1.HasValue);
            Assert.Empty (parseResults.Item2);
        }
    }
}
