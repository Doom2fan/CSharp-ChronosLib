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
