namespace Bit.BlazorUI;

internal static class BitCommandPaletteJsRuntimeExtensions
{
    // Plain values rather than one options object: the serializer discovers an object's shape through reflection, and a
    // trimmed app no longer has the metadata it needs for that. The handle it answers with is called by its members.
    public static ValueTask<IJSObjectReference> BitCommandPaletteSetup(this IJSRuntime jsRuntime,
                                                                       ElementReference root,
                                                                       DotNetObjectReference<BitCommandPalette> dotnetObj,
                                                                       List<string> toggles,
                                                                       List<string> shortcuts,
                                                                       bool suppressInEditable,
                                                                       int sequenceTimeout,
                                                                       bool vim,
                                                                       bool inline)
    {
        return jsRuntime.InvokeScriptAsync<IJSObjectReference>("BitBlazorUI.BitCommandPalette.setup",
                                                               root, dotnetObj, toggles, shortcuts, suppressInEditable, sequenceTimeout, vim, inline);
    }
}
