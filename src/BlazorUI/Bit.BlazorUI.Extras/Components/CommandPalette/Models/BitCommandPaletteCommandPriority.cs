namespace Bit.BlazorUI;

/// <summary>Well-known priority levels (any int is allowed).</summary>
public static class BitCommandPaletteCommandPriority
{
    /// <summary>Listed last; e.g. a feedback row.</summary>
    public const int Lowest = -100;
    /// <summary>Below the default.</summary>
    public const int Low = -50;
    /// <summary>The default.</summary>
    public const int Normal = 0;
    /// <summary>Above the default.</summary>
    public const int High = 50;
    /// <summary>Listed first.</summary>
    public const int Highest = 100;
}
