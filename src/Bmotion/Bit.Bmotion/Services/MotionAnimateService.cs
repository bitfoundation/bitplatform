using Bit.Bmotion.Engine;
using Bit.Bmotion.Interop;
using Bit.Bmotion.Models;
using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion.Services;

/// <summary>
/// Provides a method-based animation API analogous to the <c>animate()</c> function in
/// <see href="https://motion.dev/docs/quick-start">motion.dev</see>.
/// <para>
/// Elements are identified by a CSS selector string or a Blazor <see cref="ElementReference"/>.
/// They do <em>not</em> need to be wrapped in a <c>&lt;Motion&gt;</c> component.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // By CSS selector
/// var controls = await Motion.AnimateAsync(".box", new AnimationProps { X = 100, Opacity = 1 });
/// await controls; // wait for completion
///
/// // By ElementReference captured via @ref
/// var controls = await Motion.AnimateAsync(myRef, new AnimationProps { Scale = 1.2 },
///                                          TransitionConfig.Spring());
/// controls.Stop(); // cancel early
/// </code>
/// </example>
public sealed class MotionAnimateService
{
    private readonly AnimationEngine _engine;
    private readonly MotionInterop _interop;

    public MotionAnimateService(AnimationEngine engine, MotionInterop interop)
    {
        _engine = engine;
        _interop = interop;
    }

    /// <summary>
    /// Animate all DOM elements matching <paramref name="selector"/> to
    /// <paramref name="keyframes"/>.
    /// </summary>
    /// <param name="selector">
    /// A CSS selector string, e.g. <c>".card"</c>, <c>"#hero"</c>, or <c>"div.item"</c>.
    /// Multiple matching elements are animated simultaneously.
    /// </param>
    /// <param name="keyframes">Target animation properties.</param>
    /// <param name="transition">
    /// Optional transition configuration (easing, duration, spring parameters, etc.).
    /// Falls back to the global <see cref="Components.MotionConfig"/> default when omitted.
    /// </param>
    /// <returns>
    /// An <see cref="AnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<AnimationControls> AnimateAsync(
        string selector,
        AnimationProps keyframes,
        TransitionConfig? transition = null)
    {
        var ids = await _interop.ResolveOrRegisterBySelectorAsync(selector);
        return StartAnimations(ids, keyframes, transition);
    }

    /// <summary>
    /// Animate the element captured by <paramref name="elementReference"/> to
    /// <paramref name="keyframes"/>.
    /// </summary>
    /// <param name="elementReference">
    /// A Blazor <see cref="ElementReference"/> obtained via <c>@ref</c> on any HTML element.
    /// </param>
    /// <param name="keyframes">Target animation properties.</param>
    /// <param name="transition">Optional transition configuration.</param>
    /// <returns>
    /// An <see cref="AnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<AnimationControls> AnimateAsync(
        ElementReference elementReference,
        AnimationProps keyframes,
        TransitionConfig? transition = null)
    {
        var id = await _interop.ResolveOrRegisterByRefAsync(elementReference);
        return StartAnimations([id], keyframes, transition);
    }

    // ────────────────────────────────────────────────────────────────────────────

    private AnimationControls StartAnimations(
        string[] elementIds,
        AnimationProps keyframes,
        TransitionConfig? transition)
    {
        var values = keyframes.ToJsDictionary();

        // Only the elements we register here (i.e. not already owned by a <Motion>) are ours to
        // clean up afterwards, so the engine's element table doesn't grow unbounded over time.
        var ours = new List<string>();
        foreach (var id in elementIds)
        {
            if (!_engine.IsRegistered(id))
            {
                _engine.RegisterElement(id);
                ours.Add(id);
            }
        }

        // Start all animations concurrently; collect their completion tasks.
        var completionTasks = elementIds
            .Select(id => _engine.AnimateToAwaitAsync(id, values, transition).AsTask())
            .ToArray();

        var completion = Task.WhenAll(completionTasks);

        if (ours.Count > 0)
        {
            // Release engine state for the elements we created once their animations settle.
            _ = completion.ContinueWith(_ =>
            {
                foreach (var id in ours)
                    _engine.UnregisterElement(id);
            }, TaskScheduler.Default);
        }

        return new AnimationControls(elementIds, _engine, completion);
    }
}
