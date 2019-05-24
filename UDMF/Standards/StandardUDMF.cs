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

namespace GZDoomLib.UDMF {
    public class UDMFVertex : IUDMFBlock {
        public Dictionary<string, string> UnknownAssignments { get; set; }

        [UDMFData ("x")]
        public float X { get; set; }
        [UDMFData ("y")]
        public float Y { get; set; }
    }

    public class UDMFLinedef : IUDMFBlock {
        public Dictionary<string, string> UnknownAssignments { get; set; }

        [UDMFData ("id")]
        public int Id { get; set; } = -1;

        [UDMFData ("v1")]
        public int Vertex1 { get; set; }
        [UDMFData ("v2")]
        public int Vertex2 { get; set; }

        [UDMFData ("blocking")]
        public bool Blocking { get; set; } = false;
        [UDMFData ("blockmonsters")]
        public bool BlockMonsters { get; set; } = false;
        [UDMFData ("blockfloaters")]
        public bool BlockFloaters { get; set; } = false;
        [UDMFData ("blocksound")]
        public bool BlockSound { get; set; } = false;
        [UDMFData ("jumpover")]
        public bool JumpOverRailing { get; set; } = false;

        [UDMFData ("twosided")]
        public bool TwoSided { get; set; } = false;
        [UDMFData ("dontpegtop")]
        public bool UpperUnpegged { get; set; } = false;
        [UDMFData ("dontpegbottom")]
        public bool LowerUnpegged { get; set; } = false;

        [UDMFData ("secret")]
        public bool Secret { get; set; } = false;
        [UDMFData ("dontdraw")]
        public bool NotOnAutomap { get; set; } = false;

        [UDMFData ("mapped")]
        public bool Mapped { get; set; } = false;
        [UDMFData ("passuse")]
        public bool PassUse { get; set; } = false;

        [UDMFData ("translucent")]
        public bool Translucent { get; set; } = false;

        [UDMFData ("playercross")]
        public bool Spec_PlayerCross { get; set; } = false;
        [UDMFData ("playeruse")]
        public bool Spec_PlayerUse { get; set; } = false;
        [UDMFData ("monstercross")]
        public bool Spec_MonsterCross { get; set; } = false;
        [UDMFData ("monsteruse")]
        public bool Spec_MonsterUse { get; set; } = false;
        [UDMFData ("impact")]
        public bool Spec_ImpactActivated { get; set; } = false;
        [UDMFData ("playerpush")]
        public bool Spec_PlayerPush { get; set; } = false;
        [UDMFData ("monsterpush")]
        public bool Spec_MonsterPush { get; set; } = false;
        [UDMFData ("missilecross")]
        public bool Spec_MissileCross { get; set; } = false;
        [UDMFData ("repeatspecial")]
        public bool Spec_RepeatableAction { get; set; } = false;

        [UDMFData ("special")]
        public int Special { get; set; } = 0;
        [UDMFData ("arg0")]
        public int Arg0 { get; set; } = 0;
        [UDMFData ("arg1")]
        public int Arg1 { get; set; } = 0;
        [UDMFData ("arg2")]
        public int Arg2 { get; set; } = 0;
        [UDMFData ("arg3")]
        public int Arg3 { get; set; } = 0;
        [UDMFData ("arg4")]
        public int Arg4 { get; set; } = 0;

        [UDMFData ("sidefront")]
        public int SideFront { get; set; }
        [UDMFData ("sideback")]
        public int SideBack { get; set; } = -1;

        [UDMFData ("comment")]
        public string Comment { get; set; } = "";
    }

    public class UDMFSidedef : IUDMFBlock {
        public Dictionary<string, string> UnknownAssignments { get; set; }

        [UDMFData ("offsetx")]
        public float OffsetX { get; set; } = 0;
        [UDMFData ("offsety")]
        public float OffsetY { get; set; } = 0;

        [UDMFData ("texturetop")]
        public string UpperTexture { get; set; } = "-";
        [UDMFData ("texturemiddle")]
        public string MiddleTexture { get; set; } = "-";
        [UDMFData ("texturebottom")]
        public string BottomTexture { get; set; } = "-";

        [UDMFData ("sector")]
        public int Sector { get; set; } = 0;

        [UDMFData ("comment")]
        public string Comment { get; set; } = "";
    }

    public class UDMFSector : IUDMFBlock {
        public Dictionary<string, string> UnknownAssignments { get; set; }

        [UDMFData ("heightfloor")]
        public float FloorHeight { get; set; } = 0;
        [UDMFData ("heightceiling")]
        public float CeilHeight { get; set; } = 0;

        [UDMFData ("texturefloor")]
        public string FloorTexture { get; set; }
        [UDMFData ("textureceiling")]
        public string CeilTexture { get; set; }

        [UDMFData ("lightlevel")]
        public int LightLevel { get; set; } = 160;

        [UDMFData ("special")]
        public int Special { get; set; } = 0;
        [UDMFData ("id")]
        public int Id { get; set; } = 0;

        [UDMFData ("comment")]
        public string Comment { get; set; } = "";
    }

    public class UDMFThing : IUDMFBlock {
        public Dictionary<string, string> UnknownAssignments { get; set; }

        [UDMFData ("id")]
        public int Id { get; set; } = 0;

        [UDMFData ("x")]
        public float X { get; set; }
        [UDMFData ("y")]
        public float Y { get; set; }

        [UDMFData ("height")]
        public float Height { get; set; } = 0;

        [UDMFData ("angle")]
        public int Angle { get; set; } = 0;

        [UDMFData ("type")]
        public int Type { get; set; }

        [UDMFData ("skill1")]
        public bool Skill1 { get; set; } = false;
        [UDMFData ("skill2")]
        public bool Skill2 { get; set; } = false;
        [UDMFData ("skill3")]
        public bool Skill3 { get; set; } = false;
        [UDMFData ("skill4")]
        public bool Skill4 { get; set; } = false;
        [UDMFData ("skill5")]
        public bool Skill5 { get; set; } = false;
        [UDMFData ("ambush")]
        public bool Ambush { get; set; } = false;
        [UDMFData ("single")]
        public bool SpawnSingleplayer { get; set; } = false;
        [UDMFData ("dm")]
        public bool SpawnDeathmatch { get; set; } = false;
        [UDMFData ("coop")]
        public bool SpawnCooperative { get; set; } = false;
        [UDMFData ("friend")]
        public bool Friendly { get; set; } = false;
        [UDMFData ("dormant")]
        public bool Dormant { get; set; } = false;
        [UDMFData ("class1")]
        public bool Class1 { get; set; } = false;
        [UDMFData ("class2")]
        public bool Class2 { get; set; } = false;
        [UDMFData ("class3")]
        public bool Class3 { get; set; } = false;
        [UDMFData ("standing")]
        public bool Standing { get; set; } = false;
        [UDMFData ("strifeally")]
        public bool StrifeAlly { get; set; } = false;
        [UDMFData ("translucent")]
        public bool Translucent { get; set; } = false;
        [UDMFData ("invisible")]
        public bool Invisible { get; set; } = false;

        [UDMFData ("special")]
        public int Special { get; set; } = 0;
        [UDMFData ("arg0")]
        public int Arg0 { get; set; } = 0;
        [UDMFData ("arg1")]
        public int Arg1 { get; set; } = 0;
        [UDMFData ("arg2")]
        public int Arg2 { get; set; } = 0;
        [UDMFData ("arg3")]
        public int Arg3 { get; set; } = 0;
        [UDMFData ("arg4")]
        public int Arg4 { get; set; } = 0;

        [UDMFData ("comment")]
        public string Comment { get; set; } = "";
    }

    public class UDMFParsedMapDataStandard : UDMFParsedMapData {
        [UDMFData ("namespace")]
        public string Namespace { get; set; }

        [UDMFData ("vertex")]
        public UDMFBlockList<UDMFVertex> Vertices { get; set; }
        [UDMFData ("linedef")]
        public UDMFBlockList<UDMFLinedef> Linedefs { get; set; }
        [UDMFData ("sidedef")]
        public UDMFBlockList<UDMFSidedef> Sidedefs { get; set; }
        [UDMFData ("sector")]
        public UDMFBlockList<UDMFSector> Sectors { get; set; }
        [UDMFData ("thing")]
        public UDMFBlockList<UDMFThing> Things { get; set; }
    }
}
