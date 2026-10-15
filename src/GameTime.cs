using System;

namespace Engine;

public sealed class GameTime
{
    public TimeSpan TotalGameTime { get; private set; }

    public TimeSpan ElapsedGameTime { get; private set; }

    public bool IsRunningSlowly { get; private set; }

    public GameTime()
    {
    }

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime)
        : this(totalGameTime, elapsedGameTime, false)
    {
    }

    public GameTime(TimeSpan totalGameTime, TimeSpan elapsedGameTime, bool isRunningSlowly)
    {
        TotalGameTime = totalGameTime;
        ElapsedGameTime = elapsedGameTime;
        IsRunningSlowly = isRunningSlowly;
    }

    public static GameTime FromTime(Time time, bool isRunningSlowly = false)
    {
        if (time == null)
        {
            throw new ArgumentNullException(nameof(time));
        }

        return new GameTime(
            TimeSpan.FromSeconds(time.TotalTime),
            TimeSpan.FromSeconds(time.DeltaTime),
            isRunningSlowly);
    }

    public void Update(Time time, bool isRunningSlowly = false)
    {
        if (time == null)
        {
            throw new ArgumentNullException(nameof(time));
        }

        TotalGameTime = TimeSpan.FromSeconds(time.TotalTime);
        ElapsedGameTime = TimeSpan.FromSeconds(time.DeltaTime);
        IsRunningSlowly = isRunningSlowly;
    }
}
