namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CopyButton;

public partial class BitCopyButtonDemo
{
    private readonly string example1RazorCode = @"
<code>dotnet add package Bit.BlazorUI.Extras</code>
<BitCopyButton Value=""dotnet add package Bit.BlazorUI.Extras"" />";

    private readonly string example2RazorCode = @"
<BitCopyButton Value=""https://blazorui.bitplatform.dev"" Text=""Copy"" />
<BitCopyButton Value=""https://blazorui.bitplatform.dev"" Text=""Copy link"" CopiedText=""Link copied!"" />
<BitCopyButton Value=""https://blazorui.bitplatform.dev"" AriaLabel=""Copy link"" CopiedText=""Link copied!"" />";

    private readonly string example3RazorCode = @"
<BitCopyButton Value=""https://blazorui.bitplatform.dev"" IconName=""@BitIconName.Link"" Text=""Copy link"" />
<BitCopyButton Value=""https://blazorui.bitplatform.dev""
               IconName=""@BitIconName.Share""
               CopiedIconName=""@BitIconName.CompletedSolid""
               Text=""Share""
               CopiedText=""Ready to paste"" />";

    private readonly string example4RazorCode = @"
<BitCopyButton Value=""Fill"" Text=""Fill"" Variant=""BitVariant.Fill"" />
<BitCopyButton Value=""Outline"" Text=""Outline"" Variant=""BitVariant.Outline"" />
<BitCopyButton Value=""Text"" Text=""Text"" Variant=""BitVariant.Text"" />
<BitCopyButton Value=""Fill"" Variant=""BitVariant.Fill"" />
<BitCopyButton Value=""Outline"" Variant=""BitVariant.Outline"" />
<BitCopyButton Value=""Text"" Variant=""BitVariant.Text"" />";

    private readonly string example5RazorCode = @"
<BitCopyButton Value=""An answer worth keeping.""
               Text=""Copy""
               CopiedDuration=""TimeSpan.FromSeconds(5)""
               OnCopy=""HandleCopy""
               OnCopyFailed=""HandleCopyFailed"" />
<span>Copied @copyCount time(s)@(copyError is null ? null : $"", last error: {copyError}"")</span>";
    private readonly string example5CsharpCode = @"
private int copyCount;
private string? copyError;

private void HandleCopy(string value)
{
    copyCount++;
    copyError = null;
}

private void HandleCopyFailed(Exception exception)
{
    copyError = exception.Message;
}";

    private readonly string example6RazorCode = @"
<BitTextField @bind-Value=""draft"" Immediate Placeholder=""Type something to copy"" Style=""flex:1"" />
<BitCopyButton Value=""@draft"" />";
    private readonly string example6CsharpCode = @"
private string? draft;";

    private readonly string example7RazorCode = @"
<BitCopyButton Disabled Value=""Nothing to see here"" />
<BitCopyButton Disabled Value=""Nothing to see here"" Text=""Copy"" />";

    private readonly string example8RazorCode = @"
<BitParams Parameters=""copyButtonParams"">
    <BitCopyButton Value=""First snippet"" />
    <BitCopyButton Value=""Second snippet"" />
    <BitCopyButton Value=""Third snippet"" Variant=""BitVariant.Fill"" />
</BitParams>";
    private readonly string example8CsharpCode = @"
private readonly BitCopyButtonParams[] copyButtonParams =
[
    new() { Variant = BitVariant.Outline, Text = ""Copy"", CopiedText = ""Done"", CopiedDuration = TimeSpan.FromSeconds(4) }
];";

    private readonly string example9RazorCode = @"
<BitCopyButton Value=""Primary"" Text=""Primary"" Color=""BitColor.Primary"" />
<BitCopyButton Value=""Secondary"" Text=""Secondary"" Color=""BitColor.Secondary"" />
<BitCopyButton Value=""Tertiary"" Text=""Tertiary"" Color=""BitColor.Tertiary"" />
<BitCopyButton Value=""Info"" Text=""Info"" Color=""BitColor.Info"" />
<BitCopyButton Value=""Success"" Text=""Success"" Color=""BitColor.Success"" />
<BitCopyButton Value=""Warning"" Text=""Warning"" Color=""BitColor.Warning"" />
<BitCopyButton Value=""SevereWarning"" Text=""SevereWarning"" Color=""BitColor.SevereWarning"" />
<BitCopyButton Value=""Error"" Text=""Error"" Color=""BitColor.Error"" />";

    private readonly string example10RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitCopyButton Value=""FontAwesome""
               Text=""FontAwesome""
               Icon=""@BitIconInfo.Fa(""regular copy"")""
               CopiedIcon=""@BitIconInfo.Fa(""solid check"")"" />

<BitCopyButton Value=""Bootstrap Icons""
               Text=""Bootstrap Icons""
               Icon=""@BitIconInfo.Bi(""clipboard"")""
               CopiedIcon=""@BitIconInfo.Bi(""clipboard-check"")"" />";

    private readonly string example11RazorCode = @"
<BitCopyButton Value=""Small"" Text=""Small"" Size=""BitSize.Small"" />
<BitCopyButton Value=""Medium"" Text=""Medium"" Size=""BitSize.Medium"" />
<BitCopyButton Value=""Large"" Text=""Large"" Size=""BitSize.Large"" />
<BitCopyButton Value=""Small"" Size=""BitSize.Small"" />
<BitCopyButton Value=""Medium"" Size=""BitSize.Medium"" />
<BitCopyButton Value=""Large"" Size=""BitSize.Large"" />";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-button {
        font-style: italic;
        box-shadow: var(--bit-shd-1);
    }
</style>

<BitCopyButton Value=""Styled"" Style=""border:1px dashed var(--bit-clr-brd-pri);border-radius:0.5rem;padding:0.25rem"" />
<BitCopyButton Value=""Classy"" Class=""custom-class"" />
<BitCopyButton Value=""Styles"" Text=""Styles"" Styles=""@(new() { Button = new() { Root = ""border-radius:2rem"", Icon = ""color:var(--bit-clr-sec)"" } })"" />
<BitCopyButton Value=""Classes"" Text=""Classes"" Classes=""@(new() { Button = new() { Root = ""custom-button"" } })"" />
<span style=""@cssVariablesStyle"">
    <BitCopyButton Value=""Variables"" Text=""Variables"" />
</span>";
    private readonly string example12CsharpCode = @"
private const string cssVariablesStyle = ""--bit-Button-fill-background:var(--bit-clr-ter);"" +
                                         ""--bit-Button-fill-border-color:var(--bit-clr-ter);"" +
                                         ""--bit-Button-radius:2rem;"";";

    private readonly string example13RazorCode = @"
<div dir=""rtl"">
    <BitCopyButton Dir=""BitDir.Rtl"" Value=""سلام دنیا"" Text=""کپی"" CopiedText=""کپی شد"" />
    <BitCopyButton Dir=""BitDir.Rtl"" Value=""سلام دنیا"" AriaLabel=""کپی"" CopiedText=""کپی شد"" />
</div>";
}
