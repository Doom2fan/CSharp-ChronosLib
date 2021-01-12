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
using System.Linq;
using System.Numerics;

namespace ChronosLib.ModelLoading.WavefrontObj {
    public class MtlParser : ParserBase {
        #region ================== Static fields

        protected static readonly string whitespaceChars = " \t";

        #endregion

        #region ================== Instance fields

        protected readonly List<MaterialDefinition> materials;
        protected MaterialDefinition curMaterial;

        #endregion

        #region ================== Constructors

        public MtlParser () : base (whitespaceChars) {
            materials = new List<MaterialDefinition> ();

            Reset ();
        }

        #endregion

        #region ================== Instance methods

        #region Public methods

        public void Reset () {
            materials.Clear ();
            curMaterial = null;
        }

        public void Parse (string [] lines) {
            int lineCount = 1;
            foreach (string line in lines)
                Process (line, lineCount++);

            FinalizeCurrentMaterial ();
        }

        public void Parse (ReadOnlySpan<char> text) {
            int lineEnd;
            int lineCount = 1;

            while ((lineEnd = text.IndexOf ('\n')) != -1 || (lineEnd = text.Length) != 0) {
                ReadOnlySpan<char> line;

                if (lineEnd != 0 && text [lineEnd - 1] == '\r')
                    line = text.Slice (0, lineEnd - 1);
                else
                    line = text.Slice (0, lineEnd);

                Process (line, lineCount);

                text = text.Slice (lineEnd + 1);
                lineCount++;
            }

            FinalizeCurrentMaterial ();
        }

        public MtlLibrary FinalizeLibrary () {
            FinalizeCurrentMaterial ();

            var lib = new MtlLibrary (materials);

            Reset ();

            return lib;
        }

        #endregion

        #region Protected methods

        protected void Process (ReadOnlySpan<char> line, int lineNum) {
            currentLine = lineNum;

            // Remove comments
            int commIdx = line.IndexOf ('#');
            if (commIdx != -1)
                line = line.Slice (0, commIdx);
            line = line.Trim (whitespaceChars);

            if (line.Length == 0)
                return;

            var specifier = ReadUntil (ref line, whitespaceChars);

            if (specifier.Equals ("newmtl", StringComparison.OrdinalIgnoreCase)) {
                FinalizeCurrentMaterial ();
                curMaterial = new MaterialDefinition (ParseText (ref line, "newmtl").ToString ());
            }

            else if (specifier.Equals ("Ka", StringComparison.OrdinalIgnoreCase))
                curMaterial.AmbientReflectivity = ParseVector3 (ref line, "Ka");

            else if (specifier.Equals ("Kd", StringComparison.OrdinalIgnoreCase))
                curMaterial.DiffuseReflectivity = ParseVector3 (ref line, "Kd");

            else if (specifier.Equals ("Ks", StringComparison.OrdinalIgnoreCase))
                curMaterial.SpecularReflectivity = ParseVector3 (ref line, "Ks");

            else if (specifier.Equals ("Tf", StringComparison.OrdinalIgnoreCase))
                curMaterial.TransmissionFilter = ParseVector3 (ref line, "Tf");

            else if (specifier.Equals ("illum", StringComparison.OrdinalIgnoreCase))
                curMaterial.IlluminationModel = ParseInt (ref line, "illum");

            else if (specifier.Equals ("d", StringComparison.OrdinalIgnoreCase))
                curMaterial.Opacity = ParseFloat (ref line, "d");

            else if (specifier.Equals ("Tr", StringComparison.OrdinalIgnoreCase))
                curMaterial.Opacity = 1f - ParseFloat (ref line, "Tr");

            else if (specifier.Equals ("Ns", StringComparison.OrdinalIgnoreCase))
                curMaterial.SpecularExponent = ParseFloat (ref line, "Ns");

            else if (specifier.Equals ("sharpness", StringComparison.OrdinalIgnoreCase))
                curMaterial.Sharpness = ParseFloat (ref line, "sharpness");

            else if (specifier.Equals ("Ni", StringComparison.OrdinalIgnoreCase))
                curMaterial.OpticalDensity = ParseFloat (ref line, "Ni"); // IoR

            else if (specifier.Equals ("map_Ka", StringComparison.OrdinalIgnoreCase))
                curMaterial.AmbientTexture = ParseText (ref line, "map_Ka").ToString ();

            else if (specifier.Equals ("map_Kd", StringComparison.OrdinalIgnoreCase))
                curMaterial.DiffuseTexture = ParseText (ref line, "map_Kd").ToString ();

            else if (specifier.Equals ("map_Ks", StringComparison.OrdinalIgnoreCase))
                curMaterial.SpecularColorTexture = ParseText (ref line, "map_Ks").ToString ();

            else if (specifier.Equals ("map_Ns", StringComparison.OrdinalIgnoreCase))
                curMaterial.SpecularHighlightTexture = ParseText (ref line, "map_Ns").ToString ();

            else if (specifier.Equals ("map_bump", StringComparison.OrdinalIgnoreCase) || specifier.Equals ("bump", StringComparison.OrdinalIgnoreCase))
                curMaterial.BumpMap = ParseText (ref line, "bump").ToString ();

            else if (specifier.Equals ("map_d", StringComparison.OrdinalIgnoreCase))
                curMaterial.AlphaMap = ParseText (ref line, "map_d").ToString ();

            else if (specifier.Equals ("Pr", StringComparison.OrdinalIgnoreCase))
                curMaterial.Roughness = ParseFloat (ref line, "Pr");

            else if (specifier.Equals ("Pm", StringComparison.OrdinalIgnoreCase))
                curMaterial.Metallicness = ParseFloat (ref line, "Pm");

            else if (specifier.Equals ("Ke", StringComparison.OrdinalIgnoreCase))
                curMaterial.EmissiveCoefficient = ParseVector3 (ref line, "Ke");

            else if (specifier.Equals ("map_Pr", StringComparison.OrdinalIgnoreCase))
                curMaterial.RoughnessTexture = ParseText (ref line, "map_Pr").ToString ();

            else if (specifier.Equals ("map_Pm", StringComparison.OrdinalIgnoreCase))
                curMaterial.MetallicnessTexture = ParseText (ref line, "map_Pm").ToString ();

            else if (specifier.Equals ("map_Ke", StringComparison.OrdinalIgnoreCase))
                curMaterial.EmissiveTexture = ParseText (ref line, "map_Ke").ToString ();

            else if (specifier.Equals ("norm", StringComparison.OrdinalIgnoreCase))
                curMaterial.NormalMap = ParseText (ref line, "norm").ToString ();

            else {
                throw new MtlParseException (
                    string.Format ("Unsupported specifier \"{0}\" on line \"{1}\".",
                    specifier.ToString (),
                    currentLine
                ));
            }
        }

        protected void FinalizeCurrentMaterial () {
            if (curMaterial != null) {
                materials.Add (curMaterial);
                curMaterial = null;
            }
        }

        protected override Exception CreateParseException (string location, Exception e) {
            string message = string.Format ("An error ocurred while parsing {0} on line {1}", location, currentLine);
            return new MtlParseException (message, e);
        }

        #endregion

        #endregion
    }

    public class MtlParseException : Exception {
        #region ================== Constructors

        public MtlParseException (string message) : base (message) {
        }

        public MtlParseException (string message, Exception innerException) : base (message, innerException) {
        }

        #endregion
    }

    public class MtlLibrary {
        #region ================== Instance properties

        public IReadOnlyDictionary<string, MaterialDefinition> Definitions { get; }

        #endregion

        #region ================== Constructors

        public MtlLibrary (IEnumerable<MaterialDefinition> definitions) {
            Definitions = definitions.ToDictionary (def => def.Name);
        }

        #endregion
    }

    public class MaterialDefinition {
        #region ================== Instance properties

        public string Name { get; }

        public Vector3 AmbientReflectivity { get; internal set; }
        public Vector3 DiffuseReflectivity { get; internal set; }
        public Vector3 SpecularReflectivity { get; internal set; }
        public Vector3 TransmissionFilter { get; internal set; }
        public int IlluminationModel { get; internal set; }
        public float Opacity { get; internal set; }
        public float SpecularExponent { get; internal set; }
        public float Sharpness { get; internal set; }
        public float OpticalDensity { get; internal set; }

        public string AmbientTexture { get; internal set; }
        public string DiffuseTexture { get; internal set; }
        public string SpecularColorTexture { get; internal set; }
        public string SpecularHighlightTexture { get; internal set; }
        public string AlphaMap { get; internal set; }
        public string BumpMap { get; internal set; }

        public float Roughness { get; internal set; }
        public float Metallicness { get; internal set; }
        public Vector3 EmissiveCoefficient { get; internal set; }

        public string RoughnessTexture { get; internal set; }
        public string MetallicnessTexture { get; internal set; }
        public string EmissiveTexture { get; internal set; }
        public string NormalMap { get; internal set; }

        #endregion

        #region ================== Constructors

        internal MaterialDefinition (string name) {
            Name = name;
        }

        #endregion
    }
}