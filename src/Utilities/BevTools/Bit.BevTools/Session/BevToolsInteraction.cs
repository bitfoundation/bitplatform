namespace Bit.BevTools;

/// <summary>
/// One user interaction, from input to the next paint: the input delay (the main thread busy with something else), the
/// processing (its event handlers), and the presentation delay (rendering the frame after them).
/// </summary>
public sealed record BevToolsInteraction(DateTimeOffset At, string Name, double DurationMs, double InputDelayMs, double ProcessingMs, double PresentationMs, string? Target)
{
    /// <summary>The UI event whose handler ran inside the interaction's processing, on WebAssembly; null elsewhere or when none or several did.</summary>
    public long? HandlerEventId { get; init; }

    public string? HandlerTitle { get; init; }

    /// <summary>The browser's interactionId, by which its parts reported apart are put together; 0 when not known.</summary>
    public long InteractionId { get; init; }

    public long TimelineEventId { get; internal set; }
}
