namespace Bit.BlazorUI;

internal static class BitAiMessageListJsRuntimeExtensions
{
    internal static ValueTask BitAiMessageListObserve(this IJSRuntime jsRuntime,
                                                      string id,
                                                      ElementReference element,
                                                      DotNetObjectReference<BitAiMessageList> dotnetObj,
                                                      bool autoScroll)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiMessageList.observe", id, element, dotnetObj, autoScroll);
    }

    internal static ValueTask BitAiMessageListSetAutoScroll(this IJSRuntime jsRuntime, string id, bool autoScroll)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiMessageList.setAutoScroll", id, autoScroll);
    }

    internal static ValueTask BitAiMessageListScrollToBottom(this IJSRuntime jsRuntime, ElementReference element, bool smooth)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiMessageList.scrollToBottom", element, smooth);
    }

    internal static ValueTask BitAiMessageListDispose(this IJSRuntime jsRuntime, string id)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.AiMessageList.dispose", id);
    }
}
