namespace Bit.Bheadless;

/// <summary>Render-fragment context exposed by hover-card parts.</summary>
public sealed record BitBhHoverCardRenderContext
{
    /// <summary>Whether the card is open.</summary>
    public required bool IsOpen { get; init; }
}
