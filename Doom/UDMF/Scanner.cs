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

using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace ChronosLib.Doom.UDMF.Internal {
    #region Scanner

    internal class UDMFScanner {
        #region ================== Instance fields

        // TODO: Cache strings to reduce allocations, maybe even not use strings for numbers.
        protected StringBuilder sb;

        protected int currentPos;

        private UDMFToken? lookAheadToken;

        #endregion

        #region ================== Instance properties

        public TextReader Reader { get; protected set; }

        protected int LineStart { get; set; }

        public int CurrentLine { get; protected set; }
        public int CurrentColumn { get => (1 + (currentPos - LineStart)); }
        public int CurrentPosition { get => currentPos; }

        #endregion

        #region ================== Constructors

        public UDMFScanner () {
            lookAheadToken = null;
        }

        #endregion

        #region ================== Instance methods

        #region Public

        public void Init (TextReader reader) {
            Reset ();
            Reader = reader;
        }

        public void Reset () {
            Reader = null;
            CurrentLine = 1;
            LineStart = 0;
            currentPos = 1;
            lookAheadToken = null;
            sb = new StringBuilder (200);
        }

        public UDMFToken GetToken (UDMFTokenType type) {
            var t = new UDMFToken (currentPos, currentPos + 1, CurrentLine, CurrentColumn);
            t.Type = type;
            return t;
        }

        /// <summary>Executes a lookahead of the next token and will advance the scan on the input string.</summary>
        /// <returns></returns>
        public UDMFToken Scan () {
            UDMFToken tok = LookAhead (); // Temporarely retrieve the lookahead
            lookAheadToken = null; // Reset lookahead token, so scanning will continue
            return tok;
        }

        /// <summary>Returns a token with the longest best match.</summary>
        /// <returns></returns>
        /// <remarks>Currently, this method does not implement keywords. "true" and "false" are treated as identifiers.</remarks>
        public UDMFToken LookAhead () {
            // This prevents double scanning and matching
            // Increased performance
            if (lookAheadToken != null
                && lookAheadToken.Value.Type != UDMFTokenType._UNDETERMINED_
                && lookAheadToken.Value.Type != UDMFTokenType._NONE_)
                return lookAheadToken.Value;

            var tok = new UDMFToken (0, 0, 0, 0);
            tok.Type = UDMFTokenType._NONE_;

            do {
                SkipWhitespace ();

                char c = ReadChar ();

                tok.StartPos = currentPos;
                tok.Line = CurrentLine;
                tok.Column = CurrentColumn;

                char peek;
                sb.Clear ();
                switch (c) {
                    case '"':
                        do {
                            c = ReadChar ();

                            if (c == '\\' && Reader.Peek () == '"') {
                                ReadChar ();
                                sb.Append ('"');
                            } else if (c != '"')
                                sb.Append (c);
                        } while (c != '"');

                        tok.Text = sb.ToString ();
                        tok.Type = UDMFTokenType.QUOTED_STRING;
                        break;
                    case '{':
                        tok.Text = "{";
                        tok.Type = UDMFTokenType.BROPEN;
                        break;
                    case '}':
                        tok.Text = "}";
                        tok.Type = UDMFTokenType.BRCLOSE;
                        break;
                    case '=':
                        tok.Text = "=";
                        tok.Type = UDMFTokenType.EQSIGN;
                        break;
                    case ';':
                        tok.Text = ";";
                        tok.Type = UDMFTokenType.SEMICOLON;
                        break;

                    case '/':
                        peek = (char) Reader.Peek ();
                        if (peek == '/') {
                            _ = ReadChar ();

                            for (int p = Reader.Peek (); p != '\n' && p != -1; p = Reader.Peek ())
                                ReadChar ();
                        } else if (peek == '*') {
                            while (true) {
                                c = ReadChar ();

                                if (c == '*' && Reader.Peek () == '/') {
                                    _ = ReadChar ();
                                    break;
                                }
                            }
                        } else
                            goto default;
                        break;

                    case '+':
                    case '-':
                    default:
                        if (c == '_' || IsLetter (c)) {
                            sb.Append (c);
                            tok.Type = UDMFTokenType.IDENTIFIER;

                            c = (char) Reader.Peek ();
                            while (IsLetterOrDigit (c) || c == '_') {
                                sb.Append (c);
                                _ = ReadChar ();

                                c = (char) Reader.Peek ();
                            }
                        } else if (IsDigit (c) || c == '+' || c == '-') {
                            sb.Append (c);

                            if (c == '0') {
                                peek = (char) Reader.Peek ();
                                if (peek == 'x' || peek == 'X') {
                                    tok.Type = UDMFTokenType.INTEGER;
                                    ParseHex (sb);
                                } else if (IsDigit (peek)) {
                                    tok.Type = UDMFTokenType.INTEGER;
                                    ParseOctal (sb);
                                } else
                                    ParseDecimal (sb, ref tok);
                            } else
                                ParseDecimal (sb, ref tok);
                        } else
                            tok.Type = UDMFTokenType._UNDETERMINED_;

                        tok.Text = sb.ToString ();
                        break;
                }
            } while (tok.Type == UDMFTokenType._NONE_);
            tok.EndPos = currentPos - 1;

            /*if (tok.Type == UDMFTokenType.IDENTIFIER && (tok.Text.Length == 4 || tok.Text.Length == 5)) {
                switch (tok.Text.ToLowerInvariant ()) {
                    case "true":
                    case "false":
                        tok.Type = UDMFTokenType.KEYWORD;
                        break;
                }
            }*/

            lookAheadToken = tok;
            return tok;
        }

        #endregion

        #region Protected

        protected bool IsWhitespace (char c) {
            switch (c) {
                case '\n':
                case '\r':
                case ' ':
                case '\t':
                    return true;

                default:
                    return false;
            }
        }

        protected bool IsDigit (char c) {
            return c >= '0' && c <= '9';
        }

        protected bool IsLower (char c) {
            return c >= 'a' && c <= 'z';
        }

        protected bool IsUpper (char c) {
            return c >= 'A' && c <= 'Z';
        }

        protected bool IsLetter (char c) {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z');
        }

        protected bool IsLetterOrDigit (char c) {
            return (c >= '0' && c <= '9') ||
                   (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z');
        }

        protected bool IsKeywordChar (char c) {
            switch (c) {
                case '{':
                case '}':
                case '(':
                case ')':
                case ';':
                case '"':
                case '\'':
                case '\\':
                case ':':
                case '\n':
                case '\r':
                case '\t':
                case ' ':
                    return false;

                default:
                    return true;
            }
        }

        protected char ReadChar () {
            var ret = (char) Reader.Read ();
            currentPos++;

            if (ret == '\n') {
                CurrentLine++;
                LineStart = currentPos;
            }

            return ret;
        }

        protected void SkipWhitespace () {
            while (IsWhitespace ((char) Reader.Peek ()))
                _ = ReadChar ();
        }

        protected void ParseHex (StringBuilder sb) {
            sb.Append (ReadChar ());

            char c = (char) Reader.Peek ();
            while (IsDigit (c) || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')) {
                sb.Append (c);
                _ = ReadChar ();

                c = (char) Reader.Peek ();
            }
        }

        protected void ParseOctal (StringBuilder sb) {
            sb.Append (ReadChar ());

            char c = (char) Reader.Peek ();
            while (c >= '0' && c <= '7') {
                sb.Append (c);
                _ = ReadChar ();

                c = (char) Reader.Peek ();
            }
        }

        protected void ParseDecimal (StringBuilder sb, ref UDMFToken tok) {
            bool foundFrac = false;
            bool foundExp = false;

            char c;
            while (true) {
                c = (char) Reader.Peek ();

                if (IsDigit (c)) {
                    sb.Append (c);
                    _ = ReadChar ();
                } else if (!foundFrac && c == '.') {
                    foundFrac = true;
                    sb.Append (c);
                    _ = ReadChar ();
                } else if (foundFrac && !foundExp && (c == 'e' || c == 'E')) {
                    foundExp = true;
                    sb.Append (c);
                    _ = ReadChar ();

                    c = ReadChar ();
                    if (c == '+' || c == '-') {
                        sb.Append (c);
                        c = ReadChar ();
                    }

                    if (!IsDigit (c)) {
                        tok.Type = UDMFTokenType._UNDETERMINED_;
                        return;
                    }

                    sb.Append (c);
                } else
                    break;
            }

            tok.Type = (foundFrac ? UDMFTokenType.FLOAT : UDMFTokenType.INTEGER);
        }

        #endregion

        #endregion
    }

    #endregion

    #region Token

    public enum UDMFTokenType {
        // Non-terminal tokens
        _NONE_ = 0,
        _UNDETERMINED_ = 1,

        // Non-terminal tokens
        Start = 2,
        Global_Expr_List = 3,
        Global_Expr = 4,
        Block = 5,
        Expr_List = 6,
        Assignment_Expr = 7,
        Value = 8,

        // Terminal tokens
        IDENTIFIER = 9,
        INTEGER = 10,
        FLOAT = 11,
        QUOTED_STRING = 12,
        KEYWORD = 13,
        BROPEN = 14,
        BRCLOSE = 15,
        EQSIGN = 16,
        SEMICOLON = 17,
        COMMENTLINE = 18,
        COMMENTBLOCK = 19,
        WHITESPACE = 20
    }

    public struct UDMFToken {
        #region ================== Static functions

        public static string TokenTypeToString (UDMFTokenType val) {
            switch (val) {
                case UDMFTokenType._NONE_:         return "None";
                case UDMFTokenType._UNDETERMINED_: return "Undetermined token";
                case UDMFTokenType.IDENTIFIER:     return "Identifier";
                case UDMFTokenType.INTEGER:        return "Integer";
                case UDMFTokenType.FLOAT:          return "Float";
                case UDMFTokenType.QUOTED_STRING:  return "String";
                case UDMFTokenType.KEYWORD:        return "Keyword";
                case UDMFTokenType.BROPEN:         return "'{'";
                case UDMFTokenType.BRCLOSE:        return "'}'";
                case UDMFTokenType.EQSIGN:         return "'='";
                case UDMFTokenType.SEMICOLON:      return "';'";
                default: return "UNDETERMINED";
            }
        }

        #endregion

        #region ================== Instance fields

        [XmlAttribute]
        public UDMFTokenType Type;

        #endregion

        #region ================== Instance properties

        public int StartPos { get; set; }
        public int EndPos { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }

        public string Text { get; set; }
        public int Length { get => (EndPos - StartPos); }

        #endregion

        #region ================== Constructors

        public UDMFToken (int start, int end, int line, int column) {
            Type = UDMFTokenType._UNDETERMINED_;

            StartPos = start;
            EndPos = end;
            Line = line;
            Column = column;
            Text = string.Empty; // Must initialize with empty string, may cause null reference exceptions otherwise
        }

        #endregion

        #region ================== Instance methods

        public void UpdateRange (UDMFToken token) {
            if (token.StartPos < StartPos)
                StartPos = token.StartPos;
            if (token.EndPos > EndPos)
                EndPos = token.EndPos;
        }

        public override string ToString () {
            if (Text != null)
                return Type.ToString () + " '" + Text + "'";
            else
                return Type.ToString ();
        }

        #endregion
    }

    #endregion
}
