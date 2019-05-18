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
    public class UDMFBlockList<T> : List<T>
        where T: IUDMFBlock {
        public UDMFBlockList () : base () {
        }
    }

    public interface IUDMFBlock {
        /// <summary>
        /// Stores unrecognized assignments.
        /// </summary>
        Dictionary<string, string> UnknownAssignments { get; set; }
    }
    public class UDMFUnknownBlock : IUDMFBlock {
        /// <summary>
        /// Stores the unrecognized block's assignments.
        /// </summary>
        public Dictionary<string, string> UnknownAssignments { get; set; }
    }

    public abstract class UDMFParsedMapData {
        /// <summary>
        /// Stores unrecognized global assignments.
        /// </summary>
        public Dictionary<string, string> UnknownGlobalAssignments { get; set; } = new Dictionary<string, string> ();
        /// <summary>
        /// Stores unrecognized blocks.
        /// </summary>
        public List<Tuple<string, UDMFUnknownBlock>> UnknownBlocks { get; set; } = new List<Tuple<string, UDMFUnknownBlock>> ();

        /// <summary>
        /// Performs postprocessing on the parsed map data.
        /// </summary>
        public virtual void PostProcessing () { }
    }
}
