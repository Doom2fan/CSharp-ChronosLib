/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

namespace ChronosLib.Doom.UDMF.Standards {
    public class UDMFZDoomVertex : UDMFVertex {
        #region ================== Instance properties

        [UDMFData ("zfloor")]
        public float ZFloor { get; set; }
        [UDMFData ("zceiling")]
        public float ZCeil { get; set; }

        #endregion
    }

    public class UDMFZDoomLinedef : UDMFLinedef {
    }

    public class UDMFZDoomSidedef : UDMFSidedef {
    }

    public class UDMFZDoomSector : UDMFSector {
    }

    public class UDMFZDoomThing : UDMFThing {
    }

    public class UDMFParsedMapDataZDoom : UDMFParsedMapData {
        #region ================== Instance properties

        [UDMFData ("namespace")]
        public string Namespace { get; set; }

        [UDMFData ("vertex")]
        public UDMFBlockList<UDMFZDoomVertex> Vertices { get; set; }
        [UDMFData ("linedef")]
        public UDMFBlockList<UDMFZDoomLinedef> Linedefs { get; set; }
        [UDMFData ("sidedef")]
        public UDMFBlockList<UDMFZDoomSidedef> Sidedefs { get; set; }
        [UDMFData ("sector")]
        public UDMFBlockList<UDMFZDoomSector> Sectors { get; set; }
        [UDMFData ("thing")]
        public UDMFBlockList<UDMFZDoomThing> Things { get; set; }

        #endregion
    }
}
