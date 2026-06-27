namespace Bit.Butil;

/// <summary>
/// Selects which box dimensions trigger the observer. See
/// <see href="https://developer.mozilla.org/en-US/docs/Web/API/ResizeObserver/observe#box">ResizeObserver.observe()</see>.
/// </summary>
public enum ResizeObserverBox
{
    ContentBox,
    BorderBox,
    DevicePixelContentBox
}
