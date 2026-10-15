using System;
using System.Collections.Generic;
using System.Numerics;

namespace Engine.Graphic;

public sealed class SpriteFont : IDisposable
{
    private readonly Dictionary<char, Glyph> _glyphs;
    private readonly Dictionary<long, float> _kerningCache = new Dictionary<long, float>();
    private readonly Func<char, char, float> _kerningProvider;
    private readonly IDisposable _kerningOwner;

    private bool _disposed;

    internal SpriteFont(
        Texture2D atlas,
        Dictionary<char, Glyph> glyphs,
        float lineHeight,
        float fontSize,
        float spacing,
        Func<char, char, float> kerningProvider,
        IDisposable kerningOwner)
    {
        Atlas = atlas;
        _glyphs = glyphs;
        LineHeight = lineHeight;
        FontSize = fontSize;
        Spacing = spacing;
        _kerningProvider = kerningProvider;
        _kerningOwner = kerningOwner;
    }

    public Texture2D Atlas { get; }

    public float LineHeight { get; }

    public float FontSize { get; }

    public float Spacing { get; }

    public char? DefaultCharacter { get; set; }

    public IReadOnlyDictionary<char, Glyph> Glyphs => _glyphs;

    public bool TryGetGlyph(char character, out Glyph glyph)
    {
        return _glyphs.TryGetValue(character, out glyph);
    }

    public float GetKerning(char left, char right)
    {
        if (_kerningProvider == null)
        {
            return 0f;
        }

        long key = ((long)left << 16) | right;
        if (_kerningCache.TryGetValue(key, out float cached))
        {
            return cached;
        }

        float value = _kerningProvider(left, right);
        _kerningCache[key] = value;
        return value;
    }

    public Vector2 MeasureString(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Vector2.Zero;
        }

        float width = 0f;
        float lineWidth = 0f;
        float height = LineHeight;
        char? previous = null;

        foreach (char character in text)
        {
            if (character == '\n')
            {
                width = MathF.Max(width, lineWidth);
                lineWidth = 0f;
                height += LineHeight;
                previous = null;
                continue;
            }

            if (character == '\r')
            {
                continue;
            }

            if (previous.HasValue)
            {
                lineWidth += GetKerning(previous.Value, character);
            }

            if (TryGetGlyph(character, out Glyph glyph) ||
                (DefaultCharacter.HasValue && TryGetGlyph(DefaultCharacter.Value, out glyph)))
            {
                lineWidth += glyph.XAdvance;
            }

            previous = character;
        }

        width = MathF.Max(width, lineWidth);
        return new Vector2(width, height);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        Atlas?.Dispose();
        _kerningOwner?.Dispose();
    }
}
