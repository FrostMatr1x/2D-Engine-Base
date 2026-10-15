using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Engine.Graphic;

public enum SpriteSortMode
{
    Deferred,
    BackToFront,
    FrontToBack,
    Immediate,
    Texture
}

public struct SpriteDrawCall
{
    public Texture2D Texture;
    public SamplerState Sampler;
    public Vector2 Position;
    public Vector2 Origin;
    public Vector2 Scale;
    public float Rotation;
    public Rectangle? Source;
    public Color Color;
    public SpriteEffects Effects;
    public float LayerDepth;
}

public struct SpriteSubBatch
{
    public Texture2D Texture;
    public SamplerState Sampler;
    public uint FirstVertex;
    public uint VertexCount;
}

public static class SpriteBatcher
{
    public static SpriteVertex[] Build(SpriteSortMode sortMode, List<SpriteDrawCall> draws, out List<SpriteSubBatch> subBatches)
    {
        subBatches = new List<SpriteSubBatch>();

        IEnumerable<SpriteDrawCall> ordered = sortMode == SpriteSortMode.BackToFront
            ? draws.OrderByDescending(draw => draw.LayerDepth)
            : sortMode == SpriteSortMode.FrontToBack
                ? draws.OrderBy(draw => draw.LayerDepth)
                : draws;

        var vertices = new List<SpriteVertex>(draws.Count * 6);
        Texture2D currentTexture = null;
        SamplerState currentSampler = null;
        uint batchStart = 0;

        foreach (SpriteDrawCall draw in ordered)
        {
            if (draw.Texture == null || !draw.Texture.IsValid)
            {
                continue;
            }

            if (currentTexture != draw.Texture || currentSampler != draw.Sampler)
            {
                if (currentTexture != null)
                {
                    subBatches.Add(new SpriteSubBatch
                    {
                        Texture = currentTexture,
                        Sampler = currentSampler,
                        FirstVertex = batchStart,
                        VertexCount = (uint)vertices.Count - batchStart
                    });
                }

                currentTexture = draw.Texture;
                currentSampler = draw.Sampler;
                batchStart = (uint)vertices.Count;
            }

            AppendQuad(vertices, draw);
        }

        if (currentTexture != null)
        {
            subBatches.Add(new SpriteSubBatch
            {
                Texture = currentTexture,
                Sampler = currentSampler,
                FirstVertex = batchStart,
                VertexCount = (uint)vertices.Count - batchStart
            });
        }

        return vertices.ToArray();
    }

    private static void AppendQuad(List<SpriteVertex> vertices, SpriteDrawCall draw)
    {
        Texture2D texture = draw.Texture;
        int texWidth = texture.Width;
        int texHeight = texture.Height;

        Rectangle source = draw.Source ?? new Rectangle(0, 0, texWidth, texHeight);
        float sourceX = source.X;
        float sourceY = source.Y;
        float sourceWidth = source.Width;
        float sourceHeight = source.Height;

        float u0 = sourceX / texWidth;
        float v0 = sourceY / texHeight;
        float u1 = (sourceX + sourceWidth) / texWidth;
        float v1 = (sourceY + sourceHeight) / texHeight;

        bool flipH = draw.Effects.HasFlag(SpriteEffects.FlipHorizontally);
        bool flipV = draw.Effects.HasFlag(SpriteEffects.FlipVertically);

        uint packedColor = draw.Color.PackedValue;
        float cos = MathF.Cos(draw.Rotation);
        float sin = MathF.Sin(draw.Rotation);

        Span<float> localX = stackalloc float[4] { 0f, sourceWidth, sourceWidth, 0f };
        Span<float> localY = stackalloc float[4] { 0f, 0f, sourceHeight, sourceHeight };
        Span<float> uvs = stackalloc float[4] { u0, u1, u1, u0 };
        Span<float> uvV = stackalloc float[4] { v0, v0, v1, v1 };

        var quad = new SpriteVertex[4];
        for (int i = 0; i < 4; i++)
        {
            float offsetX = (localX[i] - draw.Origin.X) * draw.Scale.X;
            float offsetY = (localY[i] - draw.Origin.Y) * draw.Scale.Y;

            float rotatedX = offsetX * cos - offsetY * sin;
            float rotatedY = offsetX * sin + offsetY * cos;

            var position = new Vector2(draw.Position.X + rotatedX, draw.Position.Y + rotatedY);

            float u = flipH ? u0 + u1 - uvs[i] : uvs[i];
            float v = flipV ? v0 + v1 - uvV[i] : uvV[i];

            quad[i] = new SpriteVertex(position, new Vector2(u, v), packedColor);
        }

        vertices.Add(quad[0]);
        vertices.Add(quad[1]);
        vertices.Add(quad[2]);
        vertices.Add(quad[0]);
        vertices.Add(quad[2]);
        vertices.Add(quad[3]);
    }
}
