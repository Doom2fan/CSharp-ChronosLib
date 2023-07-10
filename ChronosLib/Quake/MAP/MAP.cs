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
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using ChronosLib.Quake.MAP.Internal;

namespace ChronosLib.Quake.MAP;

[DebuggerDisplay ("Message = {Message}")]
public struct QuakeMAPParseError {
    #region ================== Instance properties

    public int Line { get; }
    public int Column { get; }
    public int Position { get; }
    public int Length { get; }
    public string Message { get; }

    #endregion

    #region ================== Constructors

    public QuakeMAPParseError (string message, MAPToken tok) {
        Message = message;
        Line = tok.Line;
        Column = tok.Column;
        Position = tok.StartPos;
        Length = tok.Length;
    }

    public QuakeMAPParseError (string message, int line, int col, int pos, int length) {
        Message = message;
        Line = line;
        Column = col;
        Position = pos;
        Length = length;
    }

    #endregion
}

[DebuggerDisplay ("QuakeMAP (Entity count = {Entities.Count})")]
public struct QuakeMAP {
    public IList<QuakeEntity> Entities { get; init; }

    public static (QuakeMAP?, IList<QuakeMAPParseError>) Parse (ReadOnlyMemory<char> mapSource) {
        var parser = new MAPParser ();
        return (parser.Parse (mapSource), parser.Errors);
    }
}

[DebuggerDisplay ("QuakeEntity (KVP count = {KeyValuePairs.Count}, brush count = {Brushes.Length})")]
public struct QuakeEntity {
    public IDictionary<string, string> KeyValuePairs { get; init; }
    public QuakeBrush [] Brushes { get; init; }

    #region TryGet

    private const NumberStyles ParseIntStyle = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowLeadingSign;
    private const NumberStyles ParseFloatStyle = ParseIntStyle | NumberStyles.AllowDecimalPoint;

    public bool TryGetString (string key, out string? value, string? defaultValue = null) {
        if (KeyValuePairs is null || !KeyValuePairs.TryGetValue (key, out var valueString)) {
            value = defaultValue;
            return false;
        }

        value = valueString;
        return true;
    }

    public bool TryGetBool (string key, out bool value, bool defaultValue = false) {
        if (!TryGetString (key, out var valueString) || !bool.TryParse (valueString, out value)) {
            value = defaultValue;
            return false;
        }

        return true;
    }

    public bool TryGetInt (string key, out int value, int defaultValue = 0) {
        if (!TryGetString (key, out var valueString) || !int.TryParse (valueString, ParseIntStyle, CultureInfo.InvariantCulture, out value)) {
            value = defaultValue;
            return false;
        }

        return true;
    }

    public bool TryGetFloat (string key, out float value, float defaultValue = float.NaN) {
        if (!TryGetString (key, out var valueString) || !float.TryParse (valueString, ParseFloatStyle, CultureInfo.InvariantCulture, out value)) {
            value = defaultValue;
            return false;
        }

        return true;
    }

    private bool TryParseVector (string valueString, Span<float> elements) {
        using var splits = Utils.SplitSpan (valueString.AsSpan (), " ");

        if (splits.Length != elements.Length)
            return false;

        for (int i = 0; i < elements.Length; i++) {
            var elemString = valueString.AsSpan () [splits.Span [i]];
            if (!float.TryParse (elemString, ParseFloatStyle, CultureInfo.InvariantCulture, out var elementValue))
                return false;

            elements [i] = elementValue;
        }

        return true;
    }

    public bool TryGetVector2 (string key, out Vector2 value) => TryGetVector2 (key, out value, new Vector2 (float.NaN));

    public bool TryGetVector2 (string key, out Vector2 value, Vector2 defaultValue) {
        if (!TryGetString (key, out var valueString)) {
            value = defaultValue;
            return false;
        }

        Span<float> elements = stackalloc float [2];
        if (!TryParseVector (valueString!, elements)) {
            value = defaultValue;
            return false;
        }

        value = new Vector2 (elements [0], elements [1]);
        return true;
    }

    public bool TryGetVector3 (string key, out Vector3 value) => TryGetVector3 (key, out value, new Vector3 (float.NaN));

    public bool TryGetVector3 (string key, out Vector3 value, Vector3 defaultValue) {
        if (!TryGetString (key, out var valueString)) {
            value = defaultValue;
            return false;
        }

        Span<float> elements = stackalloc float [3];
        if (!TryParseVector (valueString!, elements)) {
            value = defaultValue;
            return false;
        }

        value = new Vector3 (elements [0], elements [1], elements [2]);
        return true;
    }

    public bool TryGetVector4 (string key, out Vector4 value) => TryGetVector4 (key, out value, new Vector4 (float.NaN));

    public bool TryGetVector4 (string key, out Vector4 value, Vector4 defaultValue) {
        if (!TryGetString (key, out var valueString)) {
            value = defaultValue;
            return false;
        }

        Span<float> elements = stackalloc float [4];
        if (!TryParseVector (valueString!, elements)) {
            value = defaultValue;
            return false;
        }

        value = new Vector4 (elements [0], elements [1], elements [2], elements [3]);
        return true;
    }

    #endregion

    #region Get or default

    public string? GetString (string key, string? defaultValue) {
        TryGetString (key, out var ret, defaultValue);
        return ret;
    }

    public bool GetBool (string key, bool defaultValue) {
        TryGetBool (key, out var ret, defaultValue);
        return ret;
    }

    public int GetInt (string key, int defaultValue) {
        TryGetInt (key, out var ret, defaultValue);
        return ret;
    }

    public float GetFloat (string key, float defaultValue) {
        TryGetFloat (key, out var ret, defaultValue);
        return ret;
    }

    public Vector2 GetVector2 (string key, Vector2 defaultValue) {
        TryGetVector2 (key, out var ret, defaultValue);
        return ret;
    }

    public Vector3 GetVector3 (string key, Vector3 defaultValue) {
        TryGetVector3 (key, out var ret, defaultValue);
        return ret;
    }

    public Vector4 GetVector4 (string key, Vector4 defaultValue) {
        TryGetVector4 (key, out var ret, defaultValue);
        return ret;
    }

    #endregion
}

[DebuggerDisplay ("QuakeBrush (Plane count = {Planes.Length})")]
public struct QuakeBrush {
    public QuakePlane [] Planes { get; init; }
}

public struct QuakePlane {
    public Vector3 Point1 { get; init; }
    public Vector3 Point2 { get; init; }
    public Vector3 Point3 { get; init; }

    public string Texture { get; init; }

    public bool TextureIsValve220 { get; init; }
    public Vector3 TextureAxis1 { get; init; }
    public Vector3 TextureAxis2 { get; init; }

    public Vector2 TextureOffsets { get; init; }
    public float TextureRotation { get; init; }
    public Vector2 TextureScale { get; init; }
}
