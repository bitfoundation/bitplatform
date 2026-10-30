namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ScrollToLatest;

public partial class BitAiScrollToLatestDemo
{
    private readonly string example1RazorCode = @"
<BitToggle @bind-Value=""isVisible"" Text=""Visible"" />

<div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri);text-align:center"">
    <BitAiScrollToLatest Visible=""isVisible"" OnClick=""HandleClick"" />
    The positioned box it floats above, such as the footer of a chat.
</div>

<div>Pressed <b>@clicks</b> times.</div>";
    private readonly string example1CsharpCode = @"
private bool isVisible = true;
private int clicks;

private void HandleClick()
{
    clicks++;
}";

    private readonly string example2RazorCode = @"
<div style=""height:400px;display:flex;flex-direction:column"">
    <BitAiMessageList @ref=""list"" Conversation=""conversation"" OnPinnedChange=""HandlePinnedChange"" />
    <div style=""position:relative;padding:0.75rem;text-align:center;color:var(--bit-clr-fg-ter)"">
        <BitAiScrollToLatest Visible=""@(isPinned is false)"" OnClick=""ScrollToLatest"" />
        The footer of the transcript.
    </div>
</div>";
    private readonly string example2CsharpCode = @"
private BitAiMessageList? list;
private bool isPinned = true;

// A conversation long enough to scroll.
private readonly BitAiConversation conversation = AiDemoConversations.Long(20);

private void HandlePinnedChange(bool pinned)
{
    isPinned = pinned;
}

private async Task ScrollToLatest()
{
    if (list is not null) await list.ScrollToLatestAsync();
}";

    private readonly string example3RazorCode = @"
<div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible Label=""3 new messages"" />
</div>";

    private readonly string example4RazorCode = @"
<div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible Disabled />
</div>";

    private readonly string example5RazorCode = @"
<BitParams Parameters=""scrollParams"">
    <div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
        <BitAiScrollToLatest Visible />
    </div>
    <div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
        <BitAiScrollToLatest Visible Label=""Its own label wins"" />
    </div>
</BitParams>";
    private readonly string example5CsharpCode = @"
private readonly BitAiScrollToLatestParams[] scrollParams = [new() { Label = ""Back to the newest message"" }];";

    private readonly string example6RazorCode = @"
<style>
    .custom-scroll {
        box-shadow: none;
    }

    .custom-scroll-button {
        border-radius: 0.25rem;
        border: 1px solid var(--bit-clr-pri);
        background-color: var(--bit-clr-bg-pri);
        color: var(--bit-clr-pri);
    }
</style>

<div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible
                         Style=""box-shadow:none""
                         Styles=""@(new() { Button = new() { Root = ""background:linear-gradient(90deg, #7c3aed, #2563eb);color:#fff;border:none"" } })"" />
</div>

<div style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible
                         Class=""custom-scroll""
                         Classes=""@(new() { Button = new() { Root = ""custom-scroll-button"" } })"" />
</div>

<div style=""position:relative;margin-top:5rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible
                         Style=""--bit-AiScrollToLatest-offset:1.5rem;--bit-AiScrollToLatest-shadow:0 0 0 0.25rem var(--bit-clr-pri-tint);--bit-AiScrollToLatest-z-index:10"" />
</div>";

    private readonly string example7RazorCode = @"
<div dir=""rtl"" style=""position:relative;margin-top:4rem;padding:1rem;border:1px dashed var(--bit-clr-brd-pri)"">
    <BitAiScrollToLatest Visible Dir=""BitDir.Rtl"" Label=""رفتن به آخرین پیام"" />
</div>";
}
