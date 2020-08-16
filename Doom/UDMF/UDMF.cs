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
using ChronosLib.Doom.UDMF.Internal;

namespace ChronosLib.Doom.UDMF {
    public class UDMFParseError {
        #region ================== Instance properties

        public int Code { get; }
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }
        public int Length { get; }
        public string Message { get; }

        #endregion

        #region ================== Constructors

        // Just for the sake of serialization
        public UDMFParseError () {
        }

        public UDMFParseError (string message, int code, UDMFToken tok) :
            this (message, code, tok.Line, tok.Column, tok.StartPos, tok.Length) {
        }

        public UDMFParseError (string message, int code, int line, int col, int pos, int length) {
            Message = message;
            Code = code;
            Line = line;
            Column = col;
            Position = pos;
            Length = length;
        }

        #endregion
    }

    [AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
    public class UDMFDataAttribute : Attribute {
        #region ================== Instance fields

        private string identifier;

        #endregion

        #region ================== Instance properties

        public virtual string Identifier { get => identifier; }

        #endregion

        #region ================== Constructors

        public UDMFDataAttribute (string name) {
            identifier = name;
        }

        #endregion
    }

    public class UDMFParser<T> : IDisposable
        where T : UDMFParsedMapData {
        #region ================== Instance fields

        protected Type dataType;
        internal UDMFParser_Internal parser;

        #endregion

        #region ================== Instance properties

        public List<UDMFParseError> Errors { get; protected set; }

        public bool IsDisposed { get; private set; }

        #endregion

        #region ================== Constructors

        public UDMFParser () {
            IsDisposed = false;
            dataType = typeof (T);
            parser = new UDMFParser_Internal (new UDMFScanner ());
        }

        #endregion

        #region ================== Instance methods

        #region Public

        public T Parse (string input) {
            if (input is null)
                throw new ArgumentNullException (nameof (input));

            return ParseInternal (input);
        }

        #endregion

        #region Protected

        protected T ParseInternal (string udmfSource) {
            parser.Errors.Clear ();
            T data = (T) parser.Parse (udmfSource, dataType);
            Errors = parser.Errors;

            return data;
        }

        #endregion

        #endregion

        #region ================== IDisposable support

        protected virtual void Dispose (bool disposing) {
            if (!IsDisposed) {
                parser?.Dispose ();

                IsDisposed = true;
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose () {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose (true);
        }

        #endregion
    }
}
