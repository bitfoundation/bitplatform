namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class CodeBox
{
    private string _codeIcon = BitIconName.Copy;
    private string _copyCodeMessage = "Copy code";
    private ElementReference _preElementRef = default!;



    [Parameter] public RenderFragment? ChildContent { get; set; }

    [Parameter] public bool HideCopyButton { get; set; }

    [Parameter] public string? Language { get; set; }

    [Parameter] public string? MaxHeight { get; set; }



    private async Task CopyCodeToClipboard()
    {
        var codeSample = await JSRuntime.GetInnerText(_preElementRef);
        await JSRuntime.CopyToClipboard(codeSample.Trim());

        _codeIcon = BitIconName.CheckMark;
        _copyCodeMessage = "Code copied!";

        StateHasChanged();

        await Task.Delay(1000);
        _codeIcon = BitIconName.Copy;
        _copyCodeMessage = "Copy code";

        StateHasChanged();
    }
}
