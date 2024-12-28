namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

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

    private List<BitButtonGroupItem> toggledItems =
    [
        new() { OnText = "Back (2X)", OffText = "Back (1X)", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
        new() { OnTitle = "Resume", OffTitle = "Play", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
        new() { OnText = "Forward (2X)", OffText = "Forward (1X)", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
    ];

    private List<BitButtonGroupItem> eventsItems =
    [
        new() { Text = "Increase", IconName = BitIconName.Add },
        new() { Text = "Reset", IconName = BitIconName.Reset },
        new() { Text = "Decrease", IconName = BitIconName.Remove }
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
