namespace Bit.BlazorUI;

/// <summary>
/// Source of a <see cref="BitMap{TMapProvider}.OnInteropError"/> event.
/// </summary>
public enum BitMapInteropErrorSource
{
    /// <summary>Loading the provider's external scripts failed.</summary>
    ScriptLoad,

    /// <summary>Loading the provider's external stylesheets failed.</summary>
    StylesheetLoad,

    /// <summary>The provider's <c>init</c> JS call failed.</summary>
    Init,

    /// <summary>The provider's <c>sync</c> JS call failed.</summary>
    Sync,

    /// <summary>The provider's <c>dispose</c> JS call failed.</summary>
    Dispose,

    /// <summary>An imperative call (add/remove/update) into the provider failed.</summary>
    Imperative,

    /// <summary>A consumer-supplied event handler threw while processing a JS-invoked callback.</summary>
    Callback,
}

/// <summary>
/// Payload for <see cref="BitMap{TMapProvider}.OnInteropError"/>. Carries the underlying
/// exception and a tag indicating which interop call site produced it.
/// </summary>
public sealed class BitMapInteropErrorArgs
{
    /// <summary>Where the error originated.</summary>
    public required BitMapInteropErrorSource Source { get; init; }

    /// <summary>The exception that was caught.</summary>
    public required Exception Exception { get; init; }

    /// <summary>Optional context (e.g. method name, marker id) for diagnostics.</summary>
    public string? Context { get; init; }
}
