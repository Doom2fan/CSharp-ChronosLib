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
using System.Diagnostics;
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
                if (disposing)
                    GC.SuppressFinalize (this);

                parser?.Dispose ();

                parser = null;

                IsDisposed = true;
            }
        }

        ~UDMFParser () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
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
