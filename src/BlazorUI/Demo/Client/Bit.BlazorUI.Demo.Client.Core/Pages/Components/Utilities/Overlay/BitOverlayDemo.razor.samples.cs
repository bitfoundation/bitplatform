namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.Overlay;

public partial class BitOverlayDemo
{
    private readonly string example1RazorCode = @"
<BitButton OnClick=""() => basicIsOpen = true"">Show Overlay</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => modeFullIsOpen = true"">Show dimmed Overlay</BitButton>

<BitOverlay @bind-IsOpen=""basicIsOpen"" Position=""BitPosition.Center"">
    <BitProgress Circular Indeterminate Thickness=""10"" AriaLabel=""Loading"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""modeFullIsOpen"" Position=""BitPosition.Center"" ModeFull>
    <BitProgress Circular Indeterminate Thickness=""10"" AriaLabel=""Loading"" />
</BitOverlay>";
    private readonly string example1CsharpCode = @"
private bool basicIsOpen;
private bool modeFullIsOpen;";

    private readonly string example2RazorCode = @"
<style>
    .position-grid {
        gap: 0.5rem;
        display: grid;
        max-width: 480px;
        grid-template-columns: repeat(3, 1fr);
    }

    .content {
        margin: 1rem;
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<div class=""position-grid"">
    @foreach (var position in positions)
    {
        <BitButton Variant=""BitVariant.Outline"" OnClick=""() => OpenAt(position)"">@position</BitButton>
    }
</div>

<BitOverlay @bind-IsOpen=""positionIsOpen"" Position=""position"" ModeFull>
    <div class=""content"">@position</div>
</BitOverlay>";
    private readonly string example2CsharpCode = @"
private static readonly BitPosition[] positions =
[
    BitPosition.TopStart, BitPosition.TopCenter, BitPosition.TopEnd,
    BitPosition.CenterStart, BitPosition.Center, BitPosition.CenterEnd,
    BitPosition.BottomStart, BitPosition.BottomCenter, BitPosition.BottomEnd,
];
private bool positionIsOpen;
private BitPosition position = BitPosition.Center;
private void OpenAt(BitPosition value)
{
    position = value;
    positionIsOpen = true;
}";

    private readonly string example3RazorCode = @"
<style>
    .content {
        width: 87%;
        padding: 1rem;
        max-width: 960px;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }

    .btn-container {
        gap: 1rem;
        display: flex;
        flex-flow: row wrap;
        align-items: center;
    }
</style>

<div class=""btn-container"">
    <BitCheckbox Label=""Blocking"" @bind-Value=""dismissalBlocking"" />
    <BitCheckbox Label=""NoDismissOnEscape"" @bind-Value=""dismissalNoEscape"" />
    <BitButton OnClick=""() => dismissalIsOpen = true"">Show Overlay</BitButton>
</div>

<BitOverlay @bind-IsOpen=""dismissalIsOpen""
            Position=""BitPosition.Center""
            ModeFull
            Blocking=""dismissalBlocking""
            NoDismissOnEscape=""dismissalNoEscape"">
    <div class=""content"">
        <h3>Try to close me</h3>
        <div>
            Click the dimmed layer, press Escape, or select this text and release outside the box.
            Clicks in here are the content's own.
        </div>
        <br />
        <div class=""btn-container"">
            <BitButton Variant=""BitVariant.Outline"" OnClick=""() => dismissalClicks++"">Clicked @dismissalClicks time(s)</BitButton>
            <BitButton OnClick=""() => dismissalIsOpen = false"">Close</BitButton>
        </div>
    </div>
</BitOverlay>";
    private readonly string example3CsharpCode = @"
private bool dismissalIsOpen;
private bool dismissalBlocking;
private bool dismissalNoEscape;
private int dismissalClicks;";

    private readonly string example4RazorCode = @"
<style>
    .container {
        display: flex;
        height: 240px;
        position: relative;
        align-items: center;
        border-radius: 16px;
        justify-content: center;
        border: 2px solid var(--bit-clr-pri);
    }

    .report {
        gap: 1rem;
        display: flex;
        min-width: 240px;
        flex-flow: column nowrap;
    }
</style>

<BitButton OnClick=""LoadReport"">Load</BitButton>

<div class=""container"" aria-busy=""@(absoluteIsOpen ? ""true"" : ""false"")"">
    <div class=""report"" inert=""@absoluteIsOpen"">
        <h3>Report</h3>
        <BitTextField Label=""Filter"" />
        <BitButton Variant=""BitVariant.Outline"">Export</BitButton>
    </div>
    <BitOverlay @bind-IsOpen=""absoluteIsOpen"" Position=""BitPosition.Center"" ModeFull AbsolutePosition Blocking>
        <BitProgress Circular Indeterminate Thickness=""10"" AriaLabel=""Loading the report"" />
    </BitOverlay>
</div>";
    private readonly string example4CsharpCode = @"
private bool absoluteIsOpen;

private async Task LoadReport()
{
    if (absoluteIsOpen) return;

    absoluteIsOpen = true;
    await Task.Delay(3000);
    absoluteIsOpen = false;
}";

    private readonly string example5RazorCode = @"
<style>
    .content {
        margin: 1rem;
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }

    .scroller {
        height: 240px;
        padding: 15px;
        overflow: auto;
        margin-top: 15px;
        position: relative;
        border-radius: 4px;
        border: 2px solid green;
    }
</style>

<BitButton OnClick=""() => pageLockIsOpen = true"">Lock the page</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => boxScrollIsOpen = true"">Cover the box, keep it scrolling</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => boxLockIsOpen = true"">Cover the box, lock it</BitButton>

<BitOverlay @bind-IsOpen=""pageLockIsOpen"" Position=""BitPosition.Center"" ModeFull AutoToggleScroll>
    <div class=""content"">Try to scroll the page.</div>
</BitOverlay>

<div class=""scroller"">
    <BitOverlay @bind-IsOpen=""boxScrollIsOpen""
                Position=""BitPosition.Center""
                ScrollerSelector="".scroller""
                AbsolutePosition>
        <div class=""content"">The box still scrolls behind me.</div>
    </BitOverlay>

    <BitOverlay @bind-IsOpen=""boxLockIsOpen""
                Position=""BitPosition.Center""
                ModeFull
                ScrollerSelector="".scroller""
                AbsolutePosition
                AutoToggleScroll>
        <div class=""content"">The box is locked.</div>
    </BitOverlay>

    @for (int i = 1; i <= 20; i++)
    {
        <div>Line @i of the scrolling box.</div>
    }
</div>";
    private readonly string example5CsharpCode = @"
private bool pageLockIsOpen;
private bool boxScrollIsOpen;
private bool boxLockIsOpen;";

    private readonly string example6RazorCode = @"
<style>
    .content {
        margin: 1rem;
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<BitButton OnClick=""() => eventsIsOpen = true"">Show Overlay</BitButton>

<BitOverlay @bind-IsOpen=""eventsIsOpen""
            Position=""BitPosition.Center""
            ModeFull
            Blocking
            OnClick=""HandleOverlayClick""
            OnOpen=""HandleOverlayOpen""
            OnClose=""HandleOverlayClose"">
    <div class=""content"">Clicked @eventsClicks time(s). Closes on the third click.</div>
</BitOverlay>

<div>Opened @eventsOpened time(s), closed @eventsClosed time(s).</div>";
    private readonly string example6CsharpCode = @"
private bool eventsIsOpen;
private int eventsClicks;
private int eventsOpened;
private int eventsClosed;

private void HandleOverlayClick(MouseEventArgs e)
{
    if (++eventsClicks >= 3)
    {
        eventsIsOpen = false;
    }
}

private void HandleOverlayOpen()
{
    eventsClicks = 0;
    eventsOpened++;
}

private void HandleOverlayClose() => eventsClosed++;";

    private readonly string example7RazorCode = @"
<style>
    .content {
        margin: 1rem;
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<BitButton OnClick=""OpenAndCloseLater"">Open for 3 seconds</BitButton>

<BitOverlay @ref=""overlayRef"" Position=""BitPosition.Center"" ModeFull>
    <div class=""content"">Opened by Open() and closed by Close() - or by a click, or Escape.</div>
</BitOverlay>";
    private readonly string example7CsharpCode = @"
private BitOverlay overlayRef = default!;

private async Task OpenAndCloseLater()
{
    await overlayRef.Open();
    await Task.Delay(3000);
    await overlayRef.Close();
}";

    private readonly string example8RazorCode = @"
<style>
    .content {
        margin: 1rem;
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<BitButton OnClick=""() => cascadedIsOpen = true"">Takes the cascade</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => cascadedOwnIsOpen = true"">Its own position</BitButton>

<BitParams Parameters=""overlayParams"">
    <BitOverlay @bind-IsOpen=""cascadedIsOpen"">
        <div class=""content"">Dimmed and centered - both from BitParams.</div>
    </BitOverlay>

    <BitOverlay @bind-IsOpen=""cascadedOwnIsOpen"" Position=""BitPosition.TopCenter"">
        <div class=""content"">My own Position, the rest from BitParams.</div>
    </BitOverlay>
</BitParams>";
    private readonly string example8CsharpCode = @"
private bool cascadedIsOpen;
private bool cascadedOwnIsOpen;

private readonly BitOverlayParams[] overlayParams =
[
    new() { ModeFull = true, Position = BitPosition.Center }
];";

    private readonly string example9RazorCode = @"
<style>
    .custom-overlay {
        backdrop-filter: blur(10px);
        background-color: rgba(0, 0, 0, 0.2);
    }

    .content {
        padding: 1rem;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<BitButton OnClick=""() => styleIsOpen = true"">Style</BitButton>
<BitButton OnClick=""() => classIsOpen = true"">Class</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => cssVarsIsOpen = true"">CSS variables</BitButton>

<BitOverlay @bind-IsOpen=""styleIsOpen""
            Position=""BitPosition.Center""
            Style=""background: linear-gradient(135deg, rgba(78, 0, 142, 0.55), rgba(255, 0, 96, 0.35));"">
    <BitProgress Circular Indeterminate Thickness=""10"" AriaLabel=""Loading"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""classIsOpen"" Position=""BitPosition.Center"" Class=""custom-overlay"">
    <BitProgress Circular Indeterminate Thickness=""10"" AriaLabel=""Loading"" />
</BitOverlay>

<BitOverlay @bind-IsOpen=""cssVarsIsOpen""
            ModeFull
            Position=""BitPosition.BottomCenter""
            Style=""--bit-Overlay-background: #1e1b4b99; --bit-Overlay-backdrop-filter: blur(4px); --bit-Overlay-padding: 2rem; --bit-Overlay-transition-duration: 400ms;"">
    <div class=""content"">Tint, blur, padding and a slower fade all come from variables.</div>
</BitOverlay>";
    private readonly string example9CsharpCode = @"
private bool styleIsOpen;
private bool classIsOpen;
private bool cssVarsIsOpen;";

    private readonly string example10RazorCode = @"
<style>
    .content {
        margin: 1rem;
        padding: 1rem;
        max-width: 360px;
        border-radius: 4px;
        background-color: var(--bit-clr-bg-pri);
        border: 1.6px solid var(--bit-clr-pri);
    }
</style>

<BitButton Dir=""BitDir.Rtl"" OnClick=""() => rtlIsOpen = true"">نمایش روکش</BitButton>

<BitOverlay @bind-IsOpen=""rtlIsOpen"" Dir=""BitDir.Rtl"" Position=""BitPosition.TopStart"" ModeFull>
    <div class=""content"">
        روزی روزگاری، داستان‌ها میان مردم پیوند می‌ساختند؛ هم‌نوایی صداهایی که رویاهای مشترک می‌آفریدند.
    </div>
</BitOverlay>";
    private readonly string example10CsharpCode = @"
private bool rtlIsOpen;";
}
