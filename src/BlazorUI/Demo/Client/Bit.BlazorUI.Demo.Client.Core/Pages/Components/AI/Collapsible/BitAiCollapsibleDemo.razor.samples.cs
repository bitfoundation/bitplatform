namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.Collapsible;

public partial class BitAiCollapsibleDemo
{
    private readonly string example1RazorCode = @"
<BitAiCollapsible Title=""Searched the documentation"">
    Three pages matched: render modes, the SignalR circuit, and prerendering.
</BitAiCollapsible>

<BitAiCollapsible Title=""Read the error log"" Open>
    The last failure was a timeout on the second attempt, after 30 seconds.
</BitAiCollapsible>";

    private readonly string example2RazorCode = @"
<BitButton OnClick=""() => isOpen = !isOpen"">@(isOpen ? ""Close"" : ""Open"") the panel</BitButton>

<BitAiCollapsible Title=""Planned the answer"" @bind-Open=""isOpen"">
    Compare the two render modes, then give a number for each.
</BitAiCollapsible>

<div>The panel is <b>@(isOpen ? ""open"" : ""closed"")</b>, toggled @toggleCount time(s) by the user.</div>

<BitAiCollapsible Title=""Toggle me"" OpenChanged=""(bool open) => toggleCount++"">
    Each toggle of the header is counted by OpenChanged.
</BitAiCollapsible>";
    private readonly string example2CsharpCode = @"
private bool isOpen;
private int toggleCount;";

    private readonly string example3RazorCode = @"
<BitAiCollapsible>
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Search"" />
        <span>search_docs</span>
    </HeaderTemplate>
    <SuffixTemplate>
        <BitTag Text=""Completed"" Color=""BitColor.Success"" Size=""BitSize.Small"" />
    </SuffixTemplate>
    <ChildContent>
        <BitAiCodeBlockView Language=""yaml"" Code=""@(""query: render modes\nresults: 3"")"" />
    </ChildContent>
</BitAiCollapsible>

<BitAiCollapsible>
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Lightbulb"" />
        <span>Thought for 4 seconds</span>
    </HeaderTemplate>
    <ChildContent>
        The question is about latency, so the fixed cost of the connection and the cost per token are worth separating.
    </ChildContent>
</BitAiCollapsible>";

    private readonly string example4RazorCode = @"
<BitAiCollapsible Title=""A disabled closed panel"" Disabled>
    Never shown.
</BitAiCollapsible>

<BitAiCollapsible Title=""A disabled open panel"" Disabled Open>
    Shown, and kept open.
</BitAiCollapsible>";

    private readonly string example5RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        outline: 2px dashed tomato;
    }

    .custom-root {
        max-width: 28rem;
    }

    .custom-header {
        color: white;
        background-color: rebeccapurple;
    }
</style>

<BitAiCollapsible Title=""Styled panel"" Style=""max-width: 24rem; box-shadow: 0 0 0.5rem dodgerblue;"">
    A panel with a style of its own.
</BitAiCollapsible>

<BitAiCollapsible Title=""Classy panel"" Class=""custom-class"">
    A panel with a class of its own.
</BitAiCollapsible>

<BitAiCollapsible Title=""Styles""
                  Styles=""@(new() { Accordion = new() { Header = ""background-color: tomato; color: white;"",
                                                        Content = ""font-style: italic;"" } })"">
    The header and the content are styled apart.
</BitAiCollapsible>

<BitAiCollapsible Title=""Classes""
                  Classes=""@(new() { Root = ""custom-root"", Accordion = new() { Header = ""custom-header"" } })"">
    The root and the header carry classes of their own.
</BitAiCollapsible>

<div style=""--bit-Accordion-radius: 1rem; --bit-Accordion-border-color: mediumseagreen; --bit-Accordion-title-color: mediumseagreen;"">
    <BitAiCollapsible Title=""Restyled by the --bit-Accordion-* variables of its container"">
        The variables inherit, so one container restyles every panel inside it.
    </BitAiCollapsible>
</div>";

    private readonly string example6RazorCode = @"
<BitAiCollapsible Dir=""BitDir.Rtl"" Title=""جستجو در مستندات"">
    سه صفحه پیدا شد: حالت‌های رندر، اتصال SignalR و پیش‌رندر.
</BitAiCollapsible>

<div dir=""rtl"">
    <BitAiCollapsible Title=""برنامه‌ریزی پاسخ"" Open>
        دو حالت رندر را مقایسه کن و برای هر کدام یک عدد بده.
    </BitAiCollapsible>
</div>";
}
