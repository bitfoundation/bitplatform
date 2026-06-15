
namespace Bit.Bmotion;
/// <summary>
/// Programmatic animation controller.
/// Analogous to Framer Motion's <c>useAnimate()</c>.
/// Obtain via DI (<c>@inject BmotionAnimationController</c>) and bind to an element ID.
/// All animation math runs in the C# <see cref="BmotionAnimationEngine"/>.
/// </summary>
public sealed class BmotionAnimationController : IDisposable
{
    private readonly BmotionAnimationEngine _engine;
    private string? _elementId;

    public BmotionAnimationController(BmotionAnimationEngine engine) => _engine = engine;

    /// <summary>
    /// Bind by element ID. Ensures the element is registered with the engine so the controller
    /// works even when the target isn't wrapped in a <c>&lt;Bmotion&gt;</c> component.
    /// </summary>
    public void BindTo(string elementId)
    {
        if (string.IsNullOrWhiteSpace(elementId))
            throw new ArgumentException("Element ID must not be null or whitespace.", nameof(elementId));
        // Release the previously bound element so repeated BindTo calls don't leak engine state.
        if (!string.IsNullOrEmpty(_elementId) && _elementId != elementId)
            _engine.UnregisterElement(_elementId);
        _elementId = elementId;
        _engine.RegisterElement(elementId);
    }

    /// <summary>Animate the bound element to the given props (fire-and-forget).</summary>
    public async ValueTask AnimateAsync(BmotionAnimationProps props, BmotionTransitionConfig? transition = null)
    {
        if (_elementId == null) return;
        await _engine.AnimateToAsync(_elementId, props.ToJsDictionary(), transition);
    }

    /// <summary>Animate and await completion.</summary>
    public async ValueTask AnimateAwaitAsync(BmotionAnimationProps props, BmotionTransitionConfig? transition = null)
    {
        if (_elementId == null) return;
        await _engine.AnimateToAwaitAsync(_elementId, props.ToJsDictionary(), transition);
    }

    /// <summary>Instantly set props without animation.</summary>
    public void Set(BmotionAnimationProps props)
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

    /// <summary>Unregister the bound element from the engine when the controller is disposed.</summary>
    public void Dispose()
    {
        if (!string.IsNullOrEmpty(_elementId))
        {
            _engine.UnregisterElement(_elementId);
            _elementId = null;
        }
    }
}
