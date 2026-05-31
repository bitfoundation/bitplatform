using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Extension methods that wrap the <see href="https://developer.mozilla.org/en-US/docs/Web/API/Element/animate">Element.animate()</see>
/// method.
/// </summary>
public static class ElementReferenceAnimationExtensions
{
    /// <summary>
    /// Starts a Web Animation on the element. Returns an <see cref="AnimationHandle"/> for play /
    /// pause / cancel / finish; dispose to cancel.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(AnimationKeyframes))]
    public static async Task<AnimationHandle> Animate(
        this ElementReference element,
        IJSRuntime js,
        AnimationKeyframes keyframes,
        AnimationOptions? options = null)
    {
        options ??= new AnimationOptions();
        var id = Guid.NewGuid();
        await js.InvokeVoid("BitButil.animation.animate", id, element, keyframes, options);
        return new AnimationHandle(js, id);
    }
}
