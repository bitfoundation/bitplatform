namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.CopyButton;

public partial class BitAiCopyButtonDemo
{
    private readonly string example1RazorCode = @"
<div style=""display:flex;align-items:center;gap:0.5rem"">
    <code>dotnet add package Bit.BlazorUI.AI</code>
    <BitAiCopyButton Content=""dotnet add package Bit.BlazorUI.AI"" />
</div>";

    private readonly string example2RazorCode = @"
<BitAiCopyButton Content=""https://blazorui.bitplatform.dev"" Label=""Copy link"" />

<BitAiCopyButton Content=""https://blazorui.bitplatform.dev"" ShowLabel />

<BitAiCopyButton Content=""https://blazorui.bitplatform.dev"" Label=""Copy link"" ShowLabel />";

    private readonly string example3RazorCode = @"
<div style=""display:flex;align-items:center;gap:0.5rem"">
    <BitAiCopyButton Content=""An answer worth keeping.""
                     ShowLabel
                     ConfirmationDuration=""TimeSpan.FromSeconds(5)""
                     OnCopied=""HandleCopied"" />
    <span>Copied @copyCount time(s)</span>
</div>";
    private readonly string example3CsharpCode = @"
private int copyCount;

private void HandleCopied()
{
    copyCount++;
}";

    private readonly string example4RazorCode = @"
<div style=""display:flex;align-items:center;gap:0.5rem"">
    <BitTextField @bind-Value=""draft"" Immediate Placeholder=""Type something to copy"" Style=""flex:1"" />
    <BitAiCopyButton Content=""@draft"" />
</div>";
    private readonly string example4CsharpCode = @"
private string? draft;";

    private readonly string example5RazorCode = @"
<BitParams Parameters=""copyButtonParams"">
    <BitAiCopyButton Content=""First snippet"" />
    <BitAiCopyButton Content=""Second snippet"" />
    <BitAiCopyButton Content=""Third snippet"" ShowLabel=""false"" />
</BitParams>";
    private readonly string example5CsharpCode = @"
private readonly BitAiCopyButtonParams[] copyButtonParams =
[
    new() { ShowLabel = true, ConfirmationDuration = TimeSpan.FromSeconds(4) }
];";

    private readonly string example6RazorCode = @"
<BitAiCopyButton Disabled Content=""Nothing to see here"" />

<BitAiCopyButton Disabled Content=""Nothing to see here"" ShowLabel />";

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        padding: 0.25rem;
        border-radius: 0.5rem;
        background-color: var(--bit-clr-bg-sec);
    }

    .custom-button {
        font-weight: bold;
        border: 2px solid var(--bit-clr-pri);
    }
</style>

<BitAiCopyButton Content=""Styled"" Style=""border:1px dashed var(--bit-clr-brd-pri);border-radius:0.5rem"" />

<BitAiCopyButton Content=""Classy"" Class=""custom-class"" />

<BitAiCopyButton Content=""Styles"" ShowLabel Styles=""@(new() { Button = new() { Root = ""border-radius:2rem"", Icon = ""color:var(--bit-clr-pri)"" } })"" />

<BitAiCopyButton Content=""Classes"" ShowLabel Classes=""@(new() { Button = new() { Root = ""custom-button"" } })"" />

<span style=""@cssVariablesStyle"">
    <BitAiCopyButton Content=""Variables"" ShowLabel />
</span>";
    private readonly string example7CsharpCode = @"
private const string cssVariablesStyle = ""--bit-Button-text-color:var(--bit-clr-pri);"" +
                                         ""--bit-Button-text-hover-background:var(--bit-clr-pri-light);"" +
                                         ""--bit-Button-radius:2rem;"";";

    private readonly string example8RazorCode = @"
<BitAiCopyButton Dir=""BitDir.Rtl"" Content=""سلام دنیا"" Label=""کپی"" ShowLabel />

<div dir=""rtl"">
    <BitAiCopyButton Content=""سلام دنیا"" Label=""کپی متن"" ShowLabel />
</div>";
}
