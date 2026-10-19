namespace Bit.Bmotion;

/// <summary>How a repeating animation behaves on each subsequent iteration.</summary>
public enum BmRepeatType
{
    /// <summary>Restart from the beginning each iteration (0 → 1, 0 → 1, …).</summary>
    Loop,
    /// <summary>Ping-pong: alternate direction each iteration (0 → 1, 1 → 0, …).</summary>
    Mirror,
    /// <summary>Jump back to the start and play in reverse each iteration (1 → 0, 1 → 0, …).</summary>
    Reverse,
}

/// <summary>
/// Repeat configuration for a transition. Implicitly converts from an <c>int</c> repeat count;
/// use the factories for richer configuration:
/// <code>
/// Repeat = 3                          // repeat 3 times (loop)
/// Repeat = BmRepeat.Forever           // repeat forever
/// Repeat = BmRepeat.Mirror()          // ping-pong forever
/// Repeat = BmRepeat.Reverse(2, delay: 0.5)
/// </code>
/// </summary>
public readonly struct BmRepeat : IEquatable<BmRepeat>
{
    private BmRepeat(int count, BmRepeatType type, double delay)
    {
        if (!double.IsFinite(delay) || delay < 0)
            throw new ArgumentException("Repeat delay must be a finite, non-negative number of seconds.", nameof(delay));
        Count = count < 0 ? -1 : count;
        Type = type;
        Delay = delay;
    }

    /// <summary>Number of additional iterations; <c>-1</c> means repeat forever.</summary>
    public int Count { get; }

    /// <summary>Behaviour of each subsequent iteration.</summary>
    public BmRepeatType Type { get; }

    /// <summary>Pause between iterations, in seconds.</summary>
    public double Delay { get; }

    /// <summary>True when this repeats forever.</summary>
    public bool IsForever => Count < 0;

    /// <summary>Repeat forever, restarting from the beginning each iteration.</summary>
    public static BmRepeat Forever => new(-1, BmRepeatType.Loop, 0);

    /// <summary>Restart from the beginning each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmRepeat Loop(int count = -1, double delay = 0) => new(count, BmRepeatType.Loop, delay);

    /// <summary>Ping-pong direction each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmRepeat Mirror(int count = -1, double delay = 0) => new(count, BmRepeatType.Mirror, delay);

    /// <summary>Play in reverse each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmRepeat Reverse(int count = -1, double delay = 0) => new(count, BmRepeatType.Reverse, delay);

    /// <summary>Repeat a fixed number of times.</summary>
    public static BmRepeat Times(int count, BmRepeatType type = BmRepeatType.Loop, double delay = 0)
        => new(count, type, delay);

    public static implicit operator BmRepeat(int count) => new(count, BmRepeatType.Loop, 0);

    public bool Equals(BmRepeat other) => Count == other.Count && Type == other.Type && Delay.Equals(other.Delay);
    public override bool Equals(object? obj) => obj is BmRepeat r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Count, Type, Delay);
    public static bool operator ==(BmRepeat left, BmRepeat right) => left.Equals(right);
    public static bool operator !=(BmRepeat left, BmRepeat right) => !left.Equals(right);
}
