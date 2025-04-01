namespace Bit.BlazorUI;

internal static class BitRichTextEditorJsRuntimeExtensions
{
    public static ValueTask BitRichTextEditorSetup(this IJSRuntime jsRuntime, 
                                                        string id, 
                                                        DotNetObjectReference<BitRichTextEditor>? dotnetObj,
                                                        ElementReference editorContainer, 
                                                        ElementReference toolbarContainer,
                                                        string? theme)
    {
        return jsRuntime.InvokeVoid("BitBlazorUI.RichTextEditor.setup", id, dotnetObj, editorContainer, toolbarContainer, theme);
    }
}
