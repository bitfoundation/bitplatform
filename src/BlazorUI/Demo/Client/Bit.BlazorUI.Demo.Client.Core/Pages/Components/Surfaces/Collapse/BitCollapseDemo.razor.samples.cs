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
    <br />
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an
    idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey
    begins here, in this quiet moment where everything is possible.
</BitCollapse>";
    private readonly string example1CsharpCode = @"
private bool expanded = true;";

    private readonly string example2RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""boundExpanded"" />
<BitCollapse @bind-Expanded=""boundExpanded"">
    The button above and the collapse share one value, so either of them can change it and both of them see it.
</BitCollapse>
<div>The bound value is currently <b>@boundExpanded</b>.</div>

<BitButton OnClick=""() => defaultCollapseRef?.ToggleAsync()"">Toggle</BitButton>
<BitCollapse @ref=""defaultCollapseRef"" DefaultExpanded OnChange=""HandleDefaultChange"">
    This section starts open because DefaultExpanded is set, and nothing on the page holds its state.
</BitCollapse>
<div>@defaultChangeLog</div>

<BitButton OnClick=""() => imperativeCollapseRef?.ExpandAsync()"">Expand</BitButton>
<BitButton OnClick=""() => imperativeCollapseRef?.CollapseAsync()"">Collapse</BitButton>
<BitButton OnClick=""() => imperativeCollapseRef?.ToggleAsync()"">Toggle</BitButton>
<BitCollapse @ref=""imperativeCollapseRef"">
    ExpandAsync, CollapseAsync and ToggleAsync go through the same path a bound value does, so the change is
    reported once through both ExpandedChanged and OnChange.
</BitCollapse>";
    private readonly string example2CsharpCode = @"
private bool boundExpanded = true;
private string defaultChangeLog = string.Empty;
private BitCollapse? defaultCollapseRef;
private BitCollapse? imperativeCollapseRef;

private void HandleDefaultChange(bool value)
{
    defaultChangeLog = $""OnChange reported {value}."";
}";

    private readonly string example3RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""horizontalExpanded"" />
<BitCollapse Horizontal Expanded=""horizontalExpanded"" Background=""BitColorKind.Secondary"">
    <div style=""white-space:nowrap"">This panel opens sideways.</div>
</BitCollapse>";
    private readonly string example3CsharpCode = @"
private bool horizontalExpanded = true;";

    private readonly string example4RazorCode = @"
<BitCollapse Expanded=""peekExpanded"" CollapsedSize=""4.5rem"">
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
    private readonly string example4CsharpCode = @"
private bool peekExpanded;";

    private readonly string example5RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""transitionExpanded"" />
<BitCollapse Expanded=""transitionExpanded"" Duration=""1000"" Delay=""200"" Easing=""cubic-bezier(0.68, -0.55, 0.27, 1.55)"">
    A thousand milliseconds after a two hundred millisecond wait, on an easing that overshoots at both ends.
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""noFadeExpanded"" />
<BitCollapse Expanded=""noFadeExpanded"" NoFade Duration=""800"">
    The size opens and closes this section; the content never changes its opacity.
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""noAnimationExpanded"" />
<BitCollapse Expanded=""noAnimationExpanded"" NoAnimation>
    There is no transition here at all, which is what a section that toggles as part of a larger change wants.
</BitCollapse>";
    private readonly string example5CsharpCode = @"
private bool transitionExpanded = true;
private bool noFadeExpanded = true;
private bool noAnimationExpanded = true;";

    private readonly string example6RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""surfaceExpanded"" />

<BitCollapse Expanded=""surfaceExpanded"">The padding and the primary background of the component.</BitCollapse>

<BitCollapse Expanded=""surfaceExpanded"" Background=""BitColorKind.Secondary"">A background of the secondary color kind.</BitCollapse>

<BitCollapse Expanded=""surfaceExpanded"" Background=""BitColorKind.Transparent"" NoPadding>
    <BitMessage Color=""BitColor.Info"">Content that carries its own surface and its own insets.</BitMessage>
</BitCollapse>";
    private readonly string example6CsharpCode = @"
private bool surfaceExpanded = true;";

    private readonly string example7RazorCode = @"
<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""lazyExpanded"" />
<BitCollapse LazyRender Expanded=""lazyExpanded"">
    @{ lazyRenderCount++; }
    <div>This content has rendered @lazyRenderCount time(s); the first of them was the moment the section was first opened.</div>
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""unmountExpanded"" />
<BitCollapse UnmountOnCollapse Expanded=""unmountExpanded"">
    <BitTextField Label=""Type something, then close and reopen"" />
</BitCollapse>";
    private readonly string example7CsharpCode = @"
private bool lazyExpanded;
private int lazyRenderCount;
private bool unmountExpanded = true;";

    private readonly string example8RazorCode = @"
<BitButton Id=""a11y-trigger""
           aria-controls=""a11y-collapse-content""
           aria-expanded=""@(a11yExpanded ? ""true"" : ""false"")""
           OnClick=""() => a11yExpanded = !a11yExpanded"">
    @(a11yExpanded ? ""Hide shipping details"" : ""Show shipping details"")
</BitButton>
<BitCollapse Id=""a11y-collapse"" Expanded=""a11yExpanded"" LabelledBy=""a11y-trigger"">
    Orders placed before 2 pm ship the same day. <BitLink Href=""/components/collapse"">Read the full policy</BitLink>.
</BitCollapse>";
    private readonly string example8CsharpCode = @"
private bool a11yExpanded;";

    private readonly string example9RazorCode = @"
<style>
    .custom-expanded {
        padding: 10px;
        background-color: #808080;
        border: 1px solid #0054C6;
    }

    .custom-wrapper {
        font-style: italic;
    }
</style>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expandedStyle"" />
<BitCollapse Expanded=""expandedStyle"" Styles=""@(new() { Expanded = ""padding:10px;background-color:#333;border: 1px solid #ff0000;"" })"">
    In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits
    to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite
    possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the
    vibrant narratives of tomorrow.
</BitCollapse>

<BitToggleButton OnText=""Collapse"" OffText=""Expand"" @bind-IsChecked=""expandedClass"" />
<BitCollapse Expanded=""expandedClass"" Classes=""@(new() { Expanded = ""custom-expanded"", Wrapper = ""custom-wrapper"" })"">
    In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and
    each word has the power to transform into something extraordinary. Here lies the start of something new-an
    opportunity to craft, inspire, and create.
</BitCollapse>";
    private readonly string example9CsharpCode = @"
private bool expandedClass = true;
private bool expandedStyle = true;";

    private readonly string example10RazorCode = @"
<BitToggleButton OnText=""بستن"" OffText=""باز کردن"" @bind-IsChecked=""expandedRtl"" />
<BitCollapse Expanded=""expandedRtl"" Dir=""BitDir.Rtl"">
    لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ و با استفاده از طراحان گرافیک است.
    چاپگرها و متون بلکه روزنامه و مجله در ستون و سطرآنچنان که لازم است
    و برای شرایط فعلی تکنولوژی مورد نیاز و کاربردهای متنوع با هدف بهبود ابزارهای کاربردی می باشد.
</BitCollapse>

<div dir=""rtl"">
    <BitToggleButton OnText=""بستن"" OffText=""باز کردن"" @bind-IsChecked=""expandedRtlHorizontal"" />
    <BitCollapse Horizontal Expanded=""expandedRtlHorizontal"" Dir=""BitDir.Rtl"" Background=""BitColorKind.Secondary"">
        <div style=""white-space:nowrap"">این بخش به سمت راست باز می شود.</div>
    </BitCollapse>
</div>";
    private readonly string example10CsharpCode = @"
private bool expandedRtl = true;
private bool expandedRtlHorizontal = true;";
}
