namespace Bit.BevTools;

/// <summary>One user interaction (Event Timing) as the page hub reports it.</summary>
public sealed class BevToolsInteractionPayload
{
    public double AgeMs { get; set; }

    public string? Name { get; set; }

    public double DurationMs { get; set; }

    public double InputDelayMs { get; set; }

    public double ProcessingMs { get; set; }

    public double PresentationMs { get; set; }

    public double ProcessingStartAgeMs { get; set; }

    public double ProcessingEndAgeMs { get; set; }

    public string? Target { get; set; }

    /// <summary>The browser's interactionId: what the parts of one interaction reported apart have in common. 0 from a hub that does not say.</summary>
    public long InteractionId { get; set; }
}
