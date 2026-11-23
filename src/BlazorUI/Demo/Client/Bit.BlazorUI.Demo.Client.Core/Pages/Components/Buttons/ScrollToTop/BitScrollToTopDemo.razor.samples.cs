namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ScrollToTop;

public partial class BitScrollToTopDemo
{
    private readonly string example1RazorCode = @"
<BitScrollToTop />

<BitScrollToTop Text=""Back to top"" />";

    private readonly string example2RazorCode = @"
<BitScrollToTop IconName=""@BitIconName.DoubleChevronUp"" AriaLabel=""Back to top"" />

<BitScrollToTop Text=""Back to top"" IconName=""@BitIconName.Up"" />

<BitScrollToTop Text=""Top of the page"" IconName=""@BitIconName.ChevronUpMed"" />";

    private readonly string example3RazorCode = @"
<BitScrollToTop Float RevealOffset=""600"" FloatOffset=""1.5rem"" />";

    private readonly string example4RazorCode = @"
<style>
    .pane {
        position: relative;
        border: 1px solid var(--bit-clr-brd-sec);
    }

    .pane-content {
        height: 15rem;
        overflow: auto;
        padding: 0.5rem;
    }
</style>

<div class=""pane"">
    <div id=""scroll-to-top-pane"" class=""pane-content"">
        @for (var i = 1; i <= 30; i++)
        {
            <div>Line @i of a pane long enough to scroll.</div>
        }
    </div>
    <BitScrollToTop FloatAbsolute RevealOffset=""100"" ScrollTarget=""#scroll-to-top-pane"" />
</div>";

    private readonly string example5RazorCode = @"
<style>
    .pane {
        position: relative;
        border: 1px solid var(--bit-clr-brd-sec);
    }

    .pane-content {
        height: 15rem;
        overflow: auto;
        padding: 0.5rem;
    }
</style>

<div class=""pane"">
    <div id=""scroll-to-top-jump"" class=""pane-content"">
        @for (var i = 1; i <= 30; i++)
        {
            <div>Line @i of a pane long enough to scroll.</div>
        }
    </div>
    <BitScrollToTop FloatAbsolute Smooth=""false"" ScrollTarget=""#scroll-to-top-jump"" />
</div>";

    private readonly string example6RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitScrollToTop Icon=""@BitIconInfo.Fa(""solid arrow-up"")"" />

<BitScrollToTop Icon=""@BitIconInfo.Bi(""arrow-up-circle"")"" Text=""Back to top"" />";

    private readonly string example7RazorCode = @"
<style>
    .custom-root {
        border-radius: 0.25rem;
        box-shadow: var(--bit-shd-card);
    }

    .custom-icon {
        color: var(--bit-clr-sec);
    }
</style>

<BitScrollToTop Text=""Back to top""
                Style=""--bit-ScrollToTop-background: var(--bit-clr-bg-sec); --bit-ScrollToTop-color: var(--bit-clr-fg-pri); --bit-ScrollToTop-border-color: var(--bit-clr-brd-pri); --bit-ScrollToTop-hover-background: var(--bit-clr-bg-sec-hover);"" />

<BitScrollToTop Style=""--bit-ScrollToTop-radius: 0.25rem; --bit-ScrollToTop-size: 2rem;"" />

<BitScrollToTop Text=""Back to top"" Classes=""@(new() { Root = ""custom-root"", Icon = ""custom-icon"" })"" IconName=""@BitIconName.Up"" />

<BitScrollToTop Text=""Back to top"" Styles=""@(new() { Text = ""font-style: italic;"" })"" />";

    private readonly string example8RazorCode = @"
<div dir=""rtl"">
    <BitScrollToTop Dir=""BitDir.Rtl"" Text=""بازگشت به بالا"" IconName=""@BitIconName.Up"" />
</div>";
}
