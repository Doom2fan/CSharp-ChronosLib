/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using ChronosLib.FileLoading;
using ChronosLib.Pooled;
using CommunityToolkit.HighPerformance.Buffers;

namespace ChronosLib.BMFont {
    public sealed class BMFontParser : SimpleParserBase, IDisposable {
        #region ================== Static fields

        private static string whitespaceChars = " \t";

        #endregion

        #region ================== Instance fields

        private BMFontGenerationInfo genInfo;
        private BMFontCommonInfo commonInfo;

        private Dictionary<RehashedValue<int>, BMFontCharacterData> charData;
        private Dictionary<RehashedValue<long>, BMFontKerningData> kernPairs;

        private BMFontPageData [] pages;
        private PooledArray<bool> pagesParsed;

        private bool parsedGenInfo;
        private bool parsedCommonInfo;
        private bool parsedCharsTag;
        private bool parsedKerningsTag;

        #endregion

        #region ================== Constructors

        public BMFontParser () : base (whitespaceChars) {
        }

        #endregion

        #region ================== Instance methods

        #region Public methods

        public BMFontInfo Parse (ReadOnlySpan<char> text) {
            genInfo = new BMFontGenerationInfo ();
            commonInfo = new BMFontCommonInfo ();

            charData = new Dictionary<RehashedValue<int>, BMFontCharacterData> ();
            kernPairs = new Dictionary<RehashedValue<long>, BMFontKerningData> ();

            parsedGenInfo = false;
            parsedCommonInfo = false;
            parsedCharsTag = false;
            parsedKerningsTag = false;

            pages = null;

            pagesParsed.Dispose ();
            pagesParsed = PooledArray<bool>.Empty ();

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

            var ret = new BMFontInfo (charData, kernPairs, pages, genInfo, commonInfo);

            pagesParsed.Dispose ();
            charData = null;
            pages = null;
            kernPairs = null;

            genInfo = new BMFontGenerationInfo ();
            commonInfo = new BMFontCommonInfo ();

            return ret;
        }

        #endregion

        #region Private methods

        private static bool CheckTag (ReadOnlySpan<char> tag, ReadOnlySpan<char> testedTag) {
            return tag.Equals (testedTag, StringComparison.Ordinal);
        }

        private void Process (ReadOnlySpan<char> line, int lineNum) {
            currentLine = lineNum;

            // Remove comments
            int commIdx = line.IndexOf ('#');
            if (commIdx != -1)
                line = line.Slice (0, commIdx);
            line = line.Trim (parserWhitespaceChars);

            if (line.Length == 0)
                return;

            var tagName = ReadIgnoreWhitespace (ref line);

            if (CheckTag (tagName, "info"))
                ParseInfo (ref line);

            else if (CheckTag (tagName, "common"))
                ParseCommon (ref line);

            else if (CheckTag (tagName, "page"))
                ParsePage (ref line);

            else if (CheckTag (tagName, "chars"))
                ParseChars (ref line);

            else if (CheckTag (tagName, "char"))
                ParseChar (ref line);

            else if (CheckTag (tagName, "kernings"))
                ParseKernings (ref line);

            else if (CheckTag (tagName, "kerning"))
                ParseKerningPair (ref line);

            else {
                throw new BMFontParseException (
                    string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                    tagName.ToString (),
                    currentLine
                ));
            }
        }

        private void ParseInfo (ref ReadOnlySpan<char> line) {
            if (parsedGenInfo)
                throw new BMFontParseException ($"Duplicate \"info\" tag on line {currentLine}");

            using var multiIntArr = PooledArray<int>.GetArray (4);

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "info");

                if (CheckTag (tagName, "face"))
                    genInfo.Face = StringPool.Shared.GetOrAdd (ParseStringTag (ref line, "face (Font family)"));

                else if (CheckTag (tagName, "size"))
                    genInfo.Size = ParseIntTag (ref line, "size (TTF font size)");

                else if (CheckTag (tagName, "bold"))
                    genInfo.Bold = ParseIntTag (ref line, "bold") == 1;

                else if (CheckTag (tagName, "italic"))
                    genInfo.Italic = ParseIntTag (ref line, "italic") == 1;

                else if (CheckTag (tagName, "charset"))
                    genInfo.CharSet = StringPool.Shared.GetOrAdd (ParseStringTag (ref line, "charset"));

                else if (CheckTag (tagName, "unicode"))
                    genInfo.Unicode = ParseIntTag (ref line, "unicode") == 1;

                else if (CheckTag (tagName, "stretchH"))
                    genInfo.StretchHeight = ParseIntTag (ref line, "stretchH");

                else if (CheckTag (tagName, "smooth"))
                    genInfo.Smooth = ParseIntTag (ref line, "smooth") == 1;

                else if (CheckTag (tagName, "aa"))
                    genInfo.SupersamplingLevel = ParseIntTag (ref line, "aa");

                else if (CheckTag (tagName, "padding")) {
                    ParseMultiIntTag (ref line, "padding", multiIntArr.Span.Slice (0, 4));

                    genInfo.PaddingTop = multiIntArr.Array [0];
                    genInfo.PaddingRight = multiIntArr.Array [1];
                    genInfo.PaddingBottom = multiIntArr.Array [2];
                    genInfo.PaddingLeft = multiIntArr.Array [3];
                }

                else if (CheckTag (tagName, "spacing")) {
                    ParseMultiIntTag (ref line, "spacing", multiIntArr.Span.Slice (0, 2));

                    genInfo.SpacingHorizontal = multiIntArr.Array [0];
                    genInfo.SpacingVertical = multiIntArr.Array [1];
                }

                else if (CheckTag (tagName, "outline"))
                    genInfo.OutlineThickness = ParseIntTag (ref line, "outline");

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            parsedGenInfo = true;
        }

        private void ParseCommon (ref ReadOnlySpan<char> line) {
            if (parsedCommonInfo)
                throw new BMFontParseException ($"Duplicate \"common\" tag on line {currentLine}.");

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "common");

                if (CheckTag (tagName, "lineHeight"))
                    commonInfo.LineHeight = ParseIntTag (ref line, "lineHeight");

                else if (CheckTag (tagName, "base"))
                    commonInfo.Base = ParseIntTag (ref line, "base");

                else if (CheckTag (tagName, "scaleW"))
                    commonInfo.ScaleW = ParseIntTag (ref line, "scaleW");

                else if (CheckTag (tagName, "scaleH"))
                    commonInfo.ScaleH = ParseIntTag (ref line, "scaleH");

                else if (CheckTag (tagName, "pages")) {
                    if (!(pages is null))
                        throw new BMFontParseException ($"Duplicate \"pages\" tag on line {currentLine}.");

                    commonInfo.PagesCount = ParseIntTag (ref line, "pages");

                    pages = new BMFontPageData [commonInfo.PagesCount];
                    pagesParsed = PooledArray<bool>.GetArray (commonInfo.PagesCount);
                }

                else if (CheckTag (tagName, "packed"))
                    commonInfo.Packed = ParseIntTag (ref line, "packed") == 1;

                else if (CheckTag (tagName, "alphaChnl"))
                    commonInfo.ChannelAlpha = (BMFontChannelContents) ParseIntTag (ref line, "alphaChnl");

                else if (CheckTag (tagName, "redChnl"))
                    commonInfo.ChannelRed = (BMFontChannelContents) ParseIntTag (ref line, "redChnl");

                else if (CheckTag (tagName, "greenChnl"))
                    commonInfo.ChannelGreen = (BMFontChannelContents) ParseIntTag (ref line, "greenChnl");

                else if (CheckTag (tagName, "blueChnl"))
                    commonInfo.ChannelBlue = (BMFontChannelContents) ParseIntTag (ref line, "blueChnl");

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            parsedCommonInfo = true;
        }

        private void ParsePage (ref ReadOnlySpan<char> line) {
            if (pages is null)
                throw new BMFontParseException ($"Encountered a \"page\" tag on line {currentLine} before the pages count was parsed.");

            int id = -1;
            string file = null;

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "page");

                if (CheckTag (tagName, "id"))
                    id = ParseIntTag (ref line, "id");

                else if (CheckTag (tagName, "file"))
                    file = StringPool.Shared.GetOrAdd (ParseStringTag (ref line, "file"));

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            if (id < 0)
                throw new BMFontParseException ($"Missing page id on line {currentLine}.");
            else if (id >= pages.Length)
                throw new BMFontParseException ($"Page id ({id}) on line {currentLine} is higher than the specified page count ({pages.Length}).");
            else if (file is null)
                throw new BMFontParseException ($"Missing texture file name on {currentLine}.");

            if (pagesParsed.Array [id])
                throw new BMFontParseException ($"Duplicate page number {id} on line {currentLine}.");

            pages [id] = new BMFontPageData {
                PageId = id,
                FileName = file
            };
            pagesParsed.Array [id] = true;
        }

        private void ParseChars (ref ReadOnlySpan<char> line) {
            if (parsedCharsTag)
                throw new BMFontParseException ($"Duplicate \"chars\" tag on line {currentLine}.");

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "chars");

                if (CheckTag (tagName, "count"))
                    charData.EnsureCapacity (ParseIntTag (ref line, "count"));

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            parsedCharsTag = true;
        }

        private void ParseChar (ref ReadOnlySpan<char> line) {
            bool parsedId = false;

            var cData = new BMFontCharacterData ();

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "page");

                if (CheckTag (tagName, "id")) {
                    cData.CharId = ParseIntTag (ref line, "id");
                    parsedId = true;
                }

                else if (CheckTag (tagName, "x"))
                    cData.PosX = ParseIntTag (ref line, "x");
                else if (CheckTag (tagName, "y"))
                    cData.PosY = ParseIntTag (ref line, "y");

                else if (CheckTag (tagName, "width"))
                    cData.Width = ParseIntTag (ref line, "width");
                else if (CheckTag (tagName, "height"))
                    cData.Height = ParseIntTag (ref line, "height");

                else if (CheckTag (tagName, "xoffset"))
                    cData.OffsetX = ParseIntTag (ref line, "xoffset");
                else if (CheckTag (tagName, "yoffset"))
                    cData.OffsetY = ParseIntTag (ref line, "yoffset");

                else if (CheckTag (tagName, "xadvance"))
                    cData.AdvanceX = ParseIntTag (ref line, "xadvance");

                else if (CheckTag (tagName, "page"))
                    cData.Page = ParseIntTag (ref line, "page");

                else if (CheckTag (tagName, "chnl"))
                    cData.Channel = (BMFontCharacterChannel) ParseIntTag (ref line, "chnl");

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            if (!parsedId)
                throw new BMFontParseException ($"Missing char id on line {currentLine}.");
            if (!charData.TryAdd (new RehashedValue<int> (cData.CharId), cData))
                throw new BMFontParseException ($"Duplicate char ({cData.CharId}) on line {currentLine}.");
        }

        private void ParseKernings (ref ReadOnlySpan<char> line) {
            if (parsedKerningsTag)
                throw new BMFontParseException ($"Duplicate \"kernings\" tag on line {currentLine}.");

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "kernings");

                if (CheckTag (tagName, "count"))
                    kernPairs.EnsureCapacity (ParseIntTag (ref line, "count"));

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            parsedKerningsTag = true;
        }

        private void ParseKerningPair (ref ReadOnlySpan<char> line) {
            bool parsedFirst = false;
            bool parsedSecond = false;

            var kernPair = new BMFontKerningData ();

            while (line.Length > 0) {
                SkipWhitespace (ref line);

                var tagName = ParseIdentifier (ref line, "kerning");

                if (CheckTag (tagName, "first")) {
                    kernPair.FirstChar = ParseIntTag (ref line, "first");
                    parsedFirst = true;
                } else if (CheckTag (tagName, "second")) {
                    kernPair.SecondChar = ParseIntTag (ref line, "second");
                    parsedSecond = true;
                } else if (CheckTag (tagName, "amount"))
                    kernPair.AmountX = ParseIntTag (ref line, "amount");

                else {
                    throw new BMFontParseException (
                        string.Format ("Unsupported tag \"{0}\" on line \"{1}\".",
                        tagName.ToString (),
                        currentLine
                    ));
                }
            }

            var pairVal = new RehashedValue<long> (BMFontInfo.GetKerningPairLong (kernPair.FirstChar, kernPair.SecondChar));

            if (!parsedFirst)
                throw new BMFontParseException ($"Missing first char id on line {currentLine}.");
            else if (!parsedSecond)
                throw new BMFontParseException ($"Missing second char id on line {currentLine}.");
            else if (!kernPairs.TryAdd (pairVal, kernPair))
                throw new BMFontParseException ($"Duplicate kerning pair ({kernPair.FirstChar}, {kernPair.SecondChar}) on line {currentLine}.");
        }

        private int ParseIntTag (ref ReadOnlySpan<char> line, string location) {
            SkipWhitespace (ref line);
            if (line.Length < 1 || line [0] != '=')
                throw new BMFontParseException ($"Expected '=' when parsing {location} on line {currentLine}, not {(line.Length < 1 ? "EOL" : line [0].ToString ())}");

            line = line.Slice (1);

            return ParseInt (ref line, location);
        }

        private void ParseMultiIntTag (ref ReadOnlySpan<char> line, string location, Span<int> ret) {
            SkipWhitespace (ref line);
            if (line.Length < 1 || line [0] != '=')
                throw new BMFontParseException ($"Expected '=' when parsing {location} on line {currentLine}, not {(line.Length < 1 ? "EOL" : line [0].ToString ())}");

            line = line.Slice (1);

            ret [0] = ParseInt (ref line, location);

            for (int i = 1; i < ret.Length; i++) {
                if (line.Length < 1 || line [0] != ',')
                    throw new BMFontParseException ($"Expected ',' when parsing {location} on line {currentLine}, not {(line.Length < 1 ? "EOL" : line [0].ToString ())}");

                line = line.Slice (1);

                ret [i] = ParseInt (ref line, location);
            }
        }

        private ReadOnlySpan<char> ParseStringTag (ref ReadOnlySpan<char> line, string location) {
            SkipWhitespace (ref line);
            if (line.Length < 1 || line [0] != '=')
                throw new BMFontParseException ($"Expected '=' when parsing {location} on line {currentLine}, not {(line.Length < 1 ? "EOL" : line [0].ToString ())}");

            line = line.Slice (1);

            return ParseString (ref line, location);
        }

        protected override Exception CreateParseException (string location, Exception e) {
            string message = string.Format ("An error ocurred while parsing {0} on line {1}", location, currentLine);
            return new BMFontParseException (message, e);
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

        private void Dispose (bool disposing) {
            if (!IsDisposed) {
                if (disposing)
                    GC.SuppressFinalize (this);

                pagesParsed.Dispose ();

                IsDisposed = true;
            }
        }

        ~BMFontParser () {
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

    public class BMFontParseException : Exception {
        #region ================== Constructors

        public BMFontParseException (string message) : base (message) {
        }

        public BMFontParseException (string message, Exception innerException) : base (message, innerException) {
        }

        #endregion
    }

    public struct BMFontInfo {
        #region ================== Instance fields

        private readonly Dictionary<RehashedValue<int>, BMFontCharacterData> charDataList;
        internal readonly Dictionary<RehashedValue<long>, BMFontKerningData> kernPairsList;
        internal readonly BMFontPageData [] pagesList;

        #endregion

        #region ================== Instance properties

        public BMFontGenerationInfo GenerationInfo { get; private set; }
        public BMFontCommonInfo CommonInfo { get; private set; }

        public int PagesCount => pagesList.Length;

        #endregion

        #region ================== Constructors

        internal BMFontInfo (
            Dictionary<RehashedValue<int>, BMFontCharacterData> charData,
            Dictionary<RehashedValue<long>, BMFontKerningData> kernData,
            BMFontPageData [] pages, BMFontGenerationInfo genInfo, BMFontCommonInfo commonInfo
        ) {
            charDataList = charData;
            kernPairsList = kernData;
            pagesList = pages;

            GenerationInfo = genInfo;
            CommonInfo = commonInfo;
        }

        #endregion

        #region ================== Static functions

        public static long GetKerningPairLong (int first, int second) {
            return (((long) first) << 32) | ((long) second);
        }

        #endregion

        #region ================== Instance methods

        public bool TryGetChar (int c, out BMFontCharacterData charData) {
            return charDataList.TryGetValue (new RehashedValue<int> (c), out charData);
        }

        public bool TryGetKerningPair (int first, int second, out BMFontKerningData kernData) {
            return kernPairsList.TryGetValue (new RehashedValue<long> (GetKerningPairLong (first, second)), out kernData);
        }

        public BMFontPageData GetPage (int pageNum) {
            return pagesList [pageNum];
        }

        #endregion
    }
}
