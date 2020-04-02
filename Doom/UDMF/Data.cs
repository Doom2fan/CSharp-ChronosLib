/*
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
using Collections.Pooled;

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

    /// <summary>A list of UDMF blocks.</summary>
    /// <typeparam name="T">The block's type.</typeparam>
    public class UDMFPooledBlockList<T> : PooledList<T>, IUDMFBlockList
        where T : IUDMFBlock {
        #region ================== Constructors

        public UDMFPooledBlockList () : base () { }

        #endregion

        #region ================== Instance methods

        public void AddBlock (IUDMFBlock block)
            => Add ((T) block);

        public Type GetBlockType ()
            => typeof (T);

        #endregion
    }

    public struct UDMFUnknownAssignment {
        public enum AssignmentType {
            Bool,
            Int,
            Float,
            String,
            Identifier,
        }

        #region ================== Instance fields

        private long intValue;

        private double floatValue;

        private string stringValueSource;
        private int stringValueStart;
        private int stringValueLength;

        #endregion

        #region ================== Instance properties

        public AssignmentType Type { get; private set; }

        #endregion

        #region ================== Constructors

        public UDMFUnknownAssignment (bool value) {
            Type = AssignmentType.Bool;
            intValue = value ? 1 : 0;

            floatValue = float.NaN;
            stringValueSource = null;
            stringValueStart = 0;
            stringValueLength = 0;
        }

        public UDMFUnknownAssignment (long value) {
            Type = AssignmentType.Int;
            intValue = value;

            floatValue = float.NaN;
            stringValueSource = null;
            stringValueStart = 0;
            stringValueLength = 0;
        }

        public UDMFUnknownAssignment (double value) {
            Type = AssignmentType.Float;
            floatValue = value;

            intValue = 0;
            stringValueSource = null;
            stringValueStart = 0;
            stringValueLength = 0;
        }

        public UDMFUnknownAssignment (string source, int start, int length, bool identifier) {
            Type = identifier ? AssignmentType.Identifier : AssignmentType.String;
            stringValueSource = source;
            stringValueStart = start;
            stringValueLength = length;

            intValue = 0;
            floatValue = float.NaN;
        }

        #endregion

        #region ================== Instance methods

        public bool GetBool () {
            if (Type != AssignmentType.Bool)
                return false;

            return intValue == 1;
        }

        public long GetInt () {
            if (Type != AssignmentType.Int)
                return 0;

            return intValue;
        }

        public double GetFloat () {
            if (Type != AssignmentType.Float)
                return double.NaN;

            return floatValue;
        }

        public ReadOnlySpan<char> GetString () {
            if (Type != AssignmentType.String)
                return null;

            return stringValueSource.AsSpan (stringValueStart, stringValueLength);
        }

        public ReadOnlySpan<char> GetIdentifier () {
            if (Type != AssignmentType.Identifier)
                return null;

            return stringValueSource.AsSpan (stringValueStart, stringValueLength);
        }

        #endregion
    }

    public interface IUDMFBlock {
        #region ================== Properties

        /// <summary>Stores unrecognized assignments.</summary>
        Dictionary<string, UDMFUnknownAssignment> UnknownAssignments { get; set; }

        #endregion
    }
    public class UDMFUnknownBlock : IUDMFBlock {
        #region ================== Instance properties

        /// <summary>Stores the unrecognized block's assignments.</summary>
        public Dictionary<string, UDMFUnknownAssignment> UnknownAssignments { get; set; }

        #endregion
    }

    public abstract class UDMFParsedMapData {
        #region ================== Instance properties

        /// <summary>Stores unrecognized global assignments.</summary>
        public Dictionary<string, UDMFUnknownAssignment> UnknownGlobalAssignments { get; set; }
        /// <summary>Stores unrecognized blocks.</summary>
        public Dictionary<string, List<UDMFUnknownBlock>> UnknownBlocks { get; set; }

        #endregion

        #region ================== Instance methods

        /// <summary>Performs post-processing on the parsed map data.</summary>
        public virtual void PostProcessing () { }

        #endregion
    }
}
