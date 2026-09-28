namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Loading;

public partial class BitLoadingDemo
{
    private readonly string example1RazorCode = @"
<BitBarsLoading />

<BitCircleLoading />

<BitDotsRingLoading />

<BitDualRingLoading />

<BitEllipsisLoading />

<BitGridLoading />

<BitHeartLoading />

<BitHourglassLoading />

<BitRingLoading />

<BitRippleLoading />

<BitRollerLoading />

<BitSpinnerLoading />

<BitXboxLoading />

<BitSlickBarsLoading />

<BitBouncingDotsLoading />

<BitRollingDashesLoading />

<BitOrbitingDotsLoading />

<BitRollingSquareLoading />";

    private readonly string example2RazorCode = @"
<BitRingLoading Label=""Uploading photos..."" />

<BitDotsRingLoading Label=""Top"" LabelPosition=""BitLabelPosition.Top"" />

<BitDotsRingLoading Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" />

<BitDotsRingLoading Label=""Start"" LabelPosition=""BitLabelPosition.Start"" />

<BitDotsRingLoading Label=""End"" LabelPosition=""BitLabelPosition.End"" />

<BitRollerLoading LabelPosition=""BitLabelPosition.Bottom"">
    <LabelTemplate>
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.SecondaryForeground"">
            Restoring your session
        </BitText>
    </LabelTemplate>
</BitRollerLoading>";

    private readonly string example3RazorCode = @"
<BitToggleButton @bind-IsChecked=""isPaused"" OnText=""Resume"" OffText=""Pause"" />

<BitRingLoading Label=""0.5x"" Speed=""0.5"" Paused=""isPaused"" />

<BitRingLoading Label=""1x (default)"" Paused=""isPaused"" />

<BitRingLoading Label=""2x"" Speed=""2"" Paused=""isPaused"" />

<BitBarsLoading Label=""4x"" Speed=""4"" Paused=""isPaused"" />";
    private readonly string example3CsharpCode = @"
private bool isPaused;";

    private readonly string example4RazorCode = @"
<BitButton OnClick=""StartWork"" IsEnabled=""@(isWorking is false)"">Run a 1.5s task</BitButton>

@if (isWorking)
{
    <BitSpinnerLoading />

    <BitSpinnerLoading Delay=""500"" />

    @* The task is over before the delay elapses, so this one never shows at all. *@
    <BitSpinnerLoading Delay=""3000"" />
}";
    private readonly string example4CsharpCode = @"
private bool isWorking;

private async Task StartWork()
{
    isWorking = true;
    await Task.Delay(1500);
    isWorking = false;
}";

    private readonly string example5RazorCode = @"
<BitRingLoading Label=""Default"" />

<BitRingLoading Label=""Thickness=2"" Thickness=""2"" />

<BitRingLoading Label=""Thickness=12"" Thickness=""12"" />

<BitSpinnerLoading Label=""Spinner"" Thickness=""10"" />

<BitDualRingLoading Label=""DualRing"" Thickness=""2"" />

<BitRippleLoading Label=""Ripple"" Thickness=""8"" />

<BitXboxLoading Label=""Xbox"" Thickness=""6"" />";

    private readonly string example6RazorCode = @"
<p>
    Fetching the latest results
    <BitRingLoading Inline />
    please wait.
</p>

<BitText Typography=""BitTypography.H4"">
    Syncing <BitDotsRingLoading Inline />
</BitText>

<BitButton IsEnabled=""false"">
    <BitRingLoading Inline CustomColor=""currentColor"" Label=""Saving"" />
</BitButton>";

    private readonly string example7RazorCode = @"
<style>
    .orders {
        display: flex;
        gap: 0.5rem;
        padding: 1rem;
        position: relative;
        flex-direction: column;
        border: 1px solid gray;
    }
</style>


<BitButton OnClick=""Refresh"" IsEnabled=""@(isRefreshing is false)"">Refresh orders</BitButton>

<div class=""orders"" aria-busy=""@(isRefreshing ? ""true"" : ""false"")"">
    <BitOverlay IsOpen=""isRefreshing"" AbsolutePosition ModeFull Style=""align-items:center;justify-content:center;color:white"">
        <BitRingLoading Label=""Refreshing orders..."" CustomSize=""40"" CustomColor=""currentColor"" />
    </BitOverlay>
    <div>Order #1024 - Shipped</div>
    <div>Order #1025 - Processing</div>
    <div>Order #1026 - Delivered</div>
</div>";
    private readonly string example7CsharpCode = @"
private bool isRefreshing;

private async Task Refresh()
{
    isRefreshing = true;
    await Task.Delay(2000);
    isRefreshing = false;
}";

    private readonly string example8RazorCode = @"
@* role=""status"" aria-live=""polite"" and a visually hidden ""Loading"" by default. *@
<BitSpinnerLoading />

@* The hidden text becomes the AriaLabel. *@
<BitSpinnerLoading AriaLabel=""Fetching your orders"" />

@* Decorative: the surroundings already report the wait. *@
<BitSpinnerLoading Role=""none"" />

@* Interrupts the screen reader rather than waiting for it. *@
<BitSpinnerLoading Label=""Signing you out"" AriaLive=""assertive"" />

@* An indeterminate progress bar, named by its Label. *@
<BitSpinnerLoading Label=""Exporting"" Role=""progressbar"" />";

    private readonly string example9RazorCode = @"
<BitParams Parameters=""loadingParams"">
    <BitRingLoading Label=""Syncing"" />

    <BitDualRingLoading Label=""Uploading"" />

    <BitSpinnerLoading Label=""Indexing"" LabelPosition=""BitLabelPosition.Bottom"" />
</BitParams>";
    private readonly string example9CsharpCode = @"
private readonly BitLoadingParams[] loadingParams =
[
    new()
    {
        CustomSize = 48,
        Thickness = 3,
        Speed = 1.5,
        LabelPosition = BitLabelPosition.End
    }
];";

    private readonly string example10RazorCode = @"
<BitBarsLoading Label=""Primary"" Color=""BitColor.Primary"" />

<BitCircleLoading Label=""Secondary"" Color=""BitColor.Secondary"" />

<BitDotsRingLoading Label=""Tertiary"" Color=""BitColor.Tertiary"" />

<BitDualRingLoading Label=""Info"" Color=""BitColor.Info"" />

<BitEllipsisLoading Label=""Success"" Color=""BitColor.Success"" />

<BitGridLoading Label=""Warning"" Color=""BitColor.Warning"" />

<BitHeartLoading Label=""SevereWarning"" Color=""BitColor.SevereWarning"" />

<BitHourglassLoading Label=""Error"" Color=""BitColor.Error"" />


<BitBarsLoading Label=""brown"" CustomColor=""brown"" />

<BitCircleLoading Label=""rgb(0 107 185 / 75%)"" CustomColor=""rgb(0 107 185 / 75%)"" />

<BitDotsRingLoading Label=""#426985"" CustomColor=""#426985"" />

<BitDualRingLoading Label=""hsl(106 100% 22% / 1)"" CustomColor=""hsl(106 100% 22% / 1)"" />

<div style=""color:mediumvioletred"">
    <BitSpinnerLoading Label=""currentColor"" CustomColor=""currentColor"" />
</div>";

    private readonly string example11RazorCode = @"
<BitXboxLoading Label=""Small"" Size=""BitSize.Small"" />

<BitXboxLoading Label=""Medium"" Size=""BitSize.Medium"" />

<BitXboxLoading Label=""Large"" Size=""BitSize.Large"" />

<BitXboxLoading Label=""Custom (128)"" CustomSize=""128"" />

<BitXboxLoading Label=""Custom (24)"" CustomSize=""24"" />";

    private readonly string example12RazorCode = @"
<style>
    .custom-class {
        padding: 1rem;
        border-radius: 8px;
        background-color: darkslateblue;
        --bit-Loading-color: gold;
        --bit-Loading-label-color: gold;
    }

    .custom-root {
        padding: 0.5rem;
        border-radius: 8px;
        background-color: whitesmoke;
    }

    .custom-child {
        border-radius: 0;
        background-color: seagreen;
    }

    .custom-label {
        color: seagreen;
        font-weight: bold;
    }
</style>


<BitRingLoading Label=""Style"" Style=""padding:1rem;border:1px solid gray;border-radius:8px"" />

<BitRingLoading Label=""Class"" Class=""custom-class"" />

<BitGridLoading Label=""Styles""
                Styles=""@(new() { Root = ""padding:0.5rem"",
                                  Container = ""outline:1px dashed gray"",
                                  Child = ""border-radius:0"",
                                  Label = ""color:tomato;font-weight:bold"" })"" />

<BitGridLoading Label=""Classes""
                Classes=""@(new() { Root = ""custom-root"",
                                   Child = ""custom-child"",
                                   Label = ""custom-label"" })"" />


<BitRingLoading Label=""Track""
                Style=""--bit-Loading-track-color: var(--bit-clr-brd-sec); --bit-Loading-thickness: 4px;"" />

<BitXboxLoading Label=""Slower, wider gap""
                Style=""--bit-Loading-speed: 0.5; --bit-Loading-gap: 1rem; --bit-Loading-label-color: var(--bit-clr-fg-sec);"" />

<div style=""display:flex;gap:1.5rem;
            --bit-Loading-size: 32px;
            --bit-Loading-color: var(--bit-clr-suc);
            --bit-Loading-label-font-weight: var(--bit-tpg-fw-semibold);"">
    <BitDotsRingLoading Label=""From"" />
    <BitGridLoading Label=""an"" />
    <BitHeartLoading Label=""ancestor"" />
</div>";

    private readonly string example13RazorCode = @"
<div dir=""rtl"">
    <BitRingLoading Dir=""BitDir.Rtl"" Label=""شروع"" LabelPosition=""BitLabelPosition.Start"" />

    <BitRingLoading Dir=""BitDir.Rtl"" Label=""پایان"" LabelPosition=""BitLabelPosition.End"" />

    @* The two loaders whose motion travels across the box are mirrored, so they run toward the end of the line. *@
    <BitEllipsisLoading Dir=""BitDir.Rtl"" Label=""نقطه‌ها"" />

    <BitRollingSquareLoading Dir=""BitDir.Rtl"" Label=""مربع"" />
</div>";
}
