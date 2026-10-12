using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages;

public partial class CodeBox
{
    private string? codeIcon;
    private bool isCodeCopied = false;
    private string copyCodeMessage = "Copy code";
    private ElementReference preElementRef = default!;


    [AutoInject] private Clipboard clipboard = default!;


    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool HideCopyButton { get; set; }
    [Parameter] public string? MaxHeight { get; set; }
    [Parameter] public bool CopyOnClick { get; set; }
    [Parameter] public EventCallback OnCopy { get; set; }



    private async Task CopyContentOnClick()
    {
        if (CopyOnClick is false) return;

        await CopyCodeToClipboard();
    }

    private async Task CopyCodeToClipboard()
    {
        var codeSample = await preElementRef.GetInnerText();
        await clipboard.WriteText(codeSample.Trim());

        codeIcon = "Accept";
        copyCodeMessage = "Copied!";
        isCodeCopied = true;

        StateHasChanged();

        await OnCopy.InvokeAsync();

        await Task.Delay(1000);
        isCodeCopied = false;
        codeIcon = null;
        copyCodeMessage = "Copy code";

        StateHasChanged();
    }
}
