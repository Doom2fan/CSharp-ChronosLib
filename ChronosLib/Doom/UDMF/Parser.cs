/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */
// Part of the code was generated by TinyPG.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using ChronosLib.Pooled;
using ChronosLib.Reflection;
using ChronosLib.StringPooling;
using Collections.Pooled;
using StrPool = CommunityToolkit.HighPerformance.Buffers.StringPool;

namespace ChronosLib.Doom.UDMF.Internal;

internal sealed class ParserInfo {
    public interface IAssignmentInfo {
        #region ================== Instance properties

        Type PropType { get; }

        #endregion
    }

    public sealed class AssignmentInfo<T> : IAssignmentInfo {
        #region ================== Instance fields

        public PropertyDelegates<T> Delegates;

        #endregion

        #region ================== Instance properties

        public Type PropType { get; set; }

        #endregion

        #region ================== Constructors

        public AssignmentInfo (PropertyInfo prop) {
            PropType = prop.PropertyType;
            Delegates = prop.CreateSetGetDelegates<T> (false, true);
        }

        #endregion

        #region ================== Instance methods

        public void Assign (object self, T val) {
            Delegates.Setter (self, val);
        }

        #endregion
    }

    public struct BlockInfo {
        #region ================== Instance fields

        public Type BlockType;
        public PropertyDelegates<IUDMFBlockList> Delegates;
        public Dictionary<string, IAssignmentInfo> Assignments;

        #endregion
    }

    #region ================== Instance fields

    public readonly Dictionary<string, BlockInfo> Blocks;
    public readonly Dictionary<string, IAssignmentInfo> GlobalAssignments;

    #endregion

    #region ================== Constructors

    public ParserInfo (Type dataType) {
        var udmfDataInfo = dataType.GetProperties ();

        Blocks = new Dictionary<string, BlockInfo> (udmfDataInfo.Length, StringComparer.InvariantCultureIgnoreCase);
        GlobalAssignments = new Dictionary<string, IAssignmentInfo> (udmfDataInfo.Length, StringComparer.InvariantCultureIgnoreCase);

        foreach (var prop in udmfDataInfo) {
            var dataAttr = prop.GetCustomAttribute<UDMFDataAttribute> ();
            var type = prop.PropertyType;

            if (dataAttr is null)
                continue;

            if (UDMFParser_Internal.IsUDMFType (type)) {
                GlobalAssignments.Add (
                    dataAttr.Identifier,
                    GetAssignmentInfo (prop)
                );
            } else if (type.IsGenericType && type.GetGenericTypeDefinition () == typeof (UDMFBlockList<>)) {
                var blockInfo = new BlockInfo ();
                var blockType = type.GetGenericArguments () [0];

                blockInfo.BlockType = blockType;
                blockInfo.Delegates = prop.CreateSetGetDelegates<IUDMFBlockList> (true, true);
                GetBlockInfo (blockType, ref blockInfo);

                Blocks.Add (dataAttr.Identifier, blockInfo);
            }
        }
    }

    #endregion

    #region ================== Instance methods

    public void InitializeDataClass (UDMFParsedMapData data) {
        foreach (var block in Blocks.Values) {
            var propVal = block.Delegates.Getter (data);

            if (propVal is null) {
                propVal = (IUDMFBlockList) Activator.CreateInstance (block.Delegates.Info.PropertyType);
                block.Delegates.Setter (data, propVal);
            }
        }
    }

    private IAssignmentInfo GetAssignmentInfo (PropertyInfo prop) {
        if (prop.PropertyType == typeof (bool)) return new AssignmentInfo<bool> (prop);
        else if (prop.PropertyType == typeof (int)) return new AssignmentInfo<int> (prop);
        else if (prop.PropertyType == typeof (uint)) return new AssignmentInfo<uint> (prop);
        else if (prop.PropertyType == typeof (long)) return new AssignmentInfo<long> (prop);
        else if (prop.PropertyType == typeof (ulong)) return new AssignmentInfo<ulong> (prop);
        else if (prop.PropertyType == typeof (float)) return new AssignmentInfo<float> (prop);
        else if (prop.PropertyType == typeof (double)) return new AssignmentInfo<double> (prop);
        else if (prop.PropertyType == typeof (string)) return new AssignmentInfo<string> (prop);

        throw new ArgumentException ("", nameof (prop));
    }

    private void GetBlockInfo (Type type, ref BlockInfo blockInfo) {
        var udmfDataInfo = type.GetProperties ();

        blockInfo.Assignments = new Dictionary<string, IAssignmentInfo> (udmfDataInfo.Length, StringComparer.InvariantCultureIgnoreCase);

        foreach (var prop in udmfDataInfo) {
            var dataAttr = prop.GetCustomAttribute<UDMFDataAttribute> ();

            if (dataAttr is null)
                continue;

            blockInfo.Assignments.Add (
                dataAttr.Identifier,
                GetAssignmentInfo (prop)
            );
        }
    }

    #endregion
}

internal sealed class UDMFParser_Internal : IDisposable {
    #region ================== Instance fields

    private static Dictionary<Type, ParserInfo> parserInfoList;
    private static StringPool namePool;

    private PooledDictionary<string, UDMFUnknownAssignment> unknownGlobalAssignmentsPooled;
    private PooledDictionary<string, UDMFUnknownAssignment> unknownAssignmentsPooled;
    private PooledDictionary<string, CL_PooledList<UDMFUnknownBlock>> unknownBlocksPooled;

    private UDMFScanner scanner;

    #endregion

    #region ================== Instance properties

    public List<UDMFParseError> Errors { get; set; }

    #endregion

    #region ================== Constructors

    static UDMFParser_Internal () {
        parserInfoList = new Dictionary<Type, ParserInfo> ();
        namePool = new StringPool ();
    }

    public UDMFParser_Internal (UDMFScanner scanner) {
        this.scanner = scanner;
        Errors = new List<UDMFParseError> ();

        unknownGlobalAssignmentsPooled = new PooledDictionary<string, UDMFUnknownAssignment> (StringComparer.InvariantCultureIgnoreCase);
        unknownAssignmentsPooled = new PooledDictionary<string, UDMFUnknownAssignment> (StringComparer.InvariantCultureIgnoreCase);
        unknownBlocksPooled = new PooledDictionary<string, CL_PooledList<UDMFUnknownBlock>> (StringComparer.InvariantCultureIgnoreCase);
    }

    #endregion

    #region ================== Instance methods

    #region Public & internal

    public UDMFParsedMapData Parse (string udmfSource, Type dataType) {
        scanner.Init (udmfSource);

        GetParserInfo (dataType);

        var data = (UDMFParsedMapData) Activator.CreateInstance (dataType);
        var info = GetParserInfo (dataType);
        info.InitializeDataClass (data);

        ParseGlobal_Expr_List (data, info);
        if (Errors.Count < 1)
            data.PostProcessing ();

        scanner.Reset ();
        unknownGlobalAssignmentsPooled.Clear ();
        unknownAssignmentsPooled.Clear ();

        return data;
    }

    internal static bool IsUDMFType (Type type) {
        switch (Type.GetTypeCode (type)) {
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.String:
            case TypeCode.Boolean:
                return true;

            default:
                return false;
        }
    }

    #endregion

    #region Private

    private ParserInfo GetParserInfo (Type dataType) {
        if (!parserInfoList.TryGetValue (dataType, out ParserInfo info)) {
            info = new ParserInfo (dataType);
            parserInfoList.Add (dataType, info);
        }

        return info;
    }

    private bool? BoolFromSpan (ReadOnlySpan<char> text) {
        if (text.Equals ("true".AsSpan (), StringComparison.InvariantCultureIgnoreCase))
            return true;
        else if (text.Equals ("false".AsSpan (), StringComparison.InvariantCultureIgnoreCase))
            return false;

        return null;
    }

    private string CleanUpQuotedString (ReadOnlySpan<char> text) {
        using var newStringData = PooledArray<char>.GetArray (text.Length);
        var newStringSpan = newStringData.Span;
        int textLen = text.Length - 1;

        int stringLength = 0;
        for (int i = 1; i < textLen; i++) {
            char c = text [i];

            if (c == '\\' && i + 1 < textLen) {
                char c2 = text [i + 1];

                if (c2 == '"') {
                    newStringSpan [stringLength] = '"';
                    i++;
                } else if (c2 == '\\') {
                    newStringSpan [stringLength] = '\\';
                    i++;
                }
            } else
                newStringSpan [stringLength] = c;

            stringLength++;
        }

        return StrPool.Shared.GetOrAdd (newStringSpan.Slice (0, stringLength));
        //valTokText.Slice (1, valTokText.Length - 2).Replace ("\\\"", "\"").Replace (@"\\", @"\")
    }

    private UDMFUnknownAssignment GetUnknownAssignment (UDMFToken tok) {
        var tokText = tok.Text;

        switch (tok.Type) {
            case UDMFTokenType.INTEGER: {
                if (!long.TryParse (tokText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var intVal))
                    long.TryParse (tokText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out intVal);
                return new UDMFUnknownAssignment (intVal);
            }

            case UDMFTokenType.FLOAT: {
                double.TryParse (tokText, NumberStyles.Integer | NumberStyles.Float, CultureInfo.InvariantCulture, out var floatVal);
                return new UDMFUnknownAssignment (floatVal);
            }

            case UDMFTokenType.IDENTIFIER: {
                bool? boolVal = BoolFromSpan (tokText);

                if (boolVal.HasValue)
                    return new UDMFUnknownAssignment (boolVal.Value);
                else
                    return new UDMFUnknownAssignment (tok.SourceString, tok.StartPos, tok.Length, true);
            }

            case UDMFTokenType.QUOTED_STRING: {
                var newText = CleanUpQuotedString (tokText);
                return new UDMFUnknownAssignment (newText, 0, newText.Length, false);
            }

            default:
                throw new ArgumentException ("Invalid token type.", nameof (tok));
        }
    }

    private void ParseGlobal_Expr_List (UDMFParsedMapData dataClass, ParserInfo info) {
        unknownGlobalAssignmentsPooled.Clear ();
        unknownBlocksPooled.Clear ();

        UDMFToken tok = scanner.LookAhead ();
        while (tok.Type == UDMFTokenType.IDENTIFIER) {
            ParseGlobal_Expr (dataClass, info);

            tok = scanner.LookAhead ();
        }

        if (unknownGlobalAssignmentsPooled.Count > 0) {
            dataClass.UnknownGlobalAssignments = new Dictionary<string, UDMFUnknownAssignment> (
                unknownGlobalAssignmentsPooled,
                StringComparer.InvariantCultureIgnoreCase
            );

            unknownGlobalAssignmentsPooled.Clear ();
        }

        if (unknownBlocksPooled.Count > 0) {
            dataClass.UnknownBlocks = new Dictionary<string, List<UDMFUnknownBlock>> (unknownBlocksPooled.Count);
            foreach (var kvp in unknownBlocksPooled) {
                var pooledList = kvp.Value;
                var newList = new List<UDMFUnknownBlock> (pooledList.Count);

                foreach (var block in pooledList)
                    newList.Add (block);

                dataClass.UnknownBlocks.Add (kvp.Key, newList);

                pooledList.Clear ();
                pooledList.Capacity = 0;
            }

            unknownBlocksPooled.Clear ();
        }
    }

    private void ParseGlobal_Expr (UDMFParsedMapData dataClass, ParserInfo info) {
        UDMFToken tok = scanner.Scan ();
        if (tok.Type != UDMFTokenType.IDENTIFIER) {
            Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.IDENTIFIER), 0x1001, tok));
            return;
        }

        var ident = tok;
        var identText = namePool.GetOrCreate (ident.Text);

        tok = scanner.LookAhead ();
        switch (tok.Type) {
            case UDMFTokenType.BROPEN:
                ParserInfo.BlockInfo block;
                info.Blocks.TryGetValue (identText, out block);
                ParseBlock (dataClass, identText, block);
                break;
            case UDMFTokenType.EQSIGN:
                if (info.GlobalAssignments.TryGetValue (identText, out var assignment))
                    ParseAssignment_Expr (dataClass, assignment);
                else {
                    var val = ParseAssignment_Expr (dataClass, null);
                    unknownGlobalAssignmentsPooled.Add (identText, GetUnknownAssignment (val.Value));
                }
                break;
            default:
                Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found.", 0x0002, tok));
                return;
        }

        return;
    }

    private void ParseBlock (UDMFParsedMapData dataClass, string ident, ParserInfo.BlockInfo? info) {
        UDMFToken tok = scanner.Scan ();
        if (tok.Type != UDMFTokenType.BROPEN) {
            Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.BROPEN), 0x1001, tok));
            return;
        }

        IUDMFBlock block;
        if (info != null) {
            block = (IUDMFBlock) Activator.CreateInstance (info.Value.BlockType);
            info.Value.Delegates.Getter (dataClass).AddBlock (block);
        } else {
            var newBlock = new UDMFUnknownBlock ();
            block = newBlock;

            if (!unknownBlocksPooled.TryGetValue (ident, out var unknownBlocksList)) {
                unknownBlocksList = new CL_PooledList<UDMFUnknownBlock> ();
                unknownBlocksPooled.Add (ident, unknownBlocksList);
            }

            unknownBlocksList.Add (newBlock);
        }

        ParseExpr_List (block, info);

        tok = scanner.Scan ();
        if (tok.Type != UDMFTokenType.BRCLOSE) {
            Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.BRCLOSE), 0x1001, tok));
            return;
        }
    }

    private void ParseExpr_List (IUDMFBlock block, ParserInfo.BlockInfo? info) {
        UDMFToken tok = scanner.LookAhead ();
        while (tok.Type == UDMFTokenType.IDENTIFIER) {
            tok = scanner.Scan ();
            if (tok.Type != UDMFTokenType.IDENTIFIER) {
                Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.IDENTIFIER), 0x1001, tok));
                return;
            }

            var tokStr = namePool.GetOrCreate (tok.Text);
            if (info != null && info.Value.Assignments.TryGetValue (namePool.GetOrCreate (tokStr), out var assignment))
                ParseAssignment_Expr (block, assignment);
            else {
                var val = ParseAssignment_Expr (block, null);

                unknownAssignmentsPooled.Add (tokStr, GetUnknownAssignment (val.Value));
            }

            tok = scanner.LookAhead ();
        }

        if (unknownAssignmentsPooled.Count > 0) {
            block.UnknownAssignments = new Dictionary<string, UDMFUnknownAssignment> (StringComparer.InvariantCultureIgnoreCase);
            block.UnknownAssignments.EnsureCapacity (unknownAssignmentsPooled.Count);
            foreach (var kvp in unknownAssignmentsPooled)
                block.UnknownAssignments.Add (kvp.Key, kvp.Value);

            unknownAssignmentsPooled.Clear ();
        }
    }

    private static void SetAssignmentInfo<T> (ParserInfo.IAssignmentInfo info, object self, T val) {
        ((ParserInfo.AssignmentInfo<T>) info).Assign (self, val);
    }

    private UDMFToken? ParseAssignment_Expr (object block, ParserInfo.IAssignmentInfo data) {
        UDMFToken tok = scanner.Scan ();
        if (tok.Type != UDMFTokenType.EQSIGN) {
            Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.EQSIGN), 0x1001, tok));
            return null;
        }

        var valTok = scanner.Scan ();
        var valTokText = valTok.Text;
        if (data != null) {
            switch (Type.GetTypeCode (data.PropType)) {
                case TypeCode.Boolean: {
                    bool? val = BoolFromSpan (valTokText);

                    if (!val.HasValue) {
                        Errors.Add (new UDMFParseError ("Expected bool, got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }

                    SetAssignmentInfo (data, block, val.Value);
                }
                break;

                case TypeCode.Int32: {
                    if (valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.INTEGER) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    if (!int.TryParse (valTokText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        int.TryParse (valTokText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val);

                    SetAssignmentInfo (data, block, val);
                }
                break;
                case TypeCode.Int64: {
                    if (valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.INTEGER) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    if (!uint.TryParse (valTokText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        uint.TryParse (valTokText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val);
                    SetAssignmentInfo (data, block, val);
                }
                break;
                case TypeCode.UInt32: {
                    if (valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.INTEGER) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    if (!long.TryParse (valTokText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        long.TryParse (valTokText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val);
                    SetAssignmentInfo (data, block, val);
                }
                break;
                case TypeCode.UInt64: {
                    if (valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.INTEGER) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    if (!ulong.TryParse (valTokText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var val))
                        ulong.TryParse (valTokText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out val);
                    SetAssignmentInfo (data, block, val);
                }
                break;

                case TypeCode.Single: {
                    if (valTok.Type != UDMFTokenType.FLOAT && valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.FLOAT) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    float.TryParse (valTokText, NumberStyles.Integer | NumberStyles.Float, CultureInfo.InvariantCulture, out var val);
                    SetAssignmentInfo (data, block, val);
                }
                break;
                case TypeCode.Double: {
                    if (valTok.Type != UDMFTokenType.FLOAT && valTok.Type != UDMFTokenType.INTEGER) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.FLOAT) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    double.TryParse (valTokText, NumberStyles.Integer | NumberStyles.Float, CultureInfo.InvariantCulture, out var val);
                    SetAssignmentInfo (data, block, val);
                }
                break;

                case TypeCode.String:
                    if (valTok.Type != UDMFTokenType.QUOTED_STRING) {
                        Errors.Add (new UDMFParseError ("Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.QUOTED_STRING) + ", got " + UDMFToken.TokenTypeToString (valTok.Type) + ".", 0x1001, valTok));
                        break;
                    }
                    SetAssignmentInfo (data, block, CleanUpQuotedString (valTokText));
                break;

                default:
                    throw new NotImplementedException ();
            }
        }

        tok = scanner.Scan ();
        if (tok.Type != UDMFTokenType.SEMICOLON) {
            Errors.Add (new UDMFParseError ("Unexpected token '" + tok.Text.ToString ().Replace ("\n", "") + "' found. Expected " + UDMFToken.TokenTypeToString (UDMFTokenType.SEMICOLON), 0x1001, tok));
            return null;
        }

        return valTok;
    }

    #endregion

    #endregion

    #region ================== IDisposable support

    private bool disposedValue = false; // To detect redundant calls

    void Dispose (bool disposing) {
        if (!disposedValue) {
            if (disposing)
                GC.SuppressFinalize (this);

            unknownGlobalAssignmentsPooled?.Dispose ();
            unknownAssignmentsPooled?.Dispose ();
            if (unknownBlocksPooled != null) {
                foreach (var kvp in unknownBlocksPooled)
                    kvp.Value?.Dispose ();

                unknownBlocksPooled.Dispose ();
            }

            disposedValue = true;
        }
    }

    ~UDMFParser_Internal () {
        if (!disposedValue) {
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
