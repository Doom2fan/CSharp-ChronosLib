/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2020 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

/*
 * The MIT License (MIT)
 *
 * Copyright (c) 2017 Eric Mellino and Veldrid contributors
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using ChronosLib.FileLoading;
using ChronosLib.Pooled;
using Microsoft.Toolkit.HighPerformance.Buffers;

namespace ChronosLib.ModelLoading.WavefrontObj {
    public class ObjParser : SimpleParserBase, IDisposable {
        #region ================== Static fields

        protected static readonly string whitespaceChars = " \t";
        protected static readonly char slashChar = '/';

        #endregion

        #region ================== Instance fields

        protected StructPooledList<Vector3> vertexPositions;
        protected StructPooledList<Vector3> vertexNormals;
        protected StructPooledList<Vector2> vertexTexCoords;

        protected StructPooledList<ObjFile.MeshGroup> meshGroups;

        protected string currentGroupName;
        protected string currentMaterial;
        protected int currentSmoothingGroup;
        protected StructPooledList<ObjFile.Face> currentGroupFaces;

        protected string materialLibName;

        #endregion

        #region ================== Constructors

        public ObjParser () : base (whitespaceChars) {
            vertexPositions = new StructPooledList<Vector3> (CL_ClearMode.Auto);
            vertexNormals = new StructPooledList<Vector3> (CL_ClearMode.Auto);
            vertexTexCoords = new StructPooledList<Vector2> (CL_ClearMode.Auto);

            meshGroups = new StructPooledList<ObjFile.MeshGroup> (CL_ClearMode.Auto);

            currentGroupFaces = new StructPooledList<ObjFile.Face> (CL_ClearMode.Auto);

            Reset ();
        }

        #endregion

        #region ================== Instance methods

        #region Public methods

        public ObjFile Parse (string [] lines) {
            int lineNum = 1;
            foreach (string line in lines)
                Process (line, lineNum++);

            return FinalizeFile ();
        }

        public ObjFile Parse (ReadOnlySpan<char> text) {
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

            return FinalizeFile ();
        }

        #endregion

        #region Protected methods

        protected void Reset () {
            // Clear the arrays
            vertexPositions.Clear ();
            vertexNormals.Clear ();
            vertexTexCoords.Clear ();
            meshGroups.Clear ();
            currentGroupFaces.Clear ();

            // Free the pooled lists/memory
            vertexPositions.Capacity = 0;
            vertexNormals.Capacity = 0;
            vertexTexCoords.Capacity = 0;
            meshGroups.Capacity = 0;
            currentGroupFaces.Capacity = 0;

            // Clear the mesh group data
            currentGroupName = null;
            currentMaterial = null;
            currentSmoothingGroup = -1;

            // Clear the global data
            materialLibName = null;
        }

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

            if (CheckSpecifier (specifier, "v"))
                vertexPositions.Add (ParseObjVector3 (ref line, "position data (v)"));
            else if (CheckSpecifier (specifier, "vn"))
                vertexNormals.Add (ParseObjVector3 (ref line, "normal data (vn)"));
            else if (CheckSpecifier (specifier, "vt")) {
                var texCoord = ParseObjVector2 (ref line, "texture coordinate data (vt)");
                // Flip v coordinate
                texCoord.Y = 1f - texCoord.Y;
                vertexTexCoords.Add (texCoord);
            } else if (CheckSpecifier (specifier, "g")) {
                FinalizeMeshGroup ();
                currentGroupName = StringPool.Shared.GetOrAdd (ParseText (ref line, "mesh group (g)"));
            } else if (CheckSpecifier (specifier, "o"))
                StringPool.Shared.GetOrAdd (ParseText (ref line, "object name (o)"));
            else if (CheckSpecifier (specifier, "usemtl")) {
                if (currentMaterial != null) {
                    string nextGroupName = currentGroupName + "_Next";
                    FinalizeMeshGroup ();
                    currentGroupName = nextGroupName;
                }

                currentMaterial = StringPool.Shared.GetOrAdd (ParseText (ref line, "material (usemtl)"));
            } else if (CheckSpecifier (specifier, "s")) {
                var text = ParseText (ref line, "smoothing group (s)");
                if (text.Equals ("off", StringComparison.OrdinalIgnoreCase))
                    currentSmoothingGroup = 0;
                else
                    currentSmoothingGroup = ParseInt (ref text, "smoothing group (s)");
            } else if (CheckSpecifier (specifier, "f"))
                ProcessFaceLine (ref line);
            else if (CheckSpecifier (specifier, "mtllib"))
                SetMaterialLib (ref line);
            else {
                throw new ObjParseException (
                    string.Format ("An unsupported line-type specifier, '{0}', was used on line {1}",
                    StringPool.Shared.GetOrAdd (specifier),
                    currentLine));
            }
        }

        protected void SetMaterialLib (ref ReadOnlySpan<char> line) {
            var text = ParseText (ref line, "material library (mtllib)");
            if (materialLibName != null) {
                throw new ObjParseException (
                    string.Format ("\"mtllib\" appeared again in the file. It should only appear once. Line {0}",
                    currentLine
                ));
            }

            materialLibName = StringPool.Shared.GetOrAdd (text);
        }

        protected void ProcessFaceLine (ref ReadOnlySpan<char> line) {
            using var verts = new StructPooledList<ObjFile.FaceVertex> (CL_ClearMode.Auto);

            while (line.Length > 0) {
                var text = ReadIgnoreWhitespace (ref line);
                var vertex = ParseFaceVertex (ref text);

                verts.Add (vertex);
            }

            var otherVertsSpan = verts.Span;

            var firstVert = otherVertsSpan [^1];
            var lastVert = otherVertsSpan [^2];
            otherVertsSpan = otherVertsSpan [0..^2];

            while (otherVertsSpan.Length > 0) {
                var thisVert = otherVertsSpan [^1];

                currentGroupFaces.Add (new ObjFile.Face (firstVert, lastVert, thisVert, currentSmoothingGroup));

                lastVert = thisVert;
                otherVertsSpan = otherVertsSpan [0..^1];
            }
        }

        protected ObjFile.FaceVertex ParseFaceVertex (ref ReadOnlySpan<char> faceComponents) {
            var text = ReadUntil (ref faceComponents, slashChar);

            int pos = ParseInt (ref text, "face position index");

            int texCoord = -1;
            if (faceComponents.Length > 0) {
                text = ReadUntil (ref faceComponents, slashChar);
                if (text.Length > 0)
                    texCoord = ParseInt (ref text, "face texture coordinate index");
            }

            int normal = -1;
            if (faceComponents.Length > 0) {
                text = ReadUntil (ref faceComponents, slashChar);
                if (text.Length > 0)
                    normal = ParseInt (ref text, "face normal index");
            }

            faceComponents = faceComponents.Trim (whitespaceChars);
            if (faceComponents.Length > 0)
                throw CreateExceptionForWrongFaceCount ();

            return new ObjFile.FaceVertex () { PositionIndex = pos, NormalIndex = normal, TexCoordIndex = texCoord };
        }

        protected Exception CreateExceptionForWrongFaceCount () {
            return new ObjParseException (
                string.Format ("Expected 1, 2, or 3 face components on line {0}",
                currentLine
            ));
        }

        protected void FinalizeMeshGroup () {
            if (currentGroupName != null) {
                var faces = currentGroupFaces.ToPooledArray ();
                meshGroups.Add (new ObjFile.MeshGroup (currentGroupName, currentMaterial, faces));

                currentGroupName = null;
                currentMaterial = null;
                currentSmoothingGroup = -1;
                currentGroupFaces.Clear ();
            }
        }

        protected ObjFile FinalizeFile () {
            currentGroupName ??= "GlobalFileGroup";
            meshGroups.Add (new ObjFile.MeshGroup (currentGroupName, currentMaterial, currentGroupFaces.ToPooledArray ()));

            var ret = new ObjFile (
                vertexPositions.ToPooledArray (), vertexNormals.ToPooledArray (), vertexTexCoords.ToPooledArray (),
                meshGroups.ToPooledArray (), materialLibName
            );

            Reset ();

            return ret;
        }

        protected override Exception CreateParseException (string location, Exception e) {
            string message = string.Format ("An error ocurred while parsing {0} on line {1}", location, currentLine);
            return new ObjParseException (message, e);
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

                vertexPositions.Dispose ();
                vertexNormals.Dispose ();
                vertexTexCoords.Dispose ();

                meshGroups.Dispose ();

                currentGroupFaces.Dispose ();

                IsDisposed = true;
            }
        }

        ~ObjParser () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
            }
        }

        public void Dispose () {
            Dispose (true);
        }

        #endregion
    }

    public class ObjParseException : Exception {
        #region ================== Constructors

        public ObjParseException (string message) : base (message) {
        }

        public ObjParseException (string message, Exception innerException) : base (message, innerException) {
        }

        #endregion
    }

    public class ObjFile : IDisposable {
        #region ================== Structs

        /// <summary>
        /// An OBJ file construct describing an individual mesh group.
        /// </summary>
        public struct MeshGroup : IDisposable {
            #region ================== Instance fields

            /// <summary>
            /// The name.
            /// </summary>
            public readonly string Name;

            /// <summary>
            /// The name of the associated <see cref="MaterialDefinition"/>.
            /// </summary>
            public readonly string Material;

            /// <summary>
            /// The set of <see cref="Face"/>s comprising this mesh group.
            /// </summary>
            public readonly PooledArray<Face> Faces;

            #endregion

            #region ================== Constructors

            /// <summary>
            /// Constructs a new <see cref="MeshGroup"/>.
            /// </summary>
            /// <param name="name">The name.</param>
            /// <param name="material">The name of the associated <see cref="MaterialDefinition"/>.</param>
            /// <param name="faces">The faces.</param>
            public MeshGroup (string name, string material, PooledArray<Face> faces) {
                IsDisposed = false;

                Name = name;
                Material = material;
                Faces = faces;
            }

            #endregion

            #region ================== IDisposable Support

            public bool IsDisposed {
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                get;
                [MethodImpl (MethodImplOptions.AggressiveInlining)]
                private set;
            }

            public void Dispose () {
                if (!IsDisposed) {
                    Faces.Dispose ();

                    IsDisposed = true;
                }
            }

            #endregion
        }

        /// <summary>
        /// An OBJ file construct describing the indices of vertex components.
        /// </summary>
        public struct FaceVertex {
            #region ================== Instance fields

            /// <summary>
            /// The index of the position component.
            /// </summary>
            public int PositionIndex;

            /// <summary>
            /// The index of the normal component.
            /// </summary>
            public int NormalIndex;

            /// <summary>
            /// The index of the texture coordinate component.
            /// </summary>
            public int TexCoordIndex;

            #endregion

            #region ================== Instance methods

            public override string ToString () {
                return string.Format ("Pos:{0}, Normal:{1}, TexCoord:{2}", PositionIndex, NormalIndex, TexCoordIndex);
            }

            #endregion
        }

        /// <summary>
        /// An OBJ file construct describing the data of a vertex.
        /// </summary>
        public struct FaceVertexData {
            #region ================== Instance fields

            /// <summary>
            /// The position component.
            /// </summary>
            public Vector3 Position;

            /// <summary>
            /// The normal component.
            /// </summary>
            public Vector3 Normal;

            /// <summary>
            /// The texture coordinate component.
            /// </summary>
            public Vector2 TexCoord;

            #endregion

            #region ================== Instance methods

            public override string ToString () {
                return string.Format ("Pos: {0}, Normal: {1}, TexCoord: {2}", Position, Normal, TexCoord);
            }

            #endregion
        }

        /// <summary>
        /// An OBJ file construct describing an individual mesh face.
        /// </summary>
        public struct Face {
            #region ================== Instance fields

            /// <summary>
            /// The first vertex.
            /// </summary>
            public readonly FaceVertex Vertex0;

            /// <summary>
            /// The second vertex.
            /// </summary>
            public readonly FaceVertex Vertex1;

            /// <summary>
            /// The third vertex.
            /// </summary>
            public readonly FaceVertex Vertex2;

            /// <summary>
            /// The smoothing group. Describes which kind of vertex smoothing should be applied.
            /// </summary>
            public readonly int SmoothingGroup;

            #endregion

            #region ================== Constructors

            public Face (FaceVertex v0, FaceVertex v1, FaceVertex v2, int smoothingGroup = -1) {
                Vertex0 = v0;
                Vertex1 = v1;
                Vertex2 = v2;

                SmoothingGroup = smoothingGroup;
            }

            #endregion
        }

        #endregion

        #region ================== Instance properties

        public PooledArray<Vector3> Positions { get; }
        public PooledArray<Vector3> Normals { get; }
        public PooledArray<Vector2> TexCoords { get; }
        public PooledArray<MeshGroup> MeshGroups { get; }
        public string MaterialLibName { get; protected set; }

        #endregion

        #region ================== Constructors

        public ObjFile (
            PooledArray<Vector3> positions, PooledArray<Vector3> normals, PooledArray<Vector2> texCoords,
            PooledArray<MeshGroup> meshGroups, string materialLibName
        ) {
            Positions = positions;
            Normals = normals;
            TexCoords = texCoords;
            MeshGroups = meshGroups;
            MaterialLibName = materialLibName;
        }

        #endregion

        #region ================== Instance methods

        /// <summary>
        /// Gets the vertex data for the given OBJ <see cref="MeshGroup"/>.
        /// </summary>
        /// <param name="group">The OBJ <see cref="MeshGroup"/> to construct the vertices from.</param>
        /// <param name="vertices">The vertex data of the <see cref="MeshGroup"/>.</param>
        public PooledArray<FaceVertexData> GetVertices (MeshGroup group) {
            var facesSpan = group.Faces.Span;

            using var verticesList = new StructPooledList<FaceVertexData> (CL_ClearMode.Auto);
            verticesList.EnsureCapacity (facesSpan.Length * 3);

            for (int i = 0; i < facesSpan.Length; i++) {
                var face = facesSpan [i];

                var triSpan = verticesList.AddSpan (3);
                triSpan [0] = ConstructVertex (face.Vertex0, face.Vertex1, face.Vertex2);
                triSpan [1] = ConstructVertex (face.Vertex1, face.Vertex2, face.Vertex0);
                triSpan [2] = ConstructVertex (face.Vertex2, face.Vertex0, face.Vertex1);
            }

            return verticesList.ToPooledArray ();
        }

        private FaceVertexData ConstructVertex (FaceVertex key, FaceVertex adjacent1, FaceVertex adjacent2) {
            Vector3 position = Positions.Span [key.PositionIndex - 1];
            Vector3 normal;
            if (key.NormalIndex == -1)
                normal = ComputeNormal (key, adjacent1, adjacent2);
            else
                normal = Normals.Span [key.NormalIndex - 1];

            Vector2 texCoord = key.TexCoordIndex == -1 ? Vector2.Zero : TexCoords.Span [key.TexCoordIndex - 1];

            return new FaceVertexData {
                Position = position,
                Normal = normal,
                TexCoord = texCoord
            };
        }

        private Vector3 ComputeNormal (FaceVertex v1, FaceVertex v2, FaceVertex v3) {
            var posSpan = Positions.Span;
            Vector3 pos1 = posSpan [v1.PositionIndex - 1];
            Vector3 pos2 = posSpan [v2.PositionIndex - 1];
            Vector3 pos3 = posSpan [v3.PositionIndex - 1];

            return Vector3.Normalize (Vector3.Cross (pos1 - pos2, pos1 - pos3));
        }

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

                foreach (var group in MeshGroups.Span)
                    group.Dispose ();

                Positions.Dispose ();
                Normals.Dispose ();
                TexCoords.Dispose ();
                MeshGroups.Dispose ();

                MaterialLibName = null;

                IsDisposed = true;
            }
        }

        ~ObjFile () {
            if (!IsDisposed) {
                Debug.Fail ($"An instance of {GetType ().FullName} has not been disposed.");
                Dispose (false);
            }
        }

        public void Dispose () {
            Dispose (true);
        }

        #endregion
    }
}