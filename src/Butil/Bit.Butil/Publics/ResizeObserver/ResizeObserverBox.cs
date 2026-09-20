namespace Bit.Butil;

/// <summary>
/// Selects which box dimensions trigger the observer. See
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver/observe#box">ResizeObserver.observe()</see>.
/// </summary>
public enum ResizeObserverBox
{
    /// <summary>The content box - the element's size excluding padding and border. The default.</summary>
    ContentBox,

    /// <summary>The border box - the content box plus padding and border.</summary>
    BorderBox,
    
    /// <summary>The content box in device pixels, which is what a canvas backing store should be sized to.</summary>
    DevicePixelContentBox
}
