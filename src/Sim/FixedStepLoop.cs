namespace Engine.Sim;

public sealed class FixedStepLoop
{
    public float FixedDeltaTime = 1f / 60f;
    public int MaxSteps = 5;

    private float _accumulator;

    public int LastStepCount { get; private set; }

    public float Alpha => _accumulator / FixedDeltaTime;

    public void Update(double deltaSeconds, ISimulation simulation)
    {
        _accumulator += (float)deltaSeconds;

        float maximum = MaxSteps * FixedDeltaTime;
        if (_accumulator > maximum)
        {
            _accumulator = maximum;
        }

        int steps = 0;
        while (_accumulator >= FixedDeltaTime)
        {
            simulation.Step(FixedDeltaTime);
            _accumulator -= FixedDeltaTime;
            steps++;
        }

        LastStepCount = steps;
    }
}
