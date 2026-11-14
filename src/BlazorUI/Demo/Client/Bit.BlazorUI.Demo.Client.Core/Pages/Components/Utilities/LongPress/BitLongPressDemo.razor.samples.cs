namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Utilities.LongPress;

public partial class BitLongPressDemo
{
    private readonly string example1RazorCode = @"
<BitLongPress OnLongPress=""HandleBasicLongPress"">
    <div class=""press-box"">Press and hold here</div>
</BitLongPress>

<div>Long presses: <b>@basicCount</b></div>
<div>Last: <b>@basicLast</b></div>";
    private readonly string example1CsharpCode = @"
private int basicCount;
private string? basicLast;

private void HandleBasicLongPress(BitLongPressEventArgs e)
{
    basicCount++;
    basicLast = $""{e.PointerType} at {e.ClientX:0}, {e.ClientY:0}"";
}";

    private readonly string example2RazorCode = @"
<BitLongPress OnLongPress=""e => contextMenu.OpenAt(e.ClientX, e.ClientY)"">
    <BitContextMenu @ref=""contextMenu"" Items=""menuItems"" OnItemClick=""(BitMenuButtonItem item) => menuClicked = item.Text"">
        <div class=""press-box"">Right-click, or press and hold here</div>
    </BitContextMenu>
</BitLongPress>

<div>Clicked item: <b>@menuClicked</b></div>";
    private readonly string example2CsharpCode = @"
private BitContextMenu<BitMenuButtonItem> contextMenu = default!;
private string? menuClicked;

private static readonly List<BitMenuButtonItem> menuItems =
[
    new() { Text = ""Share"", Key = ""share"", IconName = BitIconName.Share },
    new() { Text = ""Rename"", Key = ""rename"", IconName = BitIconName.Rename },
    new() { IsSeparator = true },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete },
];";

    private readonly string example3RazorCode = @"
<BitLongPress OnLongPress=""HandleClickLongPress"">
    <BitButton OnClick=""HandleClick"">Tap, or press and hold</BitButton>
</BitLongPress>

<div>Last: <b>@clickLog</b></div>";
    private readonly string example3CsharpCode = @"
private int clickClicks;
private int clickLongPresses;
private string? clickLog;

private void HandleClick() => clickLog = $""Clicked ({++clickClicks})"";

private void HandleClickLongPress() => clickLog = $""Long-pressed ({++clickLongPresses})"";";

    private readonly string example4RazorCode = @"
<BitLongPress Delay=""1500"" OnLongPress=""HandleSlowLongPress"">
    <div class=""press-box"">Hold for 1.5 seconds</div>
</BitLongPress>

<BitLongPress MoveTolerance=""40"" OnLongPress=""HandleTolerantLongPress"">
    <div class=""press-box"">Hold, and drift up to 40px</div>
</BitLongPress>

<div>Last: <b>@delayLog</b></div>";
    private readonly string example4CsharpCode = @"
private int delayCount;
private string? delayLog;

private void HandleSlowLongPress() => delayLog = $""Held for 1.5 seconds ({++delayCount})"";

private void HandleTolerantLongPress() => delayLog = $""Held within 40px ({++delayCount})"";";

    private readonly string example5RazorCode = @"
<BitLongPress TouchOnly OnLongPress=""(BitLongPressEventArgs e) => touchLog = e.PointerType"">
    <div class=""press-box"">Press and hold with a finger or a pen</div>
</BitLongPress>

<div>Held by: <b>@touchLog</b></div>";
    private readonly string example5CsharpCode = @"
private string? touchLog;";

    private readonly string example6RazorCode = @"
<BitLongPress KeyboardTrigger
              AriaLabel=""Photo, press and hold for its options""
              OnLongPress=""HandleKeyboardLongPress"">
    <div class=""press-box"">Tab here, then press Shift+F10</div>
</BitLongPress>

<div>Last: <b>@keyboardLog</b></div>";
    private readonly string example6CsharpCode = @"
private string? keyboardLog;

private void HandleKeyboardLongPress(BitLongPressEventArgs e) => keyboardLog = $""{e.PointerType} at {e.ClientX:0}, {e.ClientY:0}"";";

    private readonly string example7RazorCode = @"
<BitLongPress Disabled KeyboardTrigger OnLongPress=""() => { }"">
    <div class=""press-box disabled"">Holding here does nothing</div>
</BitLongPress>";

    private readonly string example8RazorCode = @"
<style>
    .custom-press .press-box {
        transition: transform 0.15s ease, background-size 0.8s linear;
        background: linear-gradient(color-mix(in srgb, var(--bit-clr-pri) 25%, transparent), color-mix(in srgb, var(--bit-clr-pri) 25%, transparent)) no-repeat left / 0% 100%;
    }

    .custom-press.bit-lpr-prs .press-box {
        transform: scale(0.97);
        background-size: 100% 100%;
    }
</style>

<BitLongPress Class=""custom-press""
              Delay=""800""
              KeyboardTrigger
              AriaLabel=""Styled long press""
              Style=""--bit-LongPress-focus-color: var(--bit-clr-suc);""
              OnLongPress=""() => styleCount++"">
    <div class=""press-box"">Press and hold here</div>
</BitLongPress>

<div>Long presses: <b>@styleCount</b></div>";
    private readonly string example8CsharpCode = @"
private int styleCount;";
}
