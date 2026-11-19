namespace Bit.BevTools;

/// <summary>A frame the browser's main thread could not paint on time, and the application work that ran in it.</summary>
public sealed record BevToolsLongFrame(DateTimeOffset At, double DurationMs, double BlockingMs, IReadOnlyList<BevToolsFrameScript> Scripts)
{
    /// <summary>The UI event, navigation or render batch that started inside the frame, on WebAssembly, where .NET runs on that thread.</summary>
    public IReadOnlyList<string> During { get; init; } = [];

    public long? TriggerEventId { get; init; }

    /// <summary>
    /// How much of the frame BevTools' own panel spent rendering, on WebAssembly, where it renders on the same thread;
    /// zero elsewhere. A frame mostly the panel's is not recorded at all: it is not the application's.
    /// </summary>
    public double OwnMs { get; init; }

    /// <summary>
    /// The panel rendered more often around the frame than BevTools keeps count of: <see cref="OwnMs"/> is then the least
    /// it spent, and a frame that is not <see cref="IsMostlyBevTools"/> may still be mostly the panel's.
    /// </summary>
    public bool OwnMsIsLowerBound { get; init; }

    /// <summary>At least half the frame was BevTools' own rendering. A lower bound that reaches half says so as surely as an exact total.</summary>
    public bool IsMostlyBevTools => OwnMs > 0 && OwnMs >= DurationMs / 2;

    public long TimelineEventId { get; internal set; }
}
