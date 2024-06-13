using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages;

public partial class CodeBox
{
    private bool isCodeCopied = false;
    private string codeIcon = BitIconName.Copy;
    private string copyCodeMessage = "Copy code";


    [AutoInject] private Clipboard clipboard = default!;


    [Parameter] public string? Code { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }



    private async Task CopyCodeToClipboard()
    {
        await clipboard.WriteText(Code!);

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
