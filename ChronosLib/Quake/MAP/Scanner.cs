/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

#nullable enable

using System;

namespace ChronosLib.Quake.MAP.Internal;

[NonCopyable]
internal ref struct MAPScanner {
    #region ================== Instance fields

    private int currentPos;

    private bool hasLookahead;
    private MAPToken lookAheadToken;

    #endregion

    #region ================== Instance properties

    public ReadOnlyMemory<char> TextSource { get; private set; }

    private int LineStart { get; set; }

    public int CurrentLine { get; private set; }
    public int CurrentColumn { get => (1 + (currentPos - LineStart)); }
    public int CurrentPosition { get => currentPos; }

    #endregion

    #region ================== Instance methods

    #region Public

    public void Init (ReadOnlyMemory<char> textSource) {
        Reset ();
        TextSource = textSource;
    }

    public void Reset () {
        CurrentLine = 1;
        LineStart = 0;
        currentPos = 0;
        hasLookahead = false;
    }

    /// <summary>Executes a lookahead of the next token and will advance the scan on the input string.</summary>
    /// <returns></returns>
    public MAPToken Read () {
        MAPToken tok = Peek (); // Temporarely retrieve the lookahead
        hasLookahead = false; // Reset lookahead token, so scanning will continue
        return tok;
    }

    /// <summary>Returns a token with the longest best match.</summary>
    /// <returns></returns>
    public MAPToken Peek () {
        if (hasLookahead
            && lookAheadToken.Type != MAPTokenType.Undetermined
            && lookAheadToken.Type != MAPTokenType.None)
            return lookAheadToken;

        var type = MAPTokenType.None;
        int startPos;
        int line;
        int column;

        do {
            SkipWhitespace ();

            startPos = currentPos;
            line = CurrentLine;
            column = CurrentColumn;

            char c = ReadChar ();

            char peek;
            switch (c) {
                case '\0':
                    if (currentPos < TextSource.Length)
                        goto default;

                    type = MAPTokenType.EOF;
                    break;

                case '"':
                    do {
                        c = ReadChar ();
                    } while (c != '"');

                    type = MAPTokenType.QuotedString;
                    break;

                case '{':
                    type = MAPTokenType.BraceOpen;
                    break;
                case '}':
                    type = MAPTokenType.BraceClose;
                    break;

                case '(':
                    type = MAPTokenType.ParensOpen;
                    break;
                case ')':
                    type = MAPTokenType.ParensClose;
                    break;

                case '[':
                    type = MAPTokenType.BracketOpen;
                    break;
                case ']':
                    type = MAPTokenType.BracketClose;
                    break;

                case '/':
                    peek = (char) PeekChar ();
                    if (peek == '/') {
                        _ = ReadChar ();

                        for (int p = PeekChar (); p != '\n' && p != -1; p = PeekChar ())
                            ReadChar ();
                    } else
                        goto default;
                    break;

                //case '+':
                case '-':
                case var _ when IsDigit (c):
                    ParseDecimal (ref type);
                    break;

                default:
                    if (IsTextChar (c, true)) {
                        type = MAPTokenType.Text;

                        c = (char) PeekChar ();
                        while (IsTextChar (c, false)) {
                            _ = ReadChar ();

                            c = (char) PeekChar ();
                        }
                    } else
                        type = MAPTokenType.Undetermined;
                    break;
            }
        } while (type == MAPTokenType.None);

        hasLookahead = true;
        lookAheadToken = new (TextSource, type, startPos, currentPos, line, column);
        return lookAheadToken;
    }

    #endregion

    #region Private

    private bool IsWhitespace (char c) {
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

    private bool IsDigit (char c) {
        return c >= '0' && c <= '9';
    }

    private bool IsLetter (char c) {
        return (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z');
    }

    private bool IsLetterOrDigit (char c) {
        return (c >= '0' && c <= '9') ||
               (c >= 'a' && c <= 'z') ||
               (c >= 'A' && c <= 'Z');
    }

    private bool IsTextChar (char c, bool noTokens) {
        if (!noTokens) {
            if (c == '+' || c == '-' ||
                c == '{' || c == '}' ||
                c == '(' || c == ')')
                return true;
        }

        return IsLetterOrDigit (c) ||
                c == '_' ||
                c == '*' ||
                c == '=' ||
                c == '/' ||
                c == '\\';
    }

    private int PeekChar () {
        if (currentPos >= TextSource.Length)
            return -1;

        return TextSource.Span [currentPos];
    }

    private char ReadChar () {
        if (currentPos >= TextSource.Length)
            return '\0';

        var ret = TextSource.Span [currentPos];
        currentPos++;

        if (ret == '\n') {
            CurrentLine++;
            LineStart = currentPos;
        }

        return ret;
    }

    private void SkipWhitespace () {
        while (IsWhitespace ((char) PeekChar ()))
            _ = ReadChar ();
    }

    private void ParseDecimal (ref MAPTokenType type) {
        var foundFrac = false;
        var foundExponent = false;

        char c;
        while (true) {
            c = (char) PeekChar ();

            if (IsDigit (c)) {
                _ = ReadChar ();
            } else if (!foundFrac && c == '.') {
                foundFrac = true;
                _ = ReadChar ();
            } else if (!foundExponent && c == 'e') {
                foundFrac = true;
                foundExponent = true;
                _ = ReadChar ();

                c = (char) PeekChar ();
                if (c == '+' || c == '-')
                    _ = ReadChar ();
            } else
                break;
        }

        type = (foundFrac || foundExponent) ? MAPTokenType.Float : MAPTokenType.Integer;
    }

    #endregion

    #endregion
}

public enum MAPTokenType {
    None,
    Undetermined,
    EOF,

    Text,
    Integer,
    Float,
    QuotedString,
    BraceOpen,
    BraceClose,
    ParensOpen,
    ParensClose,
    BracketOpen,
    BracketClose,
}

public ref struct MAPToken {
    #region ================== Instance properties

    public MAPTokenType Type { get; init; }

    public ReadOnlySpan<char> Text { get; init; }
    public int StartPos { get; init; }
    public int EndPos { get; init; }
    public int Line { get; init; }
    public int Column { get; init; }

    public int Length { get => (EndPos - StartPos); }

    #endregion

    #region ================== Constructors

    public MAPToken (ReadOnlyMemory<char> srcStr, MAPTokenType type, int start, int end, int line, int column) {
        Type = type;

        Text = srcStr.Span.Slice (start, end - start);
        StartPos = start;
        EndPos = end;
        Line = line;
        Column = column;
    }

    #endregion

    #region ================== Static methods

    public static string TokenTypeToString (MAPTokenType val) {
        switch (val) {
            case MAPTokenType.None: return "None";
            case MAPTokenType.Undetermined: return "Undetermined token";
            case MAPTokenType.EOF: return "EOF";

            case MAPTokenType.Text: return "Text";
            case MAPTokenType.Integer: return "Integer";
            case MAPTokenType.Float: return "Float";
            case MAPTokenType.QuotedString: return "String";

            case MAPTokenType.BraceOpen: return "'{'";
            case MAPTokenType.BraceClose: return "'}'";

            case MAPTokenType.ParensOpen: return "'('";
            case MAPTokenType.ParensClose: return "')'";

            case MAPTokenType.BracketOpen: return "'['";
            case MAPTokenType.BracketClose: return "']'";

            default: throw new Exception ("Token type not implemented.");
        }
    }

    #endregion

    #region ================== Instance methods

    public override string ToString () {
        if (Text != null)
            return Type.ToString () + " '" + Text.ToString () + "'";
        else
            return Type.ToString ();
    }

    #endregion
}
