using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Extension methods that wrap the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/animate">Element.animate()</see>
/// method and the rest of the Web Animations API on an element.
/// </summary>
public static class ElementReferenceAnimationExtensions
{
    /// <summary>
    /// Starts a Web Animation on the element. Returns an <see cref="AnimationHandle"/> for play /
    /// pause / cancel / finish; dispose to cancel.
    /// </summary>
    /// <param name="element">The element to animate.</param>
    /// <param name="js">The interop runtime.</param>
    /// <param name="keyframes">The keyframes to animate through.</param>
    /// <param name="options">Timing and fill, and optionally a scroll-driven timeline.</param>
    /// <param name="timelineSource">
    /// Only used with <see cref="AnimationOptions.Timeline"/>: the scroller for a <c>"scroll"</c>
    /// timeline, or the subject whose passage through the scrollport drives a <c>"view"</c> one.
    /// Omit for the nearest scrollport, respectively the animated element itself.
    /// </param>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationTimelineOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationKeyframes))]
    public static async Task<AnimationHandle> Animate(
        this ElementReference element,
        IJSRuntime js,
        AnimationKeyframes keyframes,
        AnimationOptions? options = null,
        ElementReference? timelineSource = null)
    {
        options ??= new AnimationOptions();
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.animation.animate", id, element, keyframes, options, timelineSource);
        return new AnimationHandle(js, id);
    }

    /// <summary>
    /// True when the runtime implements scroll-driven timelines (<c>ScrollTimeline</c> /
    /// <c>ViewTimeline</c>) - see <see cref="AnimationOptions.Timeline"/>.
    /// </summary>
    /// <remarks>
    /// Where it is false, an animation asking for one runs on the ordinary clock instead, so it
    /// degrades rather than failing.
    /// </remarks>
    public static ValueTask<bool> IsTimelineSupported(this ElementReference element, IJSRuntime js)
        => js.Invoke<bool>("BitButil.animation.isTimelineSupported");

    /// <summary>
    /// Every animation currently affecting the element - <b>including</b> CSS animations and
    /// transitions the page never scripted.
    /// <br/>
    /// <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/getAnimations">https://developer.mozilla.org/en-US/docs/Web/API/Element/getAnimations</see>
    /// </summary>
    /// <param name="element">The element to inspect.</param>
    /// <param name="js">The interop runtime.</param>
    /// <param name="subtree">Also include animations on descendants.</param>
    /// <remarks>
    /// This is how to answer "is anything still animating here" before measuring, snapshotting or
    /// tearing an element down - an <see cref="AnimationHandle"/> only knows about the animations
    /// Butil itself started.
    /// </remarks>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationInfo))]
    public static ValueTask<AnimationInfo[]> GetAnimations(this ElementReference element, IJSRuntime js, bool subtree = false)
        => js.Invoke<AnimationInfo[]>("BitButil.animation.getAnimations", element, subtree);

    /// <summary>
    /// Cancels every animation on the element, whoever started it.
    /// </summary>
    /// <param name="element">The element to clear.</param>
    /// <param name="js">The interop runtime.</param>
    /// <param name="subtree">Also cancel animations on descendants.</param>
    /// <returns>How many animations were cancelled.</returns>
    /// <remarks>
    /// A blunt instrument, and the point of it: a CSS animation Butil never started has no handle to
    /// cancel it through.
    /// </remarks>
    public static ValueTask<int> CancelAnimations(this ElementReference element, IJSRuntime js, bool subtree = false)
        => js.Invoke<int>("BitButil.animation.cancelAll", element, subtree);
}
