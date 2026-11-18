namespace Bit.BevTools;

/// <summary>A reference BevTools found to a disposed component that is still alive: something that keeps it.</summary>
/// <param name="Holder">What holds it: "FilterState.Changed (injected)", "the JS runtime", "its own field _timer".</param>
/// <param name="Detail">How: "handler ProductRow.OnFilterChanged", "lambda in ProductRow.OnInitialized captures it".</param>
/// <param name="Suggestion">What releases it.</param>
public sealed record BevToolsRetentionCause(string Holder, string Detail, string Suggestion)
{
    /// <summary>
    /// A hold that keeps one instance at a time and lets it go when it is overwritten: a single field of a component still
    /// on the page (an <c>@ref</c> inside an <c>@if</c>), which the next render of that markup replaces. It costs one
    /// instance, it does not grow, so it is not by itself evidence of a leak.
    /// </summary>
    public bool Bounded { get; init; }
}
