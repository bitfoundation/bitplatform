// This component is using the quilljs project as its editor: https://quilljs.com/

namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownEditor is a simple editor like GitHub md editor.
/// </summary>
public partial class BitRichTextEditor : BitComponentBase
{
    private ElementReference _toolbarRef = default!;
    private ElementReference _editorRef = default!;
    private DotNetObjectReference<BitRichTextEditor>? _dotnetObj = null;



    [Inject] private IJSRuntime _js { get; set; } = default!;


    /// <summary>
    /// Custom template for the editor content.
    /// </summary>
    [Parameter] public RenderFragment? EditorTemplate { get; set; }

    /// <summary>
    /// The placeholder value of the editor.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// The theme of the editor.
    /// </summary>
    [Parameter] public BitRichTextEditorTheme? Theme { get; set; }

    /// <summary>
    /// Custom template for the toolbar content.
    /// </summary>
    [Parameter] public RenderFragment? ToolbarTemplate { get; set; }



    /// <summary>
    /// Gets the current text content of the editor.
    /// </summary>
    public ValueTask<string> GetText()
    {
        return _js.BitRichTextEditorGetText(_Id);
    }

    /// <summary>
    /// Gets the current html content of the editor.
    /// </summary>
    public ValueTask<string> GetHtml()
    {
        return _js.BitRichTextEditorGetHtml(_Id);
    }

    /// <summary>
    /// Gets the current content of the editor in JSON format.
    /// </summary>
    public ValueTask<string> GetContent()
    {
        return _js.BitRichTextEditorGetContent(_Id);
    }

    /// <summary>
    /// Sets the current text content of the editor.
    /// </summary>
    public ValueTask SetText(string? text)
    {
        return _js.BitRichTextEditorSetText(_Id, text);
    }

    /// <summary>
    /// Sets the current html content of the editor.
    /// </summary>
    public ValueTask SetHtml(string? html)
    {
        return _js.BitRichTextEditorSetHtml(_Id, html);
    }

    /// <summary>
    /// Sets the current content of the editor in JSON format.
    /// </summary>
    public ValueTask SetContent(string? content)
    {
        return _js.BitRichTextEditorSetContent(_Id, content);
    }



    protected override string RootElementClass => "bit-rte";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            var theme = (Theme ?? BitRichTextEditorTheme.Snow).ToString().ToLower();
            await _js.BitExtrasInitScripts(["_content/Bit.BlazorUI.Extras/quilljs/quill-2.0.3.js"]);
            await _js.BitExtrasInitStylesheets([$"_content/Bit.BlazorUI.Extras/quilljs/quill.{theme}-2.0.3.css"]);

            _dotnetObj = DotNetObjectReference.Create(this);
            ElementReference? toolbarRef = ToolbarTemplate is null ? null : _toolbarRef;
            await _js.BitRichTextEditorSetup(_Id, _dotnetObj, _editorRef, toolbarRef, theme, Placeholder, ReadOnly);
        }
    }



    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (IsDisposed || disposing is false) return;

        _dotnetObj?.Dispose();

        try
        {
            await _js.BitMarkdownEditorDispose(_Id);
        }
        catch (JSDisconnectedException) { } // we can ignore this exception here


        await base.DisposeAsync(disposing);
    }
}
