namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownEditor is a simple editor like GitHub md editor.
/// </summary>
public partial class BitMarkdownEditor : BitComponentBase
{
    private ElementReference _textAreaRef = default!;



    [Inject] private IJSRuntime _js { get; set; } = default!;



    /// <summary>
    /// Returns the current value of the editor.
    /// </summary>
    public async ValueTask<string> GetValue()
    {
        return await _js.BitMarkdownEditorGetValue(_Id);
    }



    protected override string RootElementClass => "bit-mde";

    protected override Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _js.BitMarkdownEditorInit(_Id, _textAreaRef);
        }

        return base.OnAfterRenderAsync(firstRender);
    }
}
