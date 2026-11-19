namespace Bit.Bink;

/// <summary>When and how <see cref="BinkAutosave"/> saves.</summary>
public sealed record BinkAutosaveOptions
{
    /// <summary>Saves two seconds after the last change, and at least every thirty seconds while changes keep coming.</summary>
    public static readonly BinkAutosaveOptions Default = new();

    /// <summary>
    /// How long the document has to stay unchanged before it is saved. Saving only once editing pauses keeps a long
    /// drag or a burst of strokes from serializing the document many times over.
    /// </summary>
    public TimeSpan Delay { get; init; } = TimeSpan.FromSeconds(2);

    /// <summary>The longest a change waits while editing never pauses, so a long session is never far from saved.</summary>
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether changes made by collaborators, and documents loaded into the same instance, are saved too. On by
    /// default: the local copy is what would be recovered, and it should include them.
    /// </summary>
    public bool IncludeRemoteChanges { get; init; } = true;

    /// <summary>Whether disposing saves changes that are still waiting. On by default.</summary>
    public bool SaveOnDispose { get; init; } = true;

    /// <summary>How the document is written.</summary>
    public BinkSerializerOptions SerializerOptions { get; init; } = BinkSerializerOptions.Default;

    /// <summary>The clock the delays are measured on. Replaced by tests.</summary>
    public TimeProvider TimeProvider { get; init; } = TimeProvider.System;
}
