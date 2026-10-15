using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using StbTrueTypeSharp;

namespace Engine.Graphic;

public static unsafe class FontRasterizer
{
    private const int DefaultAtlasWidth = 1024;
    private const int MaxAtlasHeight = 4096;
    private const int MaxAtlasWidth = 8192;
    private const int Padding = 1;

    public static SpriteFont Rasterize(
        GraphicsDevice device,
        byte[] ttf,
        float pixelHeight,
        IReadOnlyList<int> codepoints,
        float spacing = 0f)
    {
        if (device == null)
        {
            throw new ArgumentNullException(nameof(device));
        }
        if (ttf == null || ttf.Length == 0)
        {
            throw new ArgumentException("TrueType font data must not be empty.", nameof(ttf));
        }
        if (codepoints == null)
        {
            throw new ArgumentNullException(nameof(codepoints));
        }
        if (pixelHeight <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(pixelHeight), "Font size must be positive.");
        }

        StbTrueType.stbtt_fontinfo info = StbTrueType.CreateFont(ttf, 0);
        bool transferred = false;
        try
        {
            float scale = StbTrueType.stbtt_ScaleForPixelHeight(info, pixelHeight);

            int ascent;
            int descent;
            int lineGap;
            StbTrueType.stbtt_GetFontVMetrics(info, &ascent, &descent, &lineGap);
            float lineHeight = (ascent - descent + lineGap) * scale;

            int atlasWidth = Math.Max(DefaultAtlasWidth, NextPowerOfTwo(MeasureMaxGlyphWidth(info, scale, codepoints) + Padding));
            List<Placement> placements;
            Dictionary<char, Glyph> glyphs;
            int requiredHeight;

            while (true)
            {
                placements = new List<Placement>();
                glyphs = new Dictionary<char, Glyph>();
                requiredHeight = Pack(info, scale, spacing, codepoints, atlasWidth, placements, glyphs);

                if (requiredHeight <= MaxAtlasHeight)
                {
                    break;
                }

                atlasWidth *= 2;
                if (atlasWidth > MaxAtlasWidth)
                {
                    throw new InvalidOperationException(
                        $"Font atlas for {pixelHeight}px does not fit within {MaxAtlasWidth}x{MaxAtlasHeight}.");
                }
            }

            int atlasHeight = NextPowerOfTwo(Math.Max(requiredHeight, 1));

            using var image = new Image<Rgba32>(atlasWidth, atlasHeight);
            RasterizeGlyphs(info, scale, placements, atlasWidth, atlasHeight, image);

            Texture2D atlas = device.Textures.CreateTexture(image);
            var handle = new FontHandle(info, scale);
            transferred = true;

            return new SpriteFont(
                atlas,
                glyphs,
                lineHeight,
                pixelHeight,
                spacing,
                handle.GetKerning,
                handle);
        }
        finally
        {
            if (!transferred)
            {
                info.Dispose();
            }
        }
    }

    private static int MeasureMaxGlyphWidth(
        StbTrueType.stbtt_fontinfo info,
        float scale,
        IReadOnlyList<int> codepoints)
    {
        int maxWidth = 0;
        foreach (int codepoint in codepoints)
        {
            if (codepoint <= 0 || codepoint > char.MaxValue)
            {
                continue;
            }

            int x0;
            int y0;
            int x1;
            int y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(info, codepoint, scale, scale, &x0, &y0, &x1, &y1);
            int width = x1 - x0;
            if (width > maxWidth)
            {
                maxWidth = width;
            }
        }
        return maxWidth;
    }

    private static int Pack(
        StbTrueType.stbtt_fontinfo info,
        float scale,
        float spacing,
        IReadOnlyList<int> codepoints,
        int atlasWidth,
        List<Placement> placements,
        Dictionary<char, Glyph> glyphs)
    {
        int penX = 0;
        int penY = 0;
        int rowHeight = 0;

        foreach (int codepoint in codepoints)
        {
            if (codepoint <= 0 || codepoint > char.MaxValue)
            {
                continue;
            }

            char character = (char)codepoint;
            if (glyphs.ContainsKey(character))
            {
                continue;
            }

            int advanceWidth;
            int leftSideBearing;
            StbTrueType.stbtt_GetCodepointHMetrics(info, codepoint, &advanceWidth, &leftSideBearing);
            float xAdvance = advanceWidth * scale + spacing;

            int x0;
            int y0;
            int x1;
            int y1;
            StbTrueType.stbtt_GetCodepointBitmapBox(info, codepoint, scale, scale, &x0, &y0, &x1, &y1);

            int width = x1 - x0;
            int height = y1 - y0;
            if (width <= 0 || height <= 0)
            {
                glyphs[character] = new Glyph(Rectangle.Empty, 0, 0, xAdvance);
                continue;
            }

            if (penX != 0 && penX + width + Padding > atlasWidth)
            {
                penX = 0;
                penY += rowHeight + Padding;
                rowHeight = 0;
            }

            var placement = new Placement(character, codepoint, penX, penY, width, height, x0, y0, xAdvance);
            placements.Add(placement);
            glyphs[character] = new Glyph(new Rectangle(penX, penY, width, height), x0, y0, xAdvance);

            penX += width + Padding;
            if (height > rowHeight)
            {
                rowHeight = height;
            }
        }

        return penY + rowHeight;
    }

    private static void RasterizeGlyphs(
        StbTrueType.stbtt_fontinfo info,
        float scale,
        List<Placement> placements,
        int atlasWidth,
        int atlasHeight,
        Image<Rgba32> image)
    {
        var white = new Rgba32(255, 255, 255, 255);
        foreach (Placement placement in placements)
        {
            int glyphWidth;
            int glyphHeight;
            int offsetX;
            int offsetY;
            byte* bitmap = StbTrueType.stbtt_GetCodepointBitmap(
                info, scale, scale, placement.Codepoint, &glyphWidth, &glyphHeight, &offsetX, &offsetY);

            if (bitmap == null)
            {
                continue;
            }

            try
            {
                int copyWidth = Math.Min(placement.Width, glyphWidth);
                int copyHeight = Math.Min(placement.Height, glyphHeight);

                for (int row = 0; row < copyHeight; row++)
                {
                    byte* sourceRow = bitmap + row * glyphWidth;
                    for (int column = 0; column < copyWidth; column++)
                    {
                        byte coverage = sourceRow[column];
                        if (coverage == 0)
                        {
                            continue;
                        }

                        int px = placement.X + column;
                        int py = placement.Y + row;
                        if ((uint)px >= (uint)atlasWidth || (uint)py >= (uint)atlasHeight)
                        {
                            continue;
                        }

                        image[px, py] = new Rgba32(white.R, white.G, white.B, coverage);
                    }
                }
            }
            finally
            {
                StbTrueType.stbtt_FreeBitmap(bitmap, null);
            }
        }
    }

    private static int NextPowerOfTwo(int value)
    {
        int result = 1;
        while (result < value)
        {
            result <<= 1;
        }
        return result;
    }

    private readonly struct Placement
    {
        public Placement(char character, int codepoint, int x, int y, int width, int height, int offsetX, int offsetY, float xAdvance)
        {
            Character = character;
            Codepoint = codepoint;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            OffsetX = offsetX;
            OffsetY = offsetY;
            XAdvance = xAdvance;
        }

        public char Character { get; }
        public int Codepoint { get; }
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public int OffsetX { get; }
        public int OffsetY { get; }
        public float XAdvance { get; }
    }

    private sealed class FontHandle : IDisposable
    {
        private readonly StbTrueType.stbtt_fontinfo _info;
        private readonly float _scale;

        public FontHandle(StbTrueType.stbtt_fontinfo info, float scale)
        {
            _info = info;
            _scale = scale;
        }

        public float GetKerning(char left, char right)
        {
            if (_info == null)
            {
                return 0f;
            }

            return StbTrueType.stbtt_GetCodepointKernAdvance(_info, left, right) * _scale;
        }

        public void Dispose()
        {
            _info?.Dispose();
        }
    }
}
