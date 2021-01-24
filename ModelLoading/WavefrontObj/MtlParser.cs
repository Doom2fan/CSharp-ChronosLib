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
using System.Runtime.CompilerServices;
using ChronosLib.Pooled;

namespace ChronosLib.ModelLoading.WavefrontObj {
    public class MtlParser : ParserBase, IDisposable {
        #region ================== Static fields

        protected static readonly string whitespaceChars = " \t";

        #endregion

        #region ================== Instance fields

        protected StructPooledList<MaterialDefinition> materials;
        protected MaterialDefinition curMaterial;

        #endregion

        #region ================== Constructors

        public MtlParser () : base (whitespaceChars) {
            materials = new StructPooledList<MaterialDefinition> (CL_ClearMode.Auto);

            Reset ();
        }

        #endregion

        #region ================== Instance methods

        #region Public methods

        public void Reset () {
            materials.Clear ();
            materials.Capacity = 0;
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
            static bool CheckSpecifier (ReadOnlySpan<char> specifier, ReadOnlySpan<char> testedSpec) {
                return specifier.Equals (testedSpec, StringComparison.OrdinalIgnoreCase);
            }

            currentLine = lineNum;

            // Remove comments
            int commIdx = line.IndexOf ('#');
            if (commIdx != -1)
                line = line.Slice (0, commIdx);
            line = line.Trim (whitespaceChars);

            if (line.Length == 0)
                return;

            var specifier = ReadIgnoreWhitespace (ref line);

            if (CheckSpecifier (specifier, "newmtl")) {
                FinalizeCurrentMaterial ();
                curMaterial = new MaterialDefinition (ParseText (ref line, "newmtl").ToString ());
            } else if (CheckSpecifier (specifier, "Ka"))
                curMaterial.AmbientReflectivity = ParseVector3 (ref line, "Ka");

            else if (CheckSpecifier (specifier, "Kd"))
                curMaterial.DiffuseReflectivity = ParseVector3 (ref line, "Kd");

            else if (CheckSpecifier (specifier, "Ks"))
                curMaterial.SpecularReflectivity = ParseVector3 (ref line, "Ks");

            else if (CheckSpecifier (specifier, "Tf"))
                curMaterial.TransmissionFilter = ParseVector3 (ref line, "Tf");

            else if (CheckSpecifier (specifier, "illum"))
                curMaterial.IlluminationModel = ParseInt (ref line, "illum");

            else if (CheckSpecifier (specifier, "d"))
                curMaterial.Opacity = ParseFloat (ref line, "d");

            else if (CheckSpecifier (specifier, "Tr"))
                curMaterial.Opacity = 1f - ParseFloat (ref line, "Tr");

            else if (CheckSpecifier (specifier, "Ns"))
                curMaterial.SpecularExponent = ParseFloat (ref line, "Ns");

            else if (CheckSpecifier (specifier, "sharpness"))
                curMaterial.Sharpness = ParseFloat (ref line, "sharpness");

            else if (CheckSpecifier (specifier, "Ni"))
                curMaterial.OpticalDensity = ParseFloat (ref line, "Ni"); // IoR

            else if (CheckSpecifier (specifier, "map_Ka"))
                curMaterial.AmbientTexture = ParseText (ref line, "map_Ka").ToString ();

            else if (CheckSpecifier (specifier, "map_Kd"))
                curMaterial.DiffuseTexture = ParseText (ref line, "map_Kd").ToString ();

            else if (CheckSpecifier (specifier, "map_Ks"))
                curMaterial.SpecularColorTexture = ParseText (ref line, "map_Ks").ToString ();

            else if (CheckSpecifier (specifier, "map_Ns"))
                curMaterial.SpecularHighlightTexture = ParseText (ref line, "map_Ns").ToString ();

            else if (CheckSpecifier (specifier, "map_bump") || CheckSpecifier (specifier, "bump"))
                curMaterial.BumpMap = ParseText (ref line, "bump").ToString ();

            else if (CheckSpecifier (specifier, "map_d"))
                curMaterial.AlphaMap = ParseText (ref line, "map_d").ToString ();

            else if (CheckSpecifier (specifier, "Pr"))
                curMaterial.Roughness = ParseFloat (ref line, "Pr");

            else if (CheckSpecifier (specifier, "Pm"))
                curMaterial.Metallicness = ParseFloat (ref line, "Pm");

            else if (CheckSpecifier (specifier, "Ke"))
                curMaterial.EmissiveCoefficient = ParseVector3 (ref line, "Ke");

            else if (CheckSpecifier (specifier, "map_Pr"))
                curMaterial.RoughnessTexture = ParseText (ref line, "map_Pr").ToString ();

            else if (CheckSpecifier (specifier, "map_Pm"))
                curMaterial.MetallicnessTexture = ParseText (ref line, "map_Pm").ToString ();

            else if (CheckSpecifier (specifier, "map_Ke"))
                curMaterial.EmissiveTexture = ParseText (ref line, "map_Ke").ToString ();

            else if (CheckSpecifier (specifier, "norm"))
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

        #region ================== IDisposable Support

        public bool IsDisposed {
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl (MethodImplOptions.AggressiveInlining)]
            private set;
        }

        protected virtual void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing)
                    GC.SuppressFinalize (this);

                materials.Dispose ();

                IsDisposed = true;
            }
        }

        ~MtlParser () {
#if DEBUG
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
            }
#endif
        }

        public void Dispose () {
            Dispose (true);
        }

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