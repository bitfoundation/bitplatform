namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Tooltip;

public partial class BitTooltipDemo
{
    private readonly string example1RazorCode = @"
<BitTooltip Text=""This is the tooltip text"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

<BitTooltip Text=""This tooltip never shows"" IsEnabled=""false"">
    <BitButton Variant=""BitVariant.Outline"">Disabled tooltip</BitButton>
</BitTooltip>

<BitTooltip Text=""Sign in first to save anything"">
    <BitButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Disabled anchor</BitButton>
</BitTooltip>

<BitTooltip Text=""Shown to begin with"" DefaultIsShown=""true"">
    <BitButton Variant=""BitVariant.Outline"">DefaultIsShown</BitButton>
</BitTooltip>";

    private readonly string example2RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Text=""Top"" Position=""BitTooltipPosition.Top"">
    <BitButton Variant=""BitVariant.Outline"">Top</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""TopLeft"" Position=""BitTooltipPosition.TopLeft"">
    <BitButton Variant=""BitVariant.Outline"">TopLeft</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""TopRight"" Position=""BitTooltipPosition.TopRight"">
    <BitButton Variant=""BitVariant.Outline"">TopRight</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""RightTop"" Position=""BitTooltipPosition.RightTop"">
    <BitButton Variant=""BitVariant.Outline"">RightTop</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Right"" Position=""BitTooltipPosition.Right"">
    <BitButton Variant=""BitVariant.Outline"">Right</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""RightBottom"" Position=""BitTooltipPosition.RightBottom"">
    <BitButton Variant=""BitVariant.Outline"">RightBottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""BottomRight"" Position=""BitTooltipPosition.BottomRight"">
    <BitButton Variant=""BitVariant.Outline"">BottomRight</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Bottom"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Bottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""BottomLeft"" Position=""BitTooltipPosition.BottomLeft"">
    <BitButton Variant=""BitVariant.Outline"">BottomLeft</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""LeftBottom"" Position=""BitTooltipPosition.LeftBottom"">
    <BitButton Variant=""BitVariant.Outline"">LeftBottom</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""Left"" Position=""BitTooltipPosition.Left"">
    <BitButton Variant=""BitVariant.Outline"">Left</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Text=""LeftTop"" Position=""BitTooltipPosition.LeftTop"">
    <BitButton Variant=""BitVariant.Outline"">LeftTop</BitButton>
</BitTooltip>";

    private readonly string example3RazorCode = @"
<BitTooltip Text=""Shown by the pointer only"" ShowOnFocus=""false"">
    <BitButton Variant=""BitVariant.Outline"">Hover</BitButton>
</BitTooltip>

<BitTooltip Text=""Shown by the keyboard only"" ShowOnHover=""false"">
    <BitButton Variant=""BitVariant.Outline"">Focus (tab to me)</BitButton>
</BitTooltip>

<BitTooltip Text=""Toggled by a click"" ShowOnClick ShowOnHover=""false"" ShowOnFocus=""false"">
    <BitButton Variant=""BitVariant.Outline"">Click</BitButton>
</BitTooltip>

<BitTooltip Text=""A click takes me away"" HideOnClick>
    <BitButton Variant=""BitVariant.Outline"">HideOnClick</BitButton>
</BitTooltip>

<BitTooltip Text=""Press and hold me on a touch screen"" TouchShowDelay=""700"">
    <BitButton Variant=""BitVariant.Outline"">TouchShowDelay</BitButton>
</BitTooltip>

<BitTooltip Text=""A tap leaves me out of it"" NoTouch>
    <BitButton Variant=""BitVariant.Outline"">NoTouch</BitButton>
</BitTooltip>";

    private readonly string example4RazorCode = @"
<BitTooltip Text=""Waited 700ms for you"" ShowDelay=""700"">
    <BitButton Variant=""BitVariant.Outline"">ShowDelay</BitButton>
</BitTooltip>

<BitTooltip Text=""Staying for a second"" HideDelay=""1000"">
    <BitButton Variant=""BitVariant.Outline"">HideDelay</BitButton>
</BitTooltip>


<BitTooltipGroup ShowDelay=""700"" HideDelay=""100"" SkipDelay=""600"">
    <BitTooltip Text=""Waited for, like the first of a row"">
        <BitButton Variant=""BitVariant.Outline"">Bold</BitButton>
    </BitTooltip>

    <BitTooltip Text=""Shown at once, while the last one is fresh"">
        <BitButton Variant=""BitVariant.Outline"">Italic</BitButton>
    </BitTooltip>

    <BitTooltip Text=""And so is this one"">
        <BitButton Variant=""BitVariant.Outline"">Underline</BitButton>
    </BitTooltip>

    <BitTooltip Text=""I keep the delay I was given"" ShowDelay=""0"">
        <BitButton Variant=""BitVariant.Outline"">No delay of my own</BitButton>
    </BitTooltip>
</BitTooltipGroup>";

    private readonly string example5RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Text=""Default"">
    <BitButton Variant=""BitVariant.Outline"">Default</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""No arrow"" HideArrow>
    <BitButton Variant=""BitVariant.Outline"">HideArrow</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""A bigger arrow"" ArrowSize=""18"">
    <BitButton Variant=""BitVariant.Outline"">ArrowSize</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""Held further off"" Offset=""24"">
    <BitButton Variant=""BitVariant.Outline"">Offset</BitButton>
</BitTooltip>


<BitTooltip DefaultIsShown=""true"" Position=""BitTooltipPosition.Bottom"" MaxWidth=""10rem""
            Text=""A narrow tooltip wraps its text sooner."">
    <BitButton Variant=""BitVariant.Outline"">MaxWidth=""10rem""</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Position=""BitTooltipPosition.Bottom""
            Text=""The default cap keeps a long line from running on across the whole screen."">
    <BitButton Variant=""BitVariant.Outline"">Default max width</BitButton>
</BitTooltip>


<div style=""width: 20rem;"">
    <BitTooltip FullWidth Text=""The field keeps the width it was given"">
        <BitTextField Label=""With FullWidth"" Placeholder=""Hover over me"" />
    </BitTooltip>
    <BitTooltip Text=""The field is shrunk to what it holds"">
        <BitTextField Label=""Without FullWidth"" Placeholder=""Hover over me"" />
    </BitTooltip>
</div>";

    private readonly string example6RazorCode = @"
<BitTooltip Position=""BitTooltipPosition.Bottom"" Text=""Move onto me and I will stay. Select this text."">
    <BitButton Variant=""BitVariant.Outline"">Interactive (default)</BitButton>
</BitTooltip>

<BitTooltip Interactive=""false"" Position=""BitTooltipPosition.Bottom"" Text=""Move onto me and I am gone."">
    <BitButton Variant=""BitVariant.Outline"">Interactive=""false""</BitButton>
</BitTooltip>";

    private readonly string example7RazorCode = @"
<BitTooltip Position=""BitTooltipPosition.Bottom"">
    <Template>
        <ul style=""padding: 0.5rem; margin: 0;"">
            <li>1. One</li>
            <li>2. Two</li>
        </ul>
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Template</BitButton>
    </Anchor>
</BitTooltip>

<BitTooltip LazyRender Position=""BitTooltipPosition.Bottom"">
    <Template>
        <TooltipRenderStamp />
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">LazyRender</BitButton>
    </Anchor>
</BitTooltip>

<BitTooltip Position=""BitTooltipPosition.Bottom"">
    <Template>
        <TooltipRenderStamp />
    </Template>
    <Anchor>
        <BitButton Variant=""BitVariant.Outline"">Rendered up front</BitButton>
    </Anchor>
</BitTooltip>


@* TooltipRenderStamp.razor - the stamp is taken once, when the content is first rendered. *@
<div>Rendered at @renderedAt.ToString(""HH:mm:ss"")</div>

@code {
    private DateTime renderedAt;

    protected override void OnInitialized()
    {
        renderedAt = DateTime.Now;
    }
}";

    private readonly string example8RazorCode = @"
<BitTooltip Text=""Save the current document"" Relationship=""BitTooltipRelationship.Label"">
    <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Save"" />
</BitTooltip>

<BitTooltip Text=""Everything you have written since the last save"">
    <BitButton Variant=""BitVariant.Outline"">Save</BitButton>
</BitTooltip>

<BitTooltip Id=""discard-tip"" Text=""Discard"" Relationship=""BitTooltipRelationship.None"">
    <button aria-describedby=""discard-tip-ttp"">Discard</button>
</BitTooltip>

<BitTooltip Text=""Escape leaves me alone"" NoDismissOnEscape>
    <BitButton Variant=""BitVariant.Outline"">NoDismissOnEscape</BitButton>
</BitTooltip>";

    private readonly string example9RazorCode = @"
<BitToggle @bind-Value=""isShown"" Label=""IsShown"" />

<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Show())"">Show</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Hide())"">Hide</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""@(() => tooltipRef?.Toggle())"">Toggle</BitButton>

<BitTooltip @bind-IsShown=""isShown"" Text=""Bound to the toggle"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">@bind-IsShown</BitButton>
</BitTooltip>

<BitTooltip @ref=""tooltipRef"" Text=""Driven from the buttons above"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Methods</BitButton>
</BitTooltip>";
    private readonly string example9CsharpCode = @"
private bool isShown;
private BitTooltip? tooltipRef;";

    private readonly string example10RazorCode = @"
<BitTooltip Text=""Watch the log below""
            OnShow=""@(() => events.Insert(0, $""OnShow at {DateTime.Now:HH:mm:ss}""))""
            OnHide=""@(() => events.Insert(0, $""OnHide at {DateTime.Now:HH:mm:ss}""))""
            OnToggle=""@(v => events.Insert(0, $""OnToggle({v}) at {DateTime.Now:HH:mm:ss}""))"">
    <BitButton Variant=""BitVariant.Outline"">Hover over me</BitButton>
</BitTooltip>

@foreach (var item in events.Take(6))
{
    <div>@item</div>
}";
    private readonly string example10CsharpCode = @"
private readonly List<string> events = [];";

    private readonly string example11RazorCode = @"
<BitParams Parameters=""tooltipParams"">
    <BitTooltip Text=""Bold"">
        <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Bold"" />
    </BitTooltip>

    <BitTooltip Text=""Italic"">
        <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Italic"" />
    </BitTooltip>

    <BitTooltip Text=""Underline, in my own color"" Color=""BitColor.Secondary"">
        <BitButton Variant=""BitVariant.Outline"" IconName=""@BitIconName.Underline"" />
    </BitTooltip>
</BitParams>";
    private readonly string example11CsharpCode = @"
private readonly BitTooltipParams[] tooltipParams =
[
    new()
    {
        Relationship = BitTooltipRelationship.Label,
        Position = BitTooltipPosition.Bottom,
        Color = BitColor.PrimaryForeground,
        ShowDelay = 400,
    }
];";

    private readonly string example12RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Primary"" Text=""Primary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Primary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Secondary"" Text=""Secondary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Secondary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Tertiary"" Text=""Tertiary"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Tertiary</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Info"" Text=""Info"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Info</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Success"" Text=""Success"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Success</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Warning"" Text=""Warning"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Warning</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SevereWarning"" Text=""SevereWarning"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SevereWarning</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.Error"" Text=""Error"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Error</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryBackground"" Text=""PrimaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryBackground"" Text=""SecondaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryBackground"" Text=""TertiaryBackground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryBackground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryForeground"" Text=""PrimaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryForeground"" Text=""SecondaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryForeground"" Text=""TertiaryForeground"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryForeground</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.PrimaryBorder"" Text=""PrimaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">PrimaryBorder</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.SecondaryBorder"" Text=""SecondaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">SecondaryBorder</BitButton>
</BitTooltip>
<BitTooltip DefaultIsShown=""true"" Color=""BitColor.TertiaryBorder"" Text=""TertiaryBorder"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">TertiaryBorder</BitButton>
</BitTooltip>";

    private readonly string example13RazorCode = @"
<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Small"" Text=""Small"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Small</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Medium"" Text=""Medium"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Medium</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Size=""BitSize.Large"" Text=""Large"" Position=""BitTooltipPosition.Bottom"">
    <BitButton Variant=""BitVariant.Outline"">Large</BitButton>
</BitTooltip>";

    private readonly string example14RazorCode = @"
<style>
    .custom-tooltip {
        color: tomato;
        border: solid tomato;
        border-radius: 0.5rem;
    }

    .custom-arrow {
        border-right: solid tomato;
        border-bottom: solid tomato;
    }
</style>


<BitTooltip DefaultIsShown=""true"" Text=""Styles"" Styles=""@(new() { Tooltip = ""box-shadow: aqua 0 0 0.5rem;"" })"">
    <BitButton Variant=""BitVariant.Outline"">Styles</BitButton>
</BitTooltip>

<BitTooltip DefaultIsShown=""true"" Text=""Classes"" Classes=""@(new() { Tooltip = ""custom-tooltip"", Arrow = ""custom-arrow"" })"">
    <BitButton Variant=""BitVariant.Outline"">Classes</BitButton>
</BitTooltip>

<BitTooltip Text=""No fade in or out"" NoAnimation>
    <BitButton Variant=""BitVariant.Outline"">NoAnimation</BitButton>
</BitTooltip>

<BitTooltip Text=""Lifted over what is around me"" ZIndex=""9999"">
    <BitButton Variant=""BitVariant.Outline"">ZIndex</BitButton>
</BitTooltip>


<div style=""--bit-Tooltip-background: var(--bit-clr-fg-pri);
            --bit-Tooltip-color: var(--bit-clr-bg-pri);
            --bit-Tooltip-radius: 999px;
            --bit-Tooltip-padding: 0.25rem 0.75rem;
            --bit-Tooltip-offset: 12px;"">
    <BitTooltip DefaultIsShown=""true"" Text=""Inverted pill"">
        <BitButton Variant=""BitVariant.Outline"">From the ancestor</BitButton>
    </BitTooltip>

    <BitTooltip DefaultIsShown=""true"" Text=""My Color wins"" Color=""BitColor.Success"">
        <BitButton Variant=""BitVariant.Outline"">Color parameter</BitButton>
    </BitTooltip>

    <BitTooltip DefaultIsShown=""true"" Text=""A tooltip with a longer text, set on its own Style""
                Style=""--bit-Tooltip-max-width: 9rem; --bit-Tooltip-font-size: 0.8125rem; --bit-Tooltip-text-align: center;"">
        <BitButton Variant=""BitVariant.Outline"">Own Style</BitButton>
    </BitTooltip>
</div>";

    private readonly string example15RazorCode = @"
<BitTooltip Dir=""BitDir.Rtl"" Text=""یک راهنمای راست‌به‌چپ"">
    <BitButton Variant=""BitVariant.Outline"">نشانگر ماوس را روی من بیاورید</BitButton>
</BitTooltip>

<BitTooltip Dir=""BitDir.Rtl"" DefaultIsShown=""true"" Position=""BitTooltipPosition.Left"" Text=""سمت چپ لنگر"">
    <BitButton Variant=""BitVariant.Outline"">Left</BitButton>
</BitTooltip>

<BitTooltip Dir=""BitDir.Rtl"" DefaultIsShown=""true"" Position=""BitTooltipPosition.Left"" MirrorInRtl
            Text=""آینه‌شده به سمت راست"">
    <BitButton Variant=""BitVariant.Outline"">Left + MirrorInRtl</BitButton>
</BitTooltip>";
}
