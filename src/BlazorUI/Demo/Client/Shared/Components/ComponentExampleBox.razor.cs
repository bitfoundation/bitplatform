namespace Bit.BlazorUI.Demo.Client.Shared.Components;

public partial class ComponentExampleBox
{
    private bool showCode;

    [Parameter] public string Title { get; set; } = default!;
    [Parameter] public string ExampleId { get; set; } = default!;
    [Parameter] public string HTMLSourceCode { get; set; } = default!;
    [Parameter] public string CSharpSourceCode { get; set; } = default!;
    [Parameter] public RenderFragment ExamplePreview { get; set; } = default!;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoidAsync("highlightSnippet");
    }

    private async Task CopyCodeToClipboard()
    {
        var code = string.IsNullOrEmpty(CSharpSourceCode) is false
            ? AppendCodePharaseToCSharpCode(CSharpSourceCode)
            : "";

        await JSRuntime.CopyToClipboard(HTMLSourceCode.Trim() + code);
    }

    private string AppendCodePharaseToCSharpCode(string cSharpSourceCode)
    {
        string code = $@"{"\n\n"}@code {{
{@CSharpSourceCode.Trim().Replace("\n", "\n\t")}
}}";
        return code;
    }

    private async Task CopyLinkToClipboard()
    {
        var currentUrl = NavigationManager.Uri;
        currentUrl = currentUrl.Contains("#") ? currentUrl.Substring(0, currentUrl.IndexOf("#")) : currentUrl;
        var exampleUrl = $"{currentUrl}#{ExampleId}";
        await JSRuntime.CopyToClipboard(exampleUrl);
    }
}
