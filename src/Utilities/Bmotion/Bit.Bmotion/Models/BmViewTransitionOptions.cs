namespace Bit.Bmotion;

/// <summary>
/// Options for <see cref="BmotionViewTransition.StartAsync(Func{Task}, BmViewTransitionOptions?)"/>.
/// </summary>
public sealed class BmViewTransitionOptions
{
    /// <summary>
    /// View transition types (<c>document.startViewTransition({ update, types })</c>): stylesheets select on them with
    /// <c>:active-view-transition-type(forward)</c>, so one page can animate "forwards" and "backwards" navigations
    /// differently. Ignored by browsers without typed view transitions, which still play the untyped transition.
    /// </summary>
    public IReadOnlyList<string>? Types { get; init; }

    /// <summary>
    /// Whether to skip the transition and just run the update: <c>null</c> (the default) follows the library's
    /// reduced-motion policy (<see cref="BitBmotionOptions.ReducedMotion"/>, skipping it when motion is reduced),
    /// <c>true</c> always skips it, <c>false</c> always plays it - for a transition whose stylesheet already honours
    /// <c>prefers-reduced-motion</c> itself.
    /// </summary>
    public bool? ReduceMotion { get; init; }
}
