using System;
using Engine.Graphic;

namespace Engine.Sim;

public interface ISimulation
{
    int Count { get; }

    void Init(int count, int seed);

    void Step(float deltaTime);

    void GetRenderState(Span<Instance2D> destination, float alpha);
}
