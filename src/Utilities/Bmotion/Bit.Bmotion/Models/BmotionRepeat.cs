namespace Bit.Bmotion;

/// <summary>
/// Repeat configuration for a transition. Implicitly converts from an <c>int</c> repeat count;
/// use the factories for richer configuration:
/// <code>
/// Repeat = 3                          // repeat 3 times (loop)
/// Repeat = BmotionRepeat.Forever           // repeat forever
/// Repeat = BmotionRepeat.Mirror()          // ping-pong forever
/// Repeat = BmotionRepeat.Reverse(2, delay: 0.5)
/// </code>
/// </summary>
public readonly struct BmotionRepeat : IEquatable<BmotionRepeat>
{
    private BmotionRepeat(int count, BmotionRepeatType type, double delay)
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
    public BmotionRepeatType Type { get; }

    /// <summary>Pause between iterations, in seconds.</summary>
    public double Delay { get; }

    /// <summary>True when this repeats forever.</summary>
    public bool IsForever => Count < 0;

    /// <summary>Repeat forever, restarting from the beginning each iteration.</summary>
    public static BmotionRepeat Forever => new(-1, BmotionRepeatType.Loop, 0);

    /// <summary>Restart from the beginning each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmotionRepeat Loop(int count = -1, double delay = 0) => new(count, BmotionRepeatType.Loop, delay);

    /// <summary>Ping-pong direction each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmotionRepeat Mirror(int count = -1, double delay = 0) => new(count, BmotionRepeatType.Mirror, delay);

    /// <summary>Play in reverse each iteration. Omit <paramref name="count"/> to repeat forever.</summary>
    public static BmotionRepeat Reverse(int count = -1, double delay = 0) => new(count, BmotionRepeatType.Reverse, delay);

    /// <summary>Repeat a fixed number of times.</summary>
    public static BmotionRepeat Times(int count, BmotionRepeatType type = BmotionRepeatType.Loop, double delay = 0)
        => new(count, type, delay);

    public static implicit operator BmotionRepeat(int count) => new(count, BmotionRepeatType.Loop, 0);

    public bool Equals(BmotionRepeat other) => Count == other.Count && Type == other.Type && Delay.Equals(other.Delay);
    public override bool Equals(object? obj) => obj is BmotionRepeat r && Equals(r);
    public override int GetHashCode() => HashCode.Combine(Count, Type, Delay);
    public static bool operator ==(BmotionRepeat left, BmotionRepeat right) => left.Equals(right);
    public static bool operator !=(BmotionRepeat left, BmotionRepeat right) => !left.Equals(right);
}
