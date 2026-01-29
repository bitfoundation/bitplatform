namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "BL0005:Component parameter should not be set outside of its component.", Justification = "<Pending>")]
public partial class _BitButtonGroupItemDemo
{
    private int clickCounter;
    private string? clickedItem;

    private List<BitButtonGroupItem> basicItems =
    [
        new() { Text = "Add" }, new() { Text = "Edit" }, new() { Text = "Delete" }
    ];

    private List<BitButtonGroupItem> disabledItems =
    [
        new() { Text = "Add" }, new() { Text = "Edit", IsEnabled = false }, new() { Text = "Delete" }
    ];

    private List<BitButtonGroupItem> iconItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { Text = "Edit", IconName = BitIconName.Edit },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    ];

    private List<BitButtonGroupItem> onlyIconItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { IconName = BitIconName.Edit },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    ];

    private List<BitButtonGroupItem> reversedIconItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add, ReversedIcon = true },
        new() { Text = "Edit", IconName = BitIconName.Edit, ReversedIcon = true },
        new() { Text = "Delete", IconName = BitIconName.Delete, ReversedIcon = true }
    ];

    private string? toggleKey = "play";
    private List<BitButtonGroupItem> toggledItems =
    [
        new() { Key = "back", OnText = "Back (2X)", OffText = "Back (1X)", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
        new() { Key = "play", OnTitle = "Resume", OffTitle = "Play", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
        new() { Key = "forward", OnText = "Forward (2X)", OffText = "Forward (1X)", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
    ];

    private BitButtonGroupItem? onChangeToggleItem;
    private List<BitButtonGroupItem> changeToggledItems =
    [
        new() { Key = "back", OnText = "Back (2X)", OffText = "Back (1X)", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
        new() { Key = "play", OnTitle = "Resume", OffTitle = "Play", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
        new() { Key = "forward", OnText = "Forward (2X)", OffText = "Forward (1X)", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
    ];

    private List<BitButtonGroupItem> eventsItems =
    [
        new() { Text = "Increase", IconName = BitIconName.Add },
        new() { Text = "Reset", IconName = BitIconName.Reset },
        new() { Text = "Decrease", IconName = BitIconName.Remove }
    ];

    private List<BitButtonGroupItem> externalIconItems =
    [
        new() { Text = "Add", Icon = "fa-solid fa-plus" },
        new() { Text = "Edit", Icon = BitIconInfo.Css("fa-solid fa-pen") },
        new() { Text = "Delete", Icon = BitIconInfo.Fa("solid trash") }
    ];

    private List<BitButtonGroupItem> styleClassItems =
    [
        new()
        {
            Text = "Styled",
            Style = "color: tomato; border-color: brown; background-color: peachpuff;",
            IconName = BitIconName.Brush,
        },
        new()
        {
            Text = "Classed",
            Class = "custom-item",
            IconName = BitIconName.FormatPainter,
        }
    ];

    private List<BitButtonGroupItem> rtlItems =
    [
        new() { Text = "اضافه کردن", IconName = BitIconName.Add },
        new() { Text = "ویرایش", IconName = BitIconName.Edit },
        new() { Text = "حذف", IconName = BitIconName.Delete }
    ];

    protected override void OnInitialized()
    {
        eventsItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
        eventsItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
        eventsItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
    }
}
