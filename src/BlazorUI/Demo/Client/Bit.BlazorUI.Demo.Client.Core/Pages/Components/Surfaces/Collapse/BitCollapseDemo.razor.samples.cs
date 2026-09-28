namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Surfaces.Collapse;

public partial class BitCollapseDemo
{
    private readonly string example1RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expanded"" />
<BitCollapse Expanded=""expanded"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
</BitCollapse>";
    private readonly string example1CsharpCode = @"
private bool expanded = true;";

    private readonly string example2RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""boundExpanded"" />
<BitCollapse @bind-Expanded=""boundExpanded"">
    The button and the collapse share one value: <b>@boundExpanded</b>.
</BitCollapse>

<div style=""display:flex;align-items:center;gap:0.5rem"">
    <BitButton OnClick=""() => collapseRef?.ExpandAsync()"">Expand</BitButton>
    <BitButton OnClick=""() => collapseRef?.CollapseAsync()"">Collapse</BitButton>
    <BitButton OnClick=""() => collapseRef?.ToggleAsync()"">Toggle</BitButton>
    <BitCheckbox Label=""Enabled"" @bind-Value=""collapseEnabled"" />
</div>
<BitCollapse @ref=""collapseRef"" DefaultExpanded IsEnabled=""collapseEnabled"" OnChange=""HandleChange"">
    Starts open through DefaultExpanded; nothing on the page holds its state.
</BitCollapse>
<div>@changeLog</div>";
    private readonly string example2CsharpCode = @"
private bool boundExpanded = true;
private string changeLog = string.Empty;
private BitCollapse? collapseRef;
private bool collapseEnabled = true;

private void HandleChange(bool value) => changeLog = $""OnChange({value.ToString().ToLower()})"";";

    private readonly string example3RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""surfaceExpanded"" />

<BitCollapse Expanded=""surfaceExpanded"">Default: padding and the primary background.</BitCollapse>

<BitCollapse Expanded=""surfaceExpanded"" Background=""BitColorKind.Secondary"">Secondary background.</BitCollapse>

<BitCollapse Expanded=""surfaceExpanded"" Background=""BitColorKind.Transparent"" NoPadding>
    <BitMessage Color=""BitColor.Info"">Transparent, no padding: the content carries its own surface and insets.</BitMessage>
</BitCollapse>";
    private readonly string example3CsharpCode = @"
private bool surfaceExpanded = true;";

    private readonly string example4RazorCode = @"
<div style=""display:flex;align-items:center;gap:1rem"">
    <BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""horizontalExpanded"" />
    <BitCollapse Horizontal Expanded=""horizontalExpanded"" Background=""BitColorKind.Secondary"">
        <div style=""white-space:nowrap"">This panel opens sideways.</div>
    </BitCollapse>
</div>";
    private readonly string example4CsharpCode = @"
private bool horizontalExpanded = true;";

    private readonly string example5RazorCode = @"
<BitCollapse Expanded=""peekExpanded"" CollapsedSize=""4.5rem"" Style=""--bit-Collapse-peek-fade: 2rem;"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be
    shaped into meaning, and the emotions ready to resonate with every reader.
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create.
</BitCollapse>
<BitButton Variant=""BitVariant.Text"" OnClick=""() => peekExpanded = !peekExpanded"">
    @(peekExpanded ? ""Show less"" : ""Show more"")
</BitButton>";
    private readonly string example5CsharpCode = @"
private bool peekExpanded;";

    private readonly string example6RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""transitionExpanded"" />

<div>Duration=""1000"" Delay=""200"" and an overshooting Easing</div>
<BitCollapse Expanded=""transitionExpanded"" Duration=""1000"" Delay=""200"" Easing=""cubic-bezier(0.68, -0.55, 0.27, 1.55)"">
    A thousand milliseconds after a two hundred millisecond wait.
</BitCollapse>

<div>ExpandDuration=""900"" CollapseDuration=""200""</div>
<BitCollapse Expanded=""transitionExpanded"" ExpandDuration=""900"" CollapseDuration=""200"">
    Opens slowly, closes quickly.
</BitCollapse>

<div>NoFade</div>
<BitCollapse Expanded=""transitionExpanded"" NoFade Duration=""800"">
    The content never changes its opacity.
</BitCollapse>

<div>NoAnimation</div>
<BitCollapse Expanded=""transitionExpanded"" NoAnimation>
    Appears and disappears at once.
</BitCollapse>";
    private readonly string example6CsharpCode = @"
private bool transitionExpanded = true;";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""() => eventsCollapseRef?.ToggleAsync()"">@(eventsExpanded ? ""Collapse"" : ""Expand"")</BitButton>
<BitCollapse @ref=""eventsCollapseRef""
             @bind-Expanded=""eventsExpanded""
             Duration=""600""
             OnChange=""HandleEventsChange""
             OnExpanding=""HandleEventsExpanding""
             OnCollapsing=""HandleEventsCollapsing""
             OnExpanded=""HandleEventsExpanded""
             OnCollapsed=""HandleEventsCollapsed"">
    The -ing callback lands at once; the -ed one six hundred milliseconds later.
</BitCollapse>

@foreach (var entry in eventsLog)
{
    <div>@entry</div>
}";
    private readonly string example7CsharpCode = @"
private bool eventsExpanded = true;
private BitCollapse? eventsCollapseRef;
private readonly List<string> eventsLog = [];

private void HandleEventsChange(bool value) => LogCollapseEvent($""OnChange({value.ToString().ToLower()})"");
private void HandleEventsExpanding() => LogCollapseEvent(""OnExpanding"");
private void HandleEventsCollapsing() => LogCollapseEvent(""OnCollapsing"");
private void HandleEventsExpanded() => LogCollapseEvent(""OnExpanded"");
private void HandleEventsCollapsed() => LogCollapseEvent(""OnCollapsed"");

private void LogCollapseEvent(string name)
{
    eventsLog.Insert(0, name);

    if (eventsLog.Count > 8)
    {
        eventsLog.RemoveAt(eventsLog.Count - 1);
    }
}";

    private readonly string example8RazorCode = @"
<style>
    .clip-card {
        margin: 0.5rem;
        padding: 0.75rem;
        border-radius: 0.25rem;
        background-color: var(--bit-clr-bg-sec);
        box-shadow: 0 0 0 1rem rgba(0, 120, 212, 0.25);
    }
</style>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""clipExpanded"" />

<BitCollapse Expanded=""clipExpanded"" NoPadding Duration=""600"">
    <div class=""clip-card"">Default: the glow is cut off at the edge.</div>
</BitCollapse>

<BitCollapse Expanded=""clipExpanded"" NoPadding NoClip Duration=""600"">
    <div class=""clip-card"">NoClip: the glow is drawn in full once the section is open.</div>
</BitCollapse>";
    private readonly string example8CsharpCode = @"
private bool clipExpanded = true;";

    private readonly string example9RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""renderingExpanded"" />

<div>LazyRender</div>
<BitCollapse LazyRender Expanded=""renderingExpanded"" OnExpanded=""() => lazyOpenCount++"">
    Built on the first open, kept since. Opened @lazyOpenCount time(s).
</BitCollapse>

<div>UnmountOnCollapse</div>
<BitCollapse UnmountOnCollapse Expanded=""renderingExpanded"">
    <BitTextField Label=""Type something, then close and reopen"" />
</BitCollapse>

<div>ExpandOnPrint</div>
<BitCollapse ExpandOnPrint Expanded=""renderingExpanded"">
    On paper this section is always open.
</BitCollapse>";
    private readonly string example9CsharpCode = @"
private bool renderingExpanded;
private int lazyOpenCount;";

    private readonly string example10RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""findExpanded"" />
<BitCollapse HiddenUntilFound @bind-Expanded=""findExpanded"">
    The passphrase kept in this section is <b>marmalade skies</b>. Close the section, press Ctrl+F and search for it.
</BitCollapse>";
    private readonly string example10CsharpCode = @"
private bool findExpanded = true;";

    private readonly string example11RazorCode = @"
<BitToggleButton Id=""a11y-trigger""
                 Text=""Shipping details""
                 AriaMode=""BitToggleButtonAriaMode.Expanded""
                 AriaControls=""a11y-collapse-content""
                 @bind-IsChecked=""a11yExpanded"" />
<BitCollapse Id=""a11y-collapse"" Expanded=""a11yExpanded"" LabelledBy=""a11y-trigger"">
    Orders placed before 2 pm ship the same day. <BitLink Href=""/components/collapse"">Read the full policy</BitLink>.
</BitCollapse>

<BitButton @ref=""focusTriggerRef"" OnClick=""() => focusCollapseRef?.ExpandAsync()"">Open and focus</BitButton>
<BitCollapse @ref=""focusCollapseRef""
             @bind-Expanded=""focusExpanded""
             AriaLabel=""Focused section""
             OnExpanded=""HandleFocusExpanded""
             OnCollapsing=""HandleFocusCollapsing"">
    FocusAsync put the focus here at the end of the expand transition.
    <BitButton Variant=""BitVariant.Text"" OnClick=""() => focusCollapseRef?.CollapseAsync()"">Close</BitButton>
</BitCollapse>";
    private readonly string example11CsharpCode = @"
private bool a11yExpanded;

private bool focusExpanded;
private BitButton? focusTriggerRef;
private BitCollapse? focusCollapseRef;

private async Task HandleFocusExpanded()
{
    if (focusCollapseRef is not null)
    {
        await focusCollapseRef.FocusAsync();
    }
}

private async Task HandleFocusCollapsing()
{
    if (focusTriggerRef is not null)
    {
        await focusTriggerRef.FocusAsync();
    }
}";

    private readonly string example12RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""cssVarExpanded"" />

<div style=""--bit-Collapse-background: var(--bit-clr-bg-sec);
            --bit-Collapse-padding: 1.5rem;
            --bit-Collapse-duration: 800ms;
            --bit-Collapse-easing: cubic-bezier(0.2, 0, 0, 1);"">
    <BitCollapse Expanded=""cssVarExpanded"">Background, padding, pace and easing from an ancestor.</BitCollapse>
    <BitCollapse Expanded=""cssVarExpanded"" Background=""BitColorKind.Tertiary"">Same ancestor, its own Background.</BitCollapse>
</div>

<BitCollapse Expanded=""cssVarExpanded"" Style=""--bit-Collapse-color: var(--bit-clr-pri); --bit-Collapse-font-size: var(--bit-tpg-fs-md);"">
    Color and font size on one collapse.
</BitCollapse>";
    private readonly string example12CsharpCode = @"
private bool cssVarExpanded = true;";

    private readonly string example13RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""cascadingExpanded"" />

<BitParams Parameters=""collapseParams"">
    <BitCollapse Expanded=""cascadingExpanded"">Secondary background, no padding, a slower pace.</BitCollapse>
    <BitCollapse Expanded=""cascadingExpanded"">The same, from the same BitParams.</BitCollapse>
    <BitCollapse Expanded=""cascadingExpanded"" NoPadding=""false"">Keeps its own padding.</BitCollapse>
</BitParams>";
    private readonly string example13CsharpCode = @"
private bool cascadingExpanded = true;
private readonly BitCollapseParams[] collapseParams =
[
    new()
    {
        Background = BitColorKind.Secondary,
        NoPadding = true,
        Duration = 700,
    }
];";

    private readonly string example14RazorCode = @"
<style>
    .custom-expanded {
        border-radius: 0.5rem;
        box-shadow: var(--bit-shd-2);
    }

    .custom-wrapper {
        font-weight: 600;
        color: var(--bit-clr-sec);
    }
</style>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""styleExpanded"" />

<BitCollapse Expanded=""styleExpanded"" Style=""border-inline-start: 4px solid var(--bit-clr-pri);"">
    A styled root.
</BitCollapse>

<BitCollapse Expanded=""styleExpanded""
             Styles=""@(new() { Expanded = ""border: 1px solid var(--bit-clr-brd-pri);"", Wrapper = ""font-style: italic;"" })"">
    Styles for the expanded root and the wrapper.
</BitCollapse>

<BitCollapse Expanded=""styleExpanded"" Classes=""@(new() { Expanded = ""custom-expanded"", Wrapper = ""custom-wrapper"" })"">
    Classes for the expanded root and the wrapper.
</BitCollapse>";
    private readonly string example14CsharpCode = @"
private bool styleExpanded = true;";

    private readonly string example15RazorCode = @"
<div dir=""rtl"">
    <BitToggleButton OnText=""بستن"" OffText=""باز کردن"" @bind-IsChecked=""rtlExpanded"" />
    <BitCollapse Expanded=""rtlExpanded"" Dir=""BitDir.Rtl"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
        چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
        و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
    </BitCollapse>

    <BitCollapse Horizontal Expanded=""rtlExpanded"" Dir=""BitDir.Rtl"" Background=""BitColorKind.Secondary"">
        <div style=""white-space:nowrap"">این بخش از سمت راست باز می شود.</div>
    </BitCollapse>
</div>";
    private readonly string example15CsharpCode = @"
private bool rtlExpanded = true;";
}
