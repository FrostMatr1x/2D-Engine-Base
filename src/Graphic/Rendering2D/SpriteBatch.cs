using System;
using System.Collections.Generic;
using System.Numerics;
using Engine.Graphic.Vulkan;
using Silk.NET.Vulkan;

namespace Engine.Graphic;

public sealed unsafe class SpriteBatch : IDisposable
{
    private const int InitialVertexCapacity = 1024;

    private readonly VulkanContext _context;
    private readonly VulkanBuffer[] _vertexBuffers;
    private readonly List<SpriteDrawCall> _draws = new List<SpriteDrawCall>();
    private readonly List<SpriteVertex> _vertices = new List<SpriteVertex>();
    private readonly List<SpriteSubBatch> _subBatches = new List<SpriteSubBatch>();

    private SpriteVertex[] _vertexArray = Array.Empty<SpriteVertex>();
    private SpriteSortMode _sortMode;
    private SamplerState _sampler;
    private Matrix _transformMatrix = Matrix.Identity;
    private bool _begun;
    private bool _pendingReset = true;
    private bool _disposed;

    public SpriteBatch(VulkanContext context, int framesInFlight)
    {
        _context = context;
        _vertexBuffers = new VulkanBuffer[framesInFlight];
        for (int i = 0; i < framesInFlight; i++)
        {
            _vertexBuffers[i] = VulkanBuffer.CreateHostVisible(
                context,
                (ulong)(InitialVertexCapacity * System.Runtime.CompilerServices.Unsafe.SizeOf<SpriteVertex>()),
                BufferUsageFlags.VertexBufferBit);
        }
    }

    public SpriteBatch(GraphicsDevice device) : this(device.Context, device.FramesInFlight)
    {
        device.ActiveSpriteBatch = this;
    }

    public bool HasPending => _subBatches.Count > 0;

    public IReadOnlyList<SpriteSubBatch> SubBatches => _subBatches;

    public VulkanBuffer ActiveBuffer { get; private set; }

    public void Begin(SpriteSortMode sortMode = SpriteSortMode.Deferred, SamplerState samplerState = null, Matrix? transformMatrix = null)
    {
        if (_pendingReset)
        {
            _vertices.Clear();
            _subBatches.Clear();
            _pendingReset = false;
        }

        _sortMode = sortMode;
        _sampler = samplerState ?? SamplerState.PointClamp;
        _transformMatrix = transformMatrix ?? Matrix.Identity;
        _draws.Clear();
        _begun = true;
    }

    public void End()
    {
        if (!_begun)
        {
            return;
        }
        _begun = false;

        SpriteBatcher.Build(_sortMode, _draws, _transformMatrix, _vertices, _subBatches);
        _draws.Clear();
    }

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        _draws.Add(new SpriteDrawCall
        {
            Texture = texture,
            Sampler = _sampler,
            Position = position,
            Origin = origin,
            Scale = scale,
            Rotation = rotation,
            Source = sourceRectangle,
            Color = color,
            Effects = effects,
            LayerDepth = layerDepth
        });
    }

    public void Draw(
        Texture2D texture,
        Vector2 position,
        Rectangle? sourceRectangle,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth)
    {
        Draw(texture, position, sourceRectangle, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth);
    }

    public void Draw(Texture2D texture, Vector2 position, Rectangle? sourceRectangle, Color color)
    {
        Draw(texture, position, sourceRectangle, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }

    public void Draw(Texture2D texture, Vector2 position, Color color)
    {
        Draw(texture, position, null, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Rectangle? sourceRectangle, Color color)
    {
        Rectangle source = sourceRectangle ?? new Rectangle(0, 0, texture.Width, texture.Height);
        float scaleX = source.Width != 0 ? (float)destinationRectangle.Width / source.Width : 1f;
        float scaleY = source.Height != 0 ? (float)destinationRectangle.Height / source.Height : 1f;

        Draw(
            texture,
            new Vector2(destinationRectangle.X, destinationRectangle.Y),
            sourceRectangle,
            color,
            0f,
            Vector2.Zero,
            new Vector2(scaleX, scaleY),
            SpriteEffects.None,
            0f);
    }

    public void Draw(Texture2D texture, Rectangle destinationRectangle, Color color)
    {
        Draw(texture, destinationRectangle, null, color);
    }

    public void DrawString(SpriteFont spriteFont, string text, Vector2 position, Color color)
    {
        DrawString(spriteFont, text, position, color, 0f, Vector2.Zero, Vector2.One, SpriteEffects.None, 0f);
    }

    public void DrawString(
        SpriteFont spriteFont,
        string text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        float scale,
        SpriteEffects effects,
        float layerDepth)
    {
        DrawString(spriteFont, text, position, color, rotation, origin, new Vector2(scale, scale), effects, layerDepth);
    }

    public void DrawString(
        SpriteFont spriteFont,
        string text,
        Vector2 position,
        Color color,
        float rotation,
        Vector2 origin,
        Vector2 scale,
        SpriteEffects effects,
        float layerDepth)
    {
        if (spriteFont == null)
        {
            throw new ArgumentNullException(nameof(spriteFont));
        }
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);
        Vector2 pen = Vector2.Zero;
        char? previous = null;

        foreach (char character in text)
        {
            if (character == '\n')
            {
                pen.X = 0f;
                pen.Y += spriteFont.LineHeight;
                previous = null;
                continue;
            }

            if (character == '\r')
            {
                continue;
            }

            if (previous.HasValue)
            {
                pen.X += spriteFont.GetKerning(previous.Value, character);
            }

            if (spriteFont.TryGetGlyph(character, out Glyph glyph) ||
                (spriteFont.DefaultCharacter.HasValue && spriteFont.TryGetGlyph(spriteFont.DefaultCharacter.Value, out glyph)))
            {
                if (!glyph.IsEmpty)
                {
                    Vector2 glyphOffset = pen + new Vector2(glyph.XOffset, glyph.YOffset);
                    Vector2 scaled = new Vector2(
                        (glyphOffset.X - origin.X) * scale.X,
                        (glyphOffset.Y - origin.Y) * scale.Y);
                    Vector2 rotated = new Vector2(
                        scaled.X * cos - scaled.Y * sin,
                        scaled.X * sin + scaled.Y * cos);

                    Draw(
                        spriteFont.Atlas,
                        position + rotated,
                        glyph.Bounds,
                        color,
                        rotation,
                        Vector2.Zero,
                        scale,
                        effects,
                        layerDepth);
                }

                pen.X += glyph.XAdvance;
            }

            previous = character;
        }
    }

    public void Prepare(int frameIndex)
    {
        ActiveBuffer = null;
        int count = _vertices.Count;
        if (_subBatches.Count == 0 || count == 0)
        {
            _pendingReset = true;
            return;
        }

        EnsureVertexArray(count);
        _vertices.CopyTo(_vertexArray, 0);
        EnsureCapacity(frameIndex, count);
        _vertexBuffers[frameIndex].SetData(_vertexArray.AsSpan(0, count));
        ActiveBuffer = _vertexBuffers[frameIndex];
        _pendingReset = true;
    }

    private void EnsureVertexArray(int count)
    {
        if (_vertexArray.Length < count)
        {
            _vertexArray = new SpriteVertex[count];
        }
    }

    private void EnsureCapacity(int frameIndex, int requiredVertices)
    {
        ulong requiredSize = (ulong)((long)requiredVertices * System.Runtime.CompilerServices.Unsafe.SizeOf<SpriteVertex>());
        if (_vertexBuffers[frameIndex].Size >= requiredSize)
        {
            return;
        }

        ulong currentCapacity = _vertexBuffers[frameIndex].Size;

        _context.Vk.DeviceWaitIdle(_context.Device);

        _vertexBuffers[frameIndex].Dispose();
        ulong minimum = (ulong)InitialVertexCapacity * (ulong)System.Runtime.CompilerServices.Unsafe.SizeOf<SpriteVertex>();
        ulong capacity = Math.Max(requiredSize, Math.Max(currentCapacity * 2, minimum));

        _vertexBuffers[frameIndex] = VulkanBuffer.CreateHostVisible(_context, capacity, BufferUsageFlags.VertexBufferBit);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;

        foreach (VulkanBuffer buffer in _vertexBuffers)
        {
            buffer.Dispose();
        }
    }
}
