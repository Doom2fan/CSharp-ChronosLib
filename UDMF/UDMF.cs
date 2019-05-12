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

using GZDoomLib.UDMF.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace GZDoomLib.UDMF {
    public class UDMFParseError {
        public int Code { get; }
        public int Line { get; }
        public int Column { get; }
        public int Position { get; }
        public int Length { get; }
        public string Message { get; }

        // just for the sake of serialization
        public UDMFParseError () {
        }

        public UDMFParseError (string message, int code, UDMFToken tok) : this (message, code, tok.Line, tok.Column, tok.StartPos, tok.Length) {
        }

        public UDMFParseError (string message, int code, int line, int col, int pos, int length) {
            Message = message;
            Code = code;
            Line = line;
            Column = col;
            Position = pos;
            Length = length;
        }
    }

    [AttributeUsage (AttributeTargets.Field | AttributeTargets.Property)]
    public class UDMFDataAttribute : Attribute {
        private string identifier;

        public UDMFDataAttribute (string name) {
            identifier = name;
        }

        public virtual string Identifier { get => identifier; }
    }

    public class UDMFParser<T>
        where T : UDMFParsedMapData {
        protected Type dataType = typeof (T);
        internal UDMFParser_Internal parser;
        public List<UDMFParseError> Errors { get; protected set; }

        public UDMFParser () {
            parser = new UDMFParser_Internal (new UDMFScanner ());
        }

        protected T ParseInternal (TextReader reader) {
            parser.Errors.Clear ();
            T data = (T) parser.Parse (reader, dataType);
            Errors = parser.Errors;

            return data;
        }

        public T Parse (TextReader input) {
            if (input is null)
                throw new ArgumentNullException (nameof (input));

            return ParseInternal (input);
        }

        public T Parse (Stream input) {
            if (input is null)
                throw new ArgumentNullException (nameof (input));
            if (!input.CanRead)
                throw new ArgumentException ("Input stream must be readable.", nameof (input));

            using (var reader = new StreamReader (input))
                return ParseInternal (reader);
        }

        public T Parse (string input) {
            if (input is null)
                throw new ArgumentNullException (nameof (input));

            using (var reader = new StringReader (input))
                return ParseInternal (reader);
        }
    }
}
