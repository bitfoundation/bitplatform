namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by tooltip parts.</summary>
public sealed record BitBhTooltipRenderContext
{
    /// <summary>Whether the tooltip is open.</summary>
    public required bool IsOpen { get; init; }

    /// <summary>Whether it opened after the hover delay.</summary>
    public required bool DelayedOpen { get; init; }
}
