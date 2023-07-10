/*
 * ChronosLib - A collection of useful things
 * Copyright (C) 2018-2021 Chronos "phantombeta" Ouroboros
 *
 * This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at https://mozilla.org/MPL/2.0/.
 */

using System;

namespace ChronosLib.BMFont;

public enum BMFontChannelContents {
    /// <summary>The channel contains glyph data.</summary>
    Glyphs = 0,
    /// <summary>The channel contains outline data.</summary>
    Outlines = 1,
    /// <summary>The channel contains glyph and outline data.</summary>
    GlyphsAndOutlines = 2,
    /// <summary>The channel is set to 0.</summary>
    Zero = 3,
    /// <summary>The channel is set to 1.</summary>
    One = 4,
}

[Flags]
public enum BMFontCharacterChannel {
    /// <summary>The character is found in the blue channel of the texture.</summary>
    Blue = 1,
    /// <summary>The character is found in the green channel of the texture.</summary>
    Green = 2,
    /// <summary>The character is found in the red channel of the texture.</summary>
    Red = 4,
    /// <summary>The character is found in the alpha channel of the texture.</summary>
    Alpha = 8,
}

/// <summary>Information on how the font was generated.</summary>
public struct BMFontGenerationInfo {
    /// <summary>The name of the TrueType font.</summary>
    public string Face { get; internal set; }

    /// <summary>The size of the TrueType font.</summary>
    public int Size { get; internal set; }

    /// <summary>Whether the font is bold or not.</summary>
    public bool Bold { get; internal set; }

    /// <summary>Whether the font is italic or not.</summary>
    public bool Italic { get; internal set; }

    /// <summary>The name of the OEM charset used. (When not Unicode)</summary>
    public string CharSet { get; internal set; }

    /// <summary>Whether the font is Unicode or not.</summary>
    public bool Unicode { get; internal set; }

    /// <summary>The font height stretch in percentage. 100% means no stretch.</summary>
    public int StretchHeight { get; internal set; }

    /// <summary>Whether smoothing was turned on.</summary>
    public bool Smooth { get; internal set; }

    /// <summary>The supersampling level used. 1 means not supersampling was used.</summary>
    public int SupersamplingLevel { get; internal set; }

    /// <summary>The top padding for each character.</summary>
    public int PaddingTop { get; internal set; }
    /// <summary>The bottom padding for each character.</summary>
    public int PaddingBottom { get; internal set; }
    /// <summary>The left padding for each character.</summary>
    public int PaddingLeft { get; internal set; }
    /// <summary>The right padding for each character.</summary>
    public int PaddingRight { get; internal set; }

    /// <summary>The horizontal spacing of each character.</summary>
    public int SpacingHorizontal { get; internal set; }
    /// <summary>The vertical spacing of each character.</summary>
    public int SpacingVertical { get; internal set; }

    /// <summary>The outline thickness for the characters.</summary>
    public int OutlineThickness { get; internal set; }
}

/// <summary>Information common to all the characters of the font.</summary>
public struct BMFontCommonInfo {
    /// <summary>The distance in pixels between each line of text.</summary>
    public int LineHeight { get; internal set; }

    /// <summary>The number of pixels from the absolute top of the line to the base of the characters.</summary>
    public int Base { get; internal set; }

    /// <summary>The width of the texture, normally used to scale the X pos of the character image.</summary>
    public int ScaleW { get; internal set; }

    /// <summary>The height of the texture, normally used to scale the Y pos of the character image.</summary>
    public int ScaleH { get; internal set; }

    /// <summary>The number of texture pages included in the font.</summary>
    public int PagesCount { get; internal set; }

    /// <summary>Whether the monochrome characters have been packed into each of the texture channels. If true,
    /// <see cref="ChannelAlpha"/> describes what is stored in each channel.</summary>
    public bool Packed { get; internal set; }

    /// <summary>What data the alpha channel contains.</summary>
    public BMFontChannelContents ChannelAlpha { get; internal set; }

    /// <summary>What data the red channel contains.</summary>
    public BMFontChannelContents ChannelRed { get; internal set; }

    /// <summary>What data the green channel contains.</summary>
    public BMFontChannelContents ChannelGreen { get; internal set; }

    /// <summary>What data the blue channel contains.</summary>
    public BMFontChannelContents ChannelBlue { get; internal set; }
}

/// <summary>The data for a font character.</summary>
public struct BMFontCharacterData {
    /// <summary>The ID of the character.</summary>
    public int CharId { get; internal set; }

    /// <summary>The left position of the character image in the texture.</summary>
    public int PosX { get; internal set; }
    /// <summary>The top position of the character image in the texture.</summary>
    public int PosY { get; internal set; }

    /// <summary>The width of the character image in the texture.</summary>
    public int Width { get; internal set; }
    /// <summary>The height of the character image in the texture.</summary>
    public int Height { get; internal set; }

    /// <summary>How much the current position should be offset horizontally when copying the image from the
    /// texture to the screen.</summary>
    public int OffsetX { get; internal set; }
    /// <summary>How much the current position should be offset vertically when copying the image from the texture
    /// to the screen.</summary>
    public int OffsetY { get; internal set; }

    /// <summary>How much the current position should be advanced after drawing the character.</summary>
    public int AdvanceX { get; internal set; }

    /// <summary>The texture page the character image is found in.</summary>
    public int Page { get; internal set; }

    /// <summary>The texture channel where the character image is found.</summary>
    public BMFontCharacterChannel Channel { get; internal set; }
}

/// <summary>The data for a font page.</summary>
public struct BMFontPageData {
    /// <summary>The page id.</summary>
    public int PageId { get; internal set; }

    /// <summary>The texture file name.</summary>
    public string FileName { get; internal set; }
}

/// <summary>Kerning pair information for the characters.</summary>
public struct BMFontKerningData {
    /// <summary>The id of the first character of the pair.</summary>
    public int FirstChar { get; internal set; }

    /// <summary>The id of the second character of the pair.</summary>
    public int SecondChar { get; internal set; }

    /// <summary>How much the X position should be adjusted when drawing the second character immediately following
    /// the first.</summary>
    public int AmountX { get; internal set; }
}
