namespace Bit.Bmotion;

/// <summary>How a repeating animation behaves on each subsequent iteration.</summary>
public enum BmotionRepeatType
{
    /// <summary>Restart from the beginning each iteration (0 → 1, 0 → 1, …).</summary>
    Loop,
    /// <summary>Ping-pong: alternate direction each iteration (0 → 1, 1 → 0, …).</summary>
    Mirror,
    /// <summary>Jump back to the start and play in reverse each iteration (1 → 0, 1 → 0, …).</summary>
    Reverse,
}
