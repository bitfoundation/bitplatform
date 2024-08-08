using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages;

public partial class CodeBox
{
    private bool isCodeCopied = false;
    private string codeIcon = BitIconName.Copy;
    private string copyCodeMessage = "Copy code";
    private ElementReference preElementRefrence = default!;


    [AutoInject] private Clipboard clipboard = default!;


    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter] public bool HideCopyButton { get; set; }
    [Parameter] public string? MaxHeight { get; set; }



    private async Task CopyCodeToClipboard()
    {
        var codeSample = await preElementRefrence.GetInnerText();
        await clipboard.WriteText(codeSample.Trim());

        codeIcon = BitIconName.CheckMark;
        copyCodeMessage = "Code copied!";
        isCodeCopied = true;

        StateHasChanged();

        await Task.Delay(1000);
        isCodeCopied = false;
        codeIcon = BitIconName.Copy;
        copyCodeMessage = "Copy code";

        StateHasChanged();
    }
}
