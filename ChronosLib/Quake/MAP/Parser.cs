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
using System.Numerics;
using ChronosLib.Pooled;

namespace ChronosLib.Quake.MAP.Internal;

[NonCopyable]
internal ref struct MAPParser {
    public List<QuakeMAPParseError> Errors { get; private set; }

    public QuakeMAP? Parse (ReadOnlyMemory<char> source) {
        if (Errors is null)
            Errors = new List<QuakeMAPParseError> ();
        else
            Errors.Clear ();

        var scanner = new MAPScanner ();
        scanner.Init (source);

        var entitiesList = new List<QuakeEntity> ();
        while (scanner.Peek ().Type != MAPTokenType.EOF) {
            var entity = ParseEntity (ref scanner);

            if (!entity.HasValue)
                return null;

            entitiesList.Add (entity.Value);
        }

        if (Errors.Count > 0)
            return null;

        return new QuakeMAP {
            Entities = entitiesList,
        };
    }

    private QuakeEntity? ParseEntity (ref MAPScanner scanner) {
        var tk = scanner.Read ();
        if (tk.Type != MAPTokenType.BraceOpen) {
            Errors.Add (new QuakeMAPParseError ($"Expected '{{', got '{tk.Text.ToString ()}'.", tk));
            return null;
        }

        var keyValuePairs = new Dictionary<string, string> ();
        using var brushes = new StructPooledList<QuakeBrush> (CL_ClearMode.Auto);

        tk = scanner.Peek ();
        while (tk.Type != MAPTokenType.BraceClose) {
            if (tk.Type == MAPTokenType.EOF) {
                scanner.Read ();
                Errors.Add (new QuakeMAPParseError ("Unexpected end-of-file. Expected a key-value pair, a brush or '}'.", tk));
                return null;
            }

            if (tk.Type == MAPTokenType.QuotedString) {
                var kvp = ParseKeyValuePair (ref scanner);

                if (!kvp.HasValue)
                    return null;

                keyValuePairs [kvp.Value.Key] = kvp.Value.Value;
            } else if (tk.Type == MAPTokenType.BraceOpen) {
                var brush = ParseBrush (ref scanner);

                if (!brush.HasValue)
                    return null;

                brushes.Add (brush.Value);
            } else {
                Errors.Add (new QuakeMAPParseError ("Expected a key-value pair, a brush or '}'.", tk));
                return null;
            }

            tk = scanner.Peek ();
        }
        scanner.Read ();

        return new QuakeEntity {
            KeyValuePairs = keyValuePairs,
            Brushes = brushes.ToArray (),
        };
    }

    private KeyValuePair<string, string>? ParseKeyValuePair (ref MAPScanner scanner) {
        var key = scanner.Read ();
        if (key.Type != MAPTokenType.QuotedString)
            return null;

        var value = scanner.Read ();
        if (value.Type != MAPTokenType.QuotedString)
            return null;

        return new (key.Text [1..^1].GetPooledString (), value.Text [1..^1].GetPooledString ());
    }

    private QuakeBrush? ParseBrush (ref MAPScanner scanner) {
        var tk = scanner.Read ();
        Debug.Assert (tk.Type == MAPTokenType.BraceOpen);

        using var planes = new StructPooledList<QuakePlane> (CL_ClearMode.Auto);

        tk = scanner.Peek ();
        while (tk.Type != MAPTokenType.BraceClose) {
            if (tk.Type == MAPTokenType.EOF) {
                scanner.Read ();
                Errors.Add (new QuakeMAPParseError ("Unexpected end-of-file. Expected a plane or '}'.", tk));
                break;
            }

            if (tk.Type == MAPTokenType.ParensOpen) {
                var plane = ParsePlane (ref scanner);

                if (!plane.HasValue)
                    return null;

                planes.Add (plane.Value);
            } else {
                Errors.Add (new QuakeMAPParseError ("Expected a plane or '}'.", tk));
                break;
            }

            tk = scanner.Peek ();
        }
        scanner.Read ();

        return new QuakeBrush {
            Planes = planes.ToArray (),
        };
    }

    private QuakePlane? ParsePlane (ref MAPScanner scanner) {
        Debug.Assert (scanner.Peek ().Type == MAPTokenType.ParensOpen);

        var point1 = ParsePoint (ref scanner);
        if (!point1.HasValue)
            return null;

        var point2 = ParsePoint (ref scanner);
        if (!point2.HasValue)
            return null;

        var point3 = ParsePoint (ref scanner);
        if (!point3.HasValue)
            return null;

        var textureName = scanner.Peek ();
        if (textureName.Type != MAPTokenType.Text) {
            Errors.Add (new QuakeMAPParseError ($"Expected a texture name, got '{textureName.Text.GetPooledString ()}'.", textureName));
            return null;
        } else
            scanner.Read ();

        bool isValve220;
        Vector3 textureAxis1;
        Vector3 textureAxis2;
        Vector2 textureOffsets;

        if (scanner.Peek ().Type == MAPTokenType.BracketOpen) {
            // Valve format
            var axis1 = ParseTextureAxis (ref scanner);
            if (!axis1.HasValue)
                return null;

            var axis2 = ParseTextureAxis (ref scanner);
            if (!axis2.HasValue)
                return null;

            isValve220 = true;
            textureAxis1 = new (axis1.Value.X, axis1.Value.Y, axis1.Value.Z);
            textureAxis2 = new (axis2.Value.X, axis2.Value.Y, axis2.Value.Z);
            textureOffsets = new (axis1.Value.W, axis2.Value.W);
        } else {
            // Quake format
            var offsetsX = ParseNumberAsFloat (ref scanner);
            if (!offsetsX.HasValue)
                return null;

            var offsetsY = ParseNumberAsFloat (ref scanner);
            if (!offsetsY.HasValue)
                return null;

            isValve220 = false;
            textureOffsets = new (offsetsX.Value, offsetsY.Value);
            textureAxis1 = Vector3.Zero;
            textureAxis2 = Vector3.Zero;
        }

        var textureRotation = ParseNumberAsFloat (ref scanner);
        if (!textureRotation.HasValue)
            return null;

        var texScaleX = ParseNumberAsFloat (ref scanner);
        if (!texScaleX.HasValue)
            return null;

        var texScaleY = ParseNumberAsFloat (ref scanner);
        if (!texScaleY.HasValue)
            return null;

        var textureScale = new Vector2 (texScaleX.Value, texScaleY.Value);

        return new QuakePlane {
            Point1 = point1.Value,
            Point2 = point2.Value,
            Point3 = point3.Value,

            Texture = textureName.Text.GetPooledString (),

            TextureIsValve220 = isValve220,
            TextureAxis1 = textureAxis1,
            TextureAxis2 = textureAxis2,

            TextureOffsets = textureOffsets,
            TextureRotation = textureRotation.Value,
            TextureScale = textureScale,
        };
    }

    private float? ParseNumberAsFloat (ref MAPScanner scanner) {
        var tk = scanner.Peek ();
        if (tk.Type != MAPTokenType.Integer && tk.Type != MAPTokenType.Float) {
            Errors.Add (new QuakeMAPParseError ($"Expected a float or int, got '{tk.Text.GetPooledString ()}'.", tk));
            return null;
        }
        scanner.Read ();

        if (tk.Type == MAPTokenType.Integer)
            return long.Parse (tk.Text, System.Globalization.NumberStyles.Integer);
        else
            return float.Parse (tk.Text, System.Globalization.NumberStyles.Float);
    }

    private Vector3? ParsePoint (ref MAPScanner scanner) {
        var tk = scanner.Peek ();
        if (tk.Type != MAPTokenType.ParensOpen) {
            Errors.Add (new QuakeMAPParseError ($"Expected '(', got '{tk.Text.GetPooledString ()}'.", tk));
            return null;
        }
        scanner.Read ();

        var pointX = ParseNumberAsFloat (ref scanner);
        if (!pointX.HasValue)
            return null;

        var pointY = ParseNumberAsFloat (ref scanner);
        if (!pointY.HasValue)
            return null;

        var pointZ = ParseNumberAsFloat (ref scanner);
        if (!pointZ.HasValue)
            return null;

        tk = scanner.Peek ();
        if (tk.Type != MAPTokenType.ParensClose) {
            Errors.Add (new QuakeMAPParseError ($"Expected ')', got '{tk.Text.GetPooledString ()}'.", tk));
            return null;
        }
        scanner.Read ();

        return new (pointX.Value, pointY.Value, pointZ.Value);
    }

    private Vector4? ParseTextureAxis (ref MAPScanner scanner) {
        var tk = scanner.Peek ();
        if (tk.Type != MAPTokenType.BracketOpen) {
            Errors.Add (new QuakeMAPParseError ($"Expected '[', got '{tk.Text.GetPooledString ()}'.", tk));
            return null;
        }
        scanner.Read ();

        var axisX = ParseNumberAsFloat (ref scanner);
        if (!axisX.HasValue)
            return null;

        var axisY = ParseNumberAsFloat (ref scanner);
        if (!axisY.HasValue)
            return null;

        var axisZ = ParseNumberAsFloat (ref scanner);
        if (!axisZ.HasValue)
            return null;

        var axisOffset = ParseNumberAsFloat (ref scanner);
        if (!axisOffset.HasValue)
            return null;

        tk = scanner.Peek ();
        if (tk.Type != MAPTokenType.BracketClose) {
            Errors.Add (new QuakeMAPParseError ($"Expected ']', got '{tk.Text.GetPooledString ()}'.", tk));
            return null;
        }
        scanner.Read ();

        return new (axisX.Value, axisY.Value, axisZ.Value, axisOffset.Value);
    }
}
