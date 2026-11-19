namespace Bit.Bheadless;

/// <summary>The state a <see cref="BitBhPinInput"/> shares with its <see cref="BitBhPinInputSlot"/>s.</summary>
public sealed class BitBhPinInputContext
{
    internal BitBhPinInputContext(BitBhPinInput owner) => Owner = owner;

    internal BitBhPinInput Owner { get; }

    /// <summary>The current code.</summary>
    public string Value => Owner.CurrentValue;

    /// <summary>The number of positions.</summary>
    public int Length => Owner.Length;

    /// <summary>Whether every position is filled.</summary>
    public bool IsComplete => Owner.IsComplete;
}
