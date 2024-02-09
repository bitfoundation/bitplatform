using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil05ClipboardPage
{
    private string clipboardText = string.Empty;
    private string newClipboardText = string.Empty;
    private ClipboardItem[] clipboardItems = [];
    private string newText = string.Empty;

    private async Task ReadText()
    {
        clipboardText = await clipboard.ReadText();
    }

    private async Task WriteText()
    {
        await clipboard.WriteText(newClipboardText);
    }

    private async Task Read()
    {
        clipboardItems = await clipboard.Read();
    }

    private async Task Write()
    {
        var data = System.Text.Encoding.UTF8.GetBytes(newText);
        var item = new ClipboardItem() { MimeType = "text/plain", Data = data };
        await clipboard.Write([item]);
    }

    private string readTextExampleCode =
@"@inject Bit.Butil.Clipboard clipboard

<BitButton OnClick=""ReadText"">ReadText</BitButton>

<div>Clipboard text: @clipboardText</div>

@code {
    private string clipboardText = string.Empty;

    private async Task ReadText()
    {
        clipboardText = await clipboard.ReadText();
    }
}";

    private string writeTextExampleCode =
@"@inject Bit.Butil.Clipboard clipboard

<BitTextField @bind-Value=""newClipboardText"" />

<BitButton OnClick=""WriteText"">WriteText</BitButton>

@code {
    private string newClipboardText = string.Empty;

    private async Task WriteText()
    {
        await clipboard.WriteText(newClipboardText);
    }
}";

    private string readExampleCode =
@"@inject Bit.Butil.Clipboard clipboard

<BitButton OnClick=""Read"">Read</BitButton>

@foreach (var item in clipboardItems)
{
    <div>Clipboard MimeType: ""@item.MimeType""</div>
    <div>Clipboard Data: ""@System.Text.Encoding.UTF8.GetString(item.Data)""</div>
}

@code {
    private ClipboardItem[] clipboardItems = [];

    private async Task Read()
    {
        clipboardItems = await clipboard.Read();
    }
}";

    private string writeExampleCode =
@"@inject Bit.Butil.Clipboard clipboard

<BitTextField @bind-Value=""newText"" />

<BitButton OnClick=""Write"">Write</BitButton>

@code {
    private string newText = string.Empty;

    private async Task Write()
    {
        var data = System.Text.Encoding.UTF8.GetBytes(newText);
        var item = new ClipboardItem() { MimeType = ""text/plain"", Data = data };
        await clipboard.Write([item]);
    }
}";
}
