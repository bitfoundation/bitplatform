namespace Bit.BlazorUI.Demo.Client.Core.Components;

public partial class CodeBox
{
    // No icon while idle - the button renders its own copy glyph as an inline svg instead.
    private string? _codeIcon;
    private bool _isCodeCopied = false;
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
        _isCodeCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        _isCodeCopied = false;
        _codeIcon = null;
        _copyCodeMessage = "Copy code";

        StateHasChanged();
    }
}
