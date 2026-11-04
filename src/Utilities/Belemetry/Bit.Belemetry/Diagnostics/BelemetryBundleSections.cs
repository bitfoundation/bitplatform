namespace Bit.Belemetry;

/// <summary>The sections of a support bundle, which the preview can leave out one by one.</summary>
[Flags]
public enum BelemetryBundleSections
{
    /// <summary>None.</summary>
    None = 0,

    /// <summary>Release, environment and runtime (<c>release.json</c>).</summary>
    Release = 1,

    /// <summary>Exception groups and their occurrences (<c>exceptions.json</c>).</summary>
    Exceptions = 2,

    /// <summary>The .NET side of the timeline (<c>timeline.json</c>).</summary>
    Timeline = 4,

    /// <summary>The browser side of the timeline (<c>timeline.json</c>, events with source <c>browser</c>).</summary>
    Browser = 8,

    /// <summary>The components the timeline names (<c>components.json</c>).</summary>
    Components = 16,

    /// <summary>The application's snapshots (<c>snapshots.json</c>).</summary>
    Snapshots = 32,

    /// <summary>The user's description of what happened.</summary>
    Description = 64,

    /// <summary>Every section.</summary>
    All = Release | Exceptions | Timeline | Browser | Components | Snapshots | Description,
}
