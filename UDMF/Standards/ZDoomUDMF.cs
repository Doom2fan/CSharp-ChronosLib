/*
 *  GZDoomLib - A library for using GZDoom's file formats in C#
 *  Copyright (C) 2018-2019 Chronos "phantombeta" Ouroboros
 *
 *  This program is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GZDoomLib.UDMF.Standards {
    public class UDMFZDoomVertex : UDMFVertex {
        [UDMFData ("zfloor")]
        public float ZFloor { get; set; }
        [UDMFData ("zceiling")]
        public float ZCeil { get; set; }
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
    }
}
