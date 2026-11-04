namespace Bit.BlazorUI;

/// <summary>
/// Selected bars the engine is asking the host to clone on paste
/// (<c>Ctrl</c>/<c>Cmd</c>+<c>V</c>). The host must assign new ids; the engine
/// never duplicates them. In-process only.
/// </summary>
public sealed class BitResourceTimelineCopyRequest
{
    /// <summary>The bars on the engine clipboard, in selection order.</summary>
    public required IReadOnlyList<BitResourceTimelineAllocation> Allocations { get; init; }

    /// <summary>
    /// Time shift to apply to each clone (typically one edit-snap step). The
    /// host may ignore this and place the copies elsewhere.
    /// </summary>
    public TimeSpan Offset { get; init; }

    /// <summary>
    /// Resource under the keyboard focus when pasting, or the first source
    /// bar's resource when there is no focus. Null when the timeline has no
    /// rows.
    /// </summary>
    public string? ResourceId { get; init; }
}
