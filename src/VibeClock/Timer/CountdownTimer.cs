namespace VibeClock.Timer;

public enum TimerState { Idle, Running, Paused }

/// <summary>Deadline based countdown. The UI's refresh frequency cannot change elapsed time.</summary>
public sealed class CountdownTimer
{
    private readonly TimeProvider clock;
    private DateTimeOffset endTime;
    private TimeSpan pausedRemaining;

    public CountdownTimer(TimeProvider? clock = null) => this.clock = clock ?? TimeProvider.System;
    public TimerState State { get; private set; }
    public event EventHandler? Finished;
    public TimeSpan Remaining => State switch
    {
        TimerState.Running => Clamp(endTime - clock.GetUtcNow()),
        TimerState.Paused => pausedRemaining,
        _ => TimeSpan.Zero
    };

    public void Start(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero || duration > TimeSpan.FromHours(1))
            throw new ArgumentOutOfRangeException(nameof(duration));
        endTime = clock.GetUtcNow() + duration;
        pausedRemaining = TimeSpan.Zero;
        State = TimerState.Running;
    }

    public void Pause()
    {
        Update();
        if (State != TimerState.Running) return;
        pausedRemaining = Remaining;
        State = TimerState.Paused;
    }

    public void Resume()
    {
        if (State != TimerState.Paused) return;
        endTime = clock.GetUtcNow() + pausedRemaining;
        State = TimerState.Running;
    }

    public void Reset()
    {
        State = TimerState.Idle;
        pausedRemaining = TimeSpan.Zero;
    }

    public void Update()
    {
        if (State != TimerState.Running || Remaining > TimeSpan.Zero) return;
        Reset(); // Transition before raising the event guarantees a single ding.
        Finished?.Invoke(this, EventArgs.Empty);
    }

    private static TimeSpan Clamp(TimeSpan value) =>
        TimeSpan.FromTicks(Math.Clamp(value.Ticks, 0, TimeSpan.TicksPerHour));
}
