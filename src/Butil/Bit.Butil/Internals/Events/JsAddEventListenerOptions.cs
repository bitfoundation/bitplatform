namespace Bit.Butil;

/// <summary>
/// Wire shape passed to <c>addEventListener</c> on the JS side when more than just the capture
/// flag is needed. Blazor's interop serializes with camelCase, so these map to the native
/// <c>{ capture, passive, once }</c> options object.
/// </summary>
internal sealed class JsAddEventListenerOptions
{
    public bool Capture { get; set; }
    public bool Passive { get; set; }
    public bool Once { get; set; }
}
