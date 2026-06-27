using Microsoft.AspNetCore.Components;

namespace Bit.Bmotion;
/// <summary>
/// Provides a method-based animation API analogous to the <c>animate()</c> function in
/// <see href="https://motion.dev/docs/quick-start">motion.dev</see>.
/// <para>
/// Elements are identified by a CSS selector string or a Blazor <see cref="ElementReference"/>.
/// They do <em>not</em> need to be wrapped in a <c>&lt;Bmotion&gt;</c> component.
/// </para>
/// </summary>
/// <example>
/// <code>
/// // By CSS selector
/// var controls = await Motion.AnimateAsync(".box", new BmotionAnimationProps { X = 100, Opacity = 1 });
/// await controls; // wait for completion
///
/// // By ElementReference captured via @ref
/// var controls = await Motion.AnimateAsync(myRef, new BmotionAnimationProps { Scale = 1.2 },
///                                          BmotionTransitionConfig.Spring());
/// controls.Stop(); // cancel early
/// </code>
/// </example>
public sealed class BmotionAnimateService
{
    private readonly BmotionAnimationEngine _engine;
    private readonly BmotionInterop _interop;

    public BmotionAnimateService(BmotionAnimationEngine engine, BmotionInterop interop)
    {
        ArgumentNullException.ThrowIfNull(engine);
        ArgumentNullException.ThrowIfNull(interop);
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
    /// Falls back to the global <see cref="BmotionConfig"/> default when omitted.
    /// </param>
    /// <returns>
    /// An <see cref="BmotionAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmotionAnimationControls> AnimateAsync(
        string selector,
        BmotionAnimationProps keyframes,
        BmotionTransitionConfig? transition = null)
    {
        if (string.IsNullOrWhiteSpace(selector))
            throw new ArgumentException("Selector must not be null or whitespace.", nameof(selector));
        ArgumentNullException.ThrowIfNull(keyframes);
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
    /// An <see cref="BmotionAnimationControls"/> that can be <c>await</c>ed or stopped early.
    /// </returns>
    public async ValueTask<BmotionAnimationControls> AnimateAsync(
        ElementReference elementReference,
        BmotionAnimationProps keyframes,
        BmotionTransitionConfig? transition = null)
    {
        ArgumentNullException.ThrowIfNull(keyframes);
        var id = await _interop.ResolveOrRegisterByRefAsync(elementReference);
        return StartAnimations([id], keyframes, transition);
    }

    // ────────────────────────────────────────────────────────────────────────────

    private BmotionAnimationControls StartAnimations(
        string[] elementIds,
        BmotionAnimationProps keyframes,
        BmotionTransitionConfig? transition)
    {
        var values = keyframes.ToJsDictionary();

        // Reference-count every target for the lifetime of this call. Overlapping AnimateAsync
        // invocations (and any wrapping <Bmotion>) each hold a count, so an element is only torn
        // down once the last animation settles - a single completing call can't unregister an
        // element out from under another still-running animation.
        foreach (var id in elementIds)
            _engine.RegisterElement(id);

        // Start all animations concurrently; collect their completion tasks. If task creation
        // throws synchronously (e.g. a driver rejects the keyframes), release every element we
        // already registered so they don't leak a refcount before the exception propagates.
        Task[] completionTasks;
        try
        {
            completionTasks = elementIds
                .Select(id => _engine.AnimateToAwaitAsync(id, values, transition).AsTask())
                .ToArray();
        }
        catch
        {
            foreach (var id in elementIds)
                _engine.UnregisterElement(id);
            throw;
        }

        var completion = Task.WhenAll(completionTasks);

        // The controls own the single refcount release (idempotent). It fires whichever happens
        // first: natural completion, Stop(), or Complete(). This is what prevents an
        // infinite-repeat animation - whose completion task never resolves - from leaking refcounts.
        var released = false;
        void Release()
        {
            if (released) return;
            released = true;
            foreach (var id in elementIds)
                _engine.UnregisterElement(id);
        }

        var controls = new BmotionAnimationControls(elementIds, _engine, completion, Release);

        _ = completion.ContinueWith(
            _ => controls.OnCompletionSettled(),
            TaskScheduler.Default);

        return controls;
    }
}
