namespace Bit.BlazorUI;

internal static class BitSplitterJsRuntimeExtensions
{
    /// <remarks>
    /// The whole drag lives on the JavaScript side: a pointer move that had to travel to .NET, be measured
    /// there and travel back would put a round trip - a network one on Blazor Server - between the pointer
    /// and the panel it is dragging. What comes back here is the outcome of a resize rather than the frames
    /// of it, unless the app asked for the frames too.
    /// </remarks>
    internal static ValueTask<string> BitSplitterSetup(this IJSRuntime js,
                                                       DotNetObjectReference<BitSplitter> obj,
                                                       ElementReference root,
                                                       ElementReference firstPanel,
                                                       ElementReference gutter,
                                                       ElementReference secondPanel,
                                                       ElementReference preview,
                                                       bool vertical,
                                                       bool disabled,
                                                       bool collapsible,
                                                       bool collapsed,
                                                       int collapsedSize,
                                                       int keyboardStep,
                                                       int dragStep,
                                                       int snapSize,
                                                       bool lazyResize,
                                                       bool resetOnDoubleClick,
                                                       bool notifyResize,
                                                       bool notifyDoubleClick,
                                                       double? percent,
                                                       string? persistKey,
                                                       bool persistSession)
    {
        return js.Invoke<string>("BitBlazorUI.Splitter.setup",
                                 obj, root, firstPanel, gutter, secondPanel, preview,
                                 vertical, disabled, collapsible, collapsed,
                                 collapsedSize, keyboardStep, dragStep, snapSize, lazyResize,
                                 resetOnDoubleClick, notifyResize, notifyDoubleClick,
                                 percent, persistKey, persistSession);
    }

    internal static ValueTask BitSplitterUpdate(this IJSRuntime js,
                                                string? id,
                                                bool vertical,
                                                bool disabled,
                                                bool collapsible,
                                                bool collapsed,
                                                int collapsedSize,
                                                int keyboardStep,
                                                int dragStep,
                                                int snapSize,
                                                bool lazyResize,
                                                bool resetOnDoubleClick,
                                                bool notifyResize,
                                                bool notifyDoubleClick,
                                                double? percent,
                                                string? persistKey,
                                                bool persistSession)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.update",
                             id, vertical, disabled, collapsible, collapsed,
                             collapsedSize, keyboardStep, dragStep, snapSize, lazyResize,
                             resetOnDoubleClick, notifyResize, notifyDoubleClick,
                             percent, persistKey, persistSession);
    }

    /// <remarks>
    /// The JavaScript side writes the size it dragged the panels to onto the root element itself, which is
    /// not something Blazor is tracking - a render whose style attribute happens not to change leaves those
    /// values standing. This is how the component puts them back in step whenever it has decided on
    /// something other than what was dragged: a null share hands the panels back to their parameters.
    /// </remarks>
    internal static ValueTask BitSplitterSync(this IJSRuntime js, string? id, double? percent)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.sync", id, percent);
    }

    internal static ValueTask BitSplitterDispose(this IJSRuntime js, string? id)
    {
        return js.InvokeVoid("BitBlazorUI.Splitter.dispose", id);
    }
}
