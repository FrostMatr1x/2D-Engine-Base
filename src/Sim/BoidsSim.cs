using System;
using System.Numerics;
using Engine.Graphic;

namespace Engine.Sim;

public sealed class BoidsSim : ISimulation
{
    private Vector2[] _positions = Array.Empty<Vector2>();
    private Vector2[] _previous = Array.Empty<Vector2>();
    private Vector2[] _velocities = Array.Empty<Vector2>();
    private float[] _wander = Array.Empty<float>();

    public int Count { get; private set; }

    public Vector2 Bounds { get; set; } = new Vector2(4000f, 3000f);

    public void Init(int count, int seed)
    {
        Count = count;
        _positions = new Vector2[count];
        _previous = new Vector2[count];
        _velocities = new Vector2[count];
        _wander = new float[count];

        var random = new Random(seed);
        for (int i = 0; i < count; i++)
        {
            Vector2 position = new Vector2(
                (float)random.NextDouble() * Bounds.X,
                (float)random.NextDouble() * Bounds.Y);
            _positions[i] = position;
            _previous[i] = position;

            float angle = (float)(random.NextDouble() * Math.PI * 2.0);
            float speed = 20f + (float)random.NextDouble() * 60f;
            _velocities[i] = new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * speed;
            _wander[i] = (float)random.NextDouble() * MathF.PI * 2f;
        }
    }

    public void Step(float deltaTime)
    {
        Vector2 centroid = Vector2.Zero;
        Vector2 averageVelocity = Vector2.Zero;
        for (int i = 0; i < Count; i++)
        {
            centroid += _positions[i];
            averageVelocity += _velocities[i];
        }
        centroid /= Count;
        averageVelocity /= Count;

        for (int i = 0; i < Count; i++)
        {
            Vector2 previous = _positions[i];

            _wander[i] += deltaTime * 1.5f;
            var wander = new Vector2(
                MathF.Cos(_wander[i] * 1.3f + i * 0.001f),
                MathF.Sin(_wander[i] * 0.9f + i * 0.002f));

            Vector2 toCenter = centroid - _positions[i];
            Vector2 desired = toCenter * 0.35f + averageVelocity * 0.6f + wander * 30f;

            Vector2 velocity = _velocities[i] + (desired - _velocities[i]) * (1.5f * deltaTime);
            float speed = velocity.Length();
            if (speed > 200f)
            {
                velocity *= 200f / speed;
            }
            _velocities[i] = velocity;

            Vector2 unwrapped = previous + velocity * deltaTime;
            Vector2 position = unwrapped;
            if (position.X < 0f)
            {
                position.X += Bounds.X;
            }
            else if (position.X >= Bounds.X)
            {
                position.X -= Bounds.X;
            }
            if (position.Y < 0f)
            {
                position.Y += Bounds.Y;
            }
            else if (position.Y >= Bounds.Y)
            {
                position.Y -= Bounds.Y;
            }

            previous += position - unwrapped;

            _previous[i] = previous;
            _positions[i] = position;
        }
    }

    public void GetRenderState(Span<Instance2D> destination, float alpha)
    {
        for (int i = 0; i < Count; i++)
        {
            Vector2 position = Vector2.Lerp(_previous[i], _positions[i], alpha);
            Vector2 velocity = _velocities[i];
            float rotation = MathF.Atan2(velocity.Y, velocity.X);
            byte intensity = (byte)Math.Clamp((int)(velocity.Length() * 1.2f), 40, 255);
            destination[i] = new Instance2D(
                position,
                new Vector2(4f, 2f),
                rotation,
                Instance2D.PackColor(intensity, (byte)(255 - intensity), 200, 255));
        }
    }
}
