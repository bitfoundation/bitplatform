namespace Bit.BlazorUI;

/// <summary>
/// BitMarkdownViewer is a Blazor wrapper around the famous markedjs library.
/// <see href="https://github.com/markedjs/marked"/>
/// </summary>
public partial class BitMarkdownViewer : BitComponentBase
{
    [Inject] private IJSRuntime _js { get; set; } = default!;



    protected override string RootElementClass => "bit-pdr";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender is false) return;

        string[] scripts = [
            "_content/Bit.BlazorUI.Extras/marked/marked-15.0.7.js"
        ];

        await _js.BitPdfReaderInit(scripts);
    }
}
