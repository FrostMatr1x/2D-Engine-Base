using System.Diagnostics;
using Silk.NET.GLFW;

namespace Engine;

public class Time
{
    public float DeltaTime { get; private set; }
    public double TotalTime { get; private set; }
    
    private double _lastTime;
    private Stopwatch _stopwatch;

    public Time()
    {
        _stopwatch = Stopwatch.StartNew();
        _lastTime = 0;
    }

    public void Update()
    {
        double currentTime = _stopwatch.Elapsed.TotalSeconds;
        DeltaTime = (float)(currentTime - _lastTime);
        TotalTime = currentTime;
        _lastTime = currentTime;
    }
}