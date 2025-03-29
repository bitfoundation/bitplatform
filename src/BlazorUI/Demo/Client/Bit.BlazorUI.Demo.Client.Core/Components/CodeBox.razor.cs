namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class CodeBox
{
    private string? codeIcon;
    private bool isCodeCopied = false;
    private string copyCodeMessage = "Copy code";
    private ElementReference preElementRef = default!;



    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool HideCopyButton { get; set; }
    [Parameter] public string? MaxHeight { get; set; }



    private async Task CopyCodeToClipboard()
    {
        var codeSample = await JSRuntime.GetInnerText(preElementRef);
        await JSRuntime.CopyToClipboard(codeSample.Trim());

        codeIcon = "Accept";
        copyCodeMessage = "Code copied!";
        isCodeCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isCodeCopied = false;
        codeIcon = null;
        copyCodeMessage = "Copy code";

        StateHasChanged();
    }
}
