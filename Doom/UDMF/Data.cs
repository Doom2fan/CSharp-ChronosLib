﻿/*
 *  ChronosLib - A collection of useful things
 *  Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
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

namespace ChronosLib.Doom.UDMF {
    internal interface IUDMFBlockList {
        #region ================== Methods

        void AddBlock (IUDMFBlock block);

        #endregion
    }

    /// <summary>A list of UDMF blocks.</summary>
    /// <typeparam name="T">The block's type.</typeparam>
    public class UDMFBlockList<T> : List<T>, IUDMFBlockList
        where T: IUDMFBlock {
        #region ================== Constructors

        public UDMFBlockList () : base () { }

        #endregion

        #region ================== Instance methods

        public void AddBlock (IUDMFBlock block)
            => Add ((T) block);

        public Type GetBlockType () 
            => typeof (T);

        #endregion
    }

    public interface IUDMFBlock {
        #region ================== Properties

        /// <summary>Stores unrecognized assignments.</summary>
        Dictionary<string, string> UnknownAssignments { get; set; }

        #endregion
    }
    public class UDMFUnknownBlock : IUDMFBlock {
        #region ================== Instance properties

        /// <summary>Stores the unrecognized block's assignments.</summary>
        public Dictionary<string, string> UnknownAssignments { get; set; }

        #endregion
    }

    public abstract class UDMFParsedMapData {
        #region ================== Instance properties

        /// <summary>Stores unrecognized global assignments.</summary>
        public Dictionary<string, string> UnknownGlobalAssignments { get; set; }
        /// <summary>Stores unrecognized blocks.</summary>
        public List<Tuple<string, UDMFUnknownBlock>> UnknownBlocks { get; set; }

        #endregion

        #region ================== Instance methods

        /// <summary>Performs post-processing on the parsed map data.</summary>
        public virtual void PostProcessing () { }

        #endregion
    }
}
