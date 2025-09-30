namespace Bit.BlazorUI;

/// <summary>
/// BitRichTextEditor is a WYSIWYG text editor, utilizing the famous Quill js library (<see href="https://quilljs.com/"/>).
/// </summary>
public partial class BitRichTextEditor : BitComponentBase
{
    private ElementReference _editorRef = default!;
    private ElementReference _toolbarRef = default!;
    private TaskCompletionSource _readyTcs = new();
    private DotNetObjectReference<BitRichTextEditor>? _dotnetObj = null;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Custom CSS classes for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Classes { get; set; }

    /// <summary>
    /// Custom template for the editor content.
    /// </summary>
    [Parameter] public RenderFragment? EditorTemplate { get; set; }

    /// <summary>
    /// Renders the full toolbar with all of the available features.
    /// </summary>
    [Parameter] public bool FullToolbar { get; set; }

    /// <summary>
    /// Custom Quill modules to be registered at first render (<see href="https://quilljs.com/docs/guides/building-a-custom-module"/>).
    /// </summary>
    [Parameter] public IEnumerable<BitRichTextEditorModule>? Modules { get; set; }

    /// <summary>
    /// Callback for when the editor instance is created and ready to use.
    /// </summary>
    [Parameter] public EventCallback<string> OnEditorReady { get; set; }

    /// <summary>
    /// Callback for when the Quill scripts is loaded and the Quill api is ready to use. It allows for custom actions to be performed at that moment.
    /// </summary>
    [Parameter] public EventCallback OnQuillReady { get; set; }

    /// <summary>
    /// Callback for when the scripts of the provided Quill Modules are loaded and their api are ready to use.
    /// </summary>
    [Parameter] public EventCallback OnQuillModulesReady { get; set; }

    /// <summary>
    /// The placeholder value of the editor.
    /// </summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>
    /// Makes the editor readonly.
    /// </summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>
    /// Reverses the location of the Toolbar and the Editor.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public bool Reversed { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the rich text editor.
    /// </summary>
    [Parameter] public BitRichTextEditorClassStyles? Styles { get; set; }

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
    public async ValueTask<string> GetText()
    {
        await _readyTcs.Task;
        return await _js.BitRichTextEditorGetText(_Id);
    }

    /// <summary>
    /// Gets the current html content of the editor.
    /// </summary>
    public async ValueTask<string> GetHtml()
    {
        await _readyTcs.Task;
        return await _js.BitRichTextEditorGetHtml(_Id);
    }

    /// <summary>
    /// Gets the current content of the editor in JSON format.
    /// </summary>
    public async ValueTask<string> GetContent()
    {
        await _readyTcs.Task;
        return await _js.BitRichTextEditorGetContent(_Id);
    }

    /// <summary>
    /// Sets the current text content of the editor.
    /// </summary>
    public async ValueTask SetText(string? text)
    {
        await _readyTcs.Task;
        await _js.BitRichTextEditorSetText(_Id, text);
    }

    /// <summary>
    /// Sets the current html content of the editor.
    /// </summary>
    public async ValueTask SetHtml(string? html)
    {
        await _readyTcs.Task;
        await _js.BitRichTextEditorSetHtml(_Id, html);
    }

    /// <summary>
    /// Sets the current content of the editor in JSON format.
    /// </summary>
    public async ValueTask SetContent(string? content)
    {
        await _readyTcs.Task;
        await _js.BitRichTextEditorSetContent(_Id, content);
    }



    protected override string RootElementClass => "bit-rte";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);

        ClassBuilder.Register(() => Reversed ? "bit-rte-rvs" : string.Empty);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        await _js.BitExtrasInitScripts(["_content/Bit.BlazorUI.Extras/quilljs/quill-2.0.3.js"]);

        _ = OnQuillReady.InvokeAsync();

        var theme = (Theme ?? BitRichTextEditorTheme.Snow).ToString().ToLower();
        await _js.BitExtrasInitStylesheets([$"_content/Bit.BlazorUI.Extras/quilljs/quill.{theme}-2.0.3.css"]);

        List<QuillModule> quillModules = [];

        if (Modules is not null)
        {
            List<string> quillModuleScripts = [];
            foreach (var module in Modules)
            {
                quillModuleScripts.Add(module.Src);
                quillModules.Add(new() { Name = module.Name, Config = module.Config });
            }

            try
            {
                await _js.BitExtrasInitScripts(quillModuleScripts);

                _ = OnQuillModulesReady.InvokeAsync();
            }
            catch
            {
                // we need to ignore script load exceptions here, since we can't safely recover from such errors in this state!
                // so the developers should make sure the scripts they are providing is correct and has no issue to load.
            }
        }

        _dotnetObj = DotNetObjectReference.Create(this);
        ElementReference? toolbarRef = ToolbarTemplate is null ? null : _toolbarRef;
        await _js.BitRichTextEditorSetup(_Id, _dotnetObj, _editorRef, toolbarRef, theme, Placeholder, ReadOnly, FullToolbar, Styles?.Toolbar, Classes?.Toolbar, quillModules);
        _readyTcs.SetResult();

        await OnEditorReady.InvokeAsync(_Id);
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
