namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private int clickCounter;
    private string? clickedCustom;

    private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

    private List<Operation> basicCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit" }, new() { Name = "Delete" }
    ];

    private List<Operation> disabledCustoms =
    [
        new() { Name = "Add" }, new() { Name = "Edit", IsEnabled = false }, new() { Name = "Delete" }
    ];

    private List<Operation> iconCustoms =
    [
        new() { Name = "Add", Icon = BitIconName.Add },
        new() { Name = "Edit", Icon = BitIconName.Edit },
        new() { Name = "Delete", Icon = BitIconName.Delete }
    ];

    private List<Operation> onlyIconCustoms =
    [
        new() { Name = "Add", Icon = BitIconName.Add },
        new() { Icon = BitIconName.Edit },
        new() { Name = "Delete", Icon = BitIconName.Delete }
    ];

    private List<Operation> reversedIconCustoms =
    [
        new() { Name = "Add", Icon = BitIconName.Add, ReversedIcon = true },
        new() { Name = "Edit", Icon = BitIconName.Edit, ReversedIcon = true },
        new() { Name = "Delete", Icon = BitIconName.Delete, ReversedIcon = true }
    ];

    private BitButtonGroupNameSelectors<Operation> toggledNameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        OnText = { Selector = i => i.OnName },
        OffText = { Selector = i => i.OffName },
        OnTitle = { Selector = i => i.OnTitle },
        OffTitle = { Selector = i => i.OffTitle },
        OnIconName = { Selector = i => i.OnIcon },
        OffIconName = { Selector = i => i.OffIcon },
        ReversedIcon = { Selector = i => i.ReversedIcon },
        IsToggled = { Name = nameof(Operation.IsSelected) }
    };

    private List<Operation> toggledCustoms =
    [
        new() { Id = "back", OnName = "Back (2X)", OffName = "Back (1X)", OnIcon = BitIconName.RewindTwoX, OffIcon = BitIconName.Rewind },
        new() { Id = "play", OnTitle = "Resume", OffTitle = "Play", OnIcon = BitIconName.PlayResume, OffIcon = BitIconName.Play },
        new() { Id = "forward", OnName = "Forward (2X)", OffName = "Forward (1X)", OnIcon = BitIconName.FastForwardTwoX, OffIcon = BitIconName.FastForward, ReversedIcon = true }
    ];

    private string? toggleKey = "play";
    private Operation? onChangeToggleCustom;

    private List<Operation> eventsCustoms =
    [
        new() { Name = "Increase", Icon = BitIconName.Add },
        new() { Name = "Reset", Icon = BitIconName.Reset },
        new() { Name = "Decrease", Icon = BitIconName.Remove }
    ];

    private List<Operation> styleClassCustoms =
    [
        new()
        {
            Name = "Styled",
            Style = "color: tomato; border-color: brown; background-color: peachpuff;",
            Icon = BitIconName.Brush,
        },
        new()
        {
            Name = "Classed",
            Class = "custom-item",
            Icon = BitIconName.FormatPainter,
        }
    ];

    private List<Operation> rtlCustoms =
    [
        new() { Name = "اضافه کردن", Icon = BitIconName.Add },
        new() { Name = "ویرایش", Icon = BitIconName.Edit },
        new() { Name = "حذف", Icon = BitIconName.Delete }
    ];

    protected override void OnInitialized()
    {
        eventsCustoms[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
        eventsCustoms[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
        eventsCustoms[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
    }
}
