namespace Bit.Bheadless;

/// <summary>Live-region politeness.</summary>
public enum BitBhPoliteness
{
    /// <summary>Announced when the user is idle (<c>aria-live="polite"</c>).</summary>
    Polite,

    /// <summary>Interrupts the user (<c>aria-live="assertive"</c>). Use sparingly.</summary>
    Assertive,
}
