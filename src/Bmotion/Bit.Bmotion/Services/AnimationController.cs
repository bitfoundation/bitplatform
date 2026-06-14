using Bit.Bmotion.Engine;
using Bit.Bmotion.Models;

namespace Bit.Bmotion.Services;

/// <summary>
/// Programmatic animation controller.
/// Analogous to Framer Motion's <c>useAnimate()</c>.
/// Obtain via DI (<c>@inject AnimationController</c>) and bind to an element ID.
/// All animation math runs in the C# <see cref="AnimationEngine"/>.
/// </summary>
public sealed class AnimationController
{
    private readonly AnimationEngine _engine;
    private string? _elementId;

    public AnimationController(AnimationEngine engine) => _engine = engine;

    /// <summary>Bind by element ID.</summary>
    public void BindTo(string elementId) => _elementId = elementId;

    /// <summary>Animate the bound element to the given props (fire-and-forget).</summary>
    public async ValueTask AnimateAsync(AnimationProps props, TransitionConfig? transition = null)
    {
        if (_elementId == null) return;
        await _engine.AnimateToAsync(_elementId, props.ToJsDictionary(), transition);
    }

    /// <summary>Animate and await completion.</summary>
    public async ValueTask AnimateAwaitAsync(AnimationProps props, TransitionConfig? transition = null)
    {
        if (_elementId == null) return;
        await _engine.AnimateToAwaitAsync(_elementId, props.ToJsDictionary(), transition);
    }

    /// <summary>Instantly set props without animation.</summary>
    public void Set(AnimationProps props)
    {
        if (_elementId == null) return;
        _engine.SetInstant(_elementId, props.ToJsDictionary());
    }

    /// <summary>Stop animations on the bound element.</summary>
    public void Stop(params string[] properties)
    {
        if (_elementId == null) return;
        var props = properties == null || properties.Length == 0 ? null : properties;
        _engine.Stop(_elementId, props);
    }
}
