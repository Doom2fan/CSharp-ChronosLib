/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using ChronosLib.Pooled;

namespace ChronosLib.Doom.UDMF {
    internal interface IUDMFBlockList {
        #region ================== Methods

        void AddBlock (IUDMFBlock block);

        #endregion
    }

    /// <summary>A list of UDMF blocks.</summary>
    /// <typeparam name="T">The block's type.</typeparam>
    public class UDMFBlockList<T> : List<T>, IUDMFBlockList
        where T : IUDMFBlock {
        #region ================== Constructors

        public UDMFBlockList () : base () { }

        #endregion

        #region ================== Instance methods

        public void AddBlock (IUDMFBlock block)
            => Add ((T) block);

        public Type GetBlockType ()
            => typeof (T);

        public List<T> ToList () {
            var ret = new List<T> (Count);

            foreach (var item in this)
                ret.Add (item);

            return ret;
        }

        #endregion
    }

    /// <summary>A list of UDMF blocks.</summary>
    /// <typeparam name="T">The block's type.</typeparam>
    public class UDMFPooledBlockList<T> : CL_PooledList<T>, IUDMFBlockList
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

        public UDMFUnknownAssignment (string source, bool identifier) {
            Type = identifier ? AssignmentType.Identifier : AssignmentType.String;
            stringValueSource = source;
            stringValueStart = 0;
            stringValueLength = source.Length;

            intValue = 0;
            floatValue = float.NaN;
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

    public static class UDMFBlockExtensions {
        #region ================== Instance methods

        /// <summary>Gets an unknown assignment's value as a bool.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a bool containing the value of the assignment -or- null if the assignment doesn't exist
        /// or is not of type Bool.</returns>
        public static bool? GetUnknownAssignmentBool<T> (this T self, string assignmentName)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Bool)
                return assignment.GetBool ();

            return null;
        }

        /// <summary>Gets an unknown assignment's value as a bool.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a bool containing the value of the assignment -or- <paramref name="defaultVal"/> if the
        /// assignment doesn't exist or is not of type Bool.</returns>
        public static bool GetUnknownAssignmentBool<T> (this T self, string assignmentName, bool defaultVal)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Bool)
                return assignment.GetBool ();

            return defaultVal;
        }

        /// <summary>Gets an unknown assignment's value as an int.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a long containing the value of the assignment -or- null if the assignment doesn't exist
        /// or is not of type Int.</returns>
        public static long? GetUnknownAssignmentInt<T> (this T self, string assignmentName)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Int)
                return assignment.GetInt ();

            return null;
        }

        /// <summary>Gets an unknown assignment's value as an int.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a long containing the value of the assignment -or- <paramref name="defaultVal"/> if the
        /// assignment doesn't exist or is not of type Int.</returns>
        public static long GetUnknownAssignmentInt<T> (this T self, string assignmentName, long defaultVal)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Int)
                return assignment.GetInt ();

            return defaultVal;
        }

        /// <summary>Gets an unknown assignment's value as a float.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a double containing the value of the assignment -or- null if the assignment doesn't exist
        /// or is not of type Float.</returns>
        public static double? GetUnknownAssignmentFloat<T> (this T self, string assignmentName)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Float)
                return assignment.GetFloat ();

            return null;
        }

        /// <summary>Gets an unknown assignment's value as a float.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a double containing the value of the assignment -or- <paramref name="defaultVal"/> if the
        /// assignment doesn't exist or is not of type Float.</returns>
        public static double GetUnknownAssignmentFloat<T> (this T self, string assignmentName, double defaultVal)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Float)
                return assignment.GetFloat ();

            return defaultVal;
        }

        /// <summary>Gets an unknown assignment's value as a string.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a ReadOnlySpan<char></char> containing the value of the assignment -or-
        /// <paramref name="defaultVal"/> if the assignment doesn't exist or is not of type String.</returns>
        public static ReadOnlySpan<char> GetUnknownAssignmentString<T> (this T self, string assignmentName, ReadOnlySpan<char> defaultVal)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.String)
                return assignment.GetString ();

            return defaultVal;
        }

        /// <summary>Gets an unknown assignment's value as an identifier.</summary>
        /// <param name="assignmentName">The assignment's name.</param>
        /// <returns>Returns a ReadOnlySpan<char></char> containing the value of the assignment -or-
        /// <paramref name="defaultVal"/> if the assignment doesn't exist or is not of type Identifier.</returns>
        public static ReadOnlySpan<char> GetUnknownAssignmentIdentifier<T> (this T self, string assignmentName, ReadOnlySpan<char> defaultVal)
            where T : IUDMFBlock {
            if (self.UnknownAssignments.TryGetValue (assignmentName, out var assignment) &&
                assignment.Type == UDMFUnknownAssignment.AssignmentType.Identifier)
                return assignment.GetIdentifier ();

            return defaultVal;
        }

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
