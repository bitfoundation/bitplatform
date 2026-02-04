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
        new() { Name = "Add", Image = BitIconName.Add },
        new() { Name = "Edit", Image = BitIconName.Edit },
        new() { Name = "Delete", Image = BitIconName.Delete }
    ];

    private List<Operation> onlyIconCustoms =
    [
        new() { Name = "Add", Image = BitIconName.Add },
        new() { Image = BitIconName.Edit },
        new() { Name = "Delete", Image = BitIconName.Delete }
    ];

    private List<Operation> reversedIconCustoms =
    [
        new() { Name = "Add", Image = BitIconName.Add, ReversedIcon = true },
        new() { Name = "Edit", Image = BitIconName.Edit, ReversedIcon = true },
        new() { Name = "Delete", Image = BitIconName.Delete, ReversedIcon = true }
    ];

    private BitButtonGroupNameSelectors<Operation> toggledNameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        OnText = { Selector = i => i.OnName },
        OffText = { Selector = i => i.OffName },
        OnTitle = { Selector = i => i.OnTitle },
        OffTitle = { Selector = i => i.OffTitle },
        OnIconName = { Selector = i => i.OnImage },
        OffIconName = { Selector = i => i.OffImage },
        ReversedIcon = { Selector = i => i.ReversedIcon },
        IsToggled = { Name = nameof(Operation.IsSelected) }
    };

    private string? toggleKey = "play";
    private List<Operation> toggledCustoms =
    [
        new() { Id = "back", OnName = "Back (2X)", OffName = "Back (1X)", OnImage = BitIconName.RewindTwoX, OffImage = BitIconName.Rewind },
        new() { Id = "play", OnTitle = "Resume", OffTitle = "Play", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
        new() { Id = "forward", OnName = "Forward (2X)", OffName = "Forward (1X)", OnImage = BitIconName.FastForwardTwoX, OffImage = BitIconName.FastForward, ReversedIcon = true }
    ];

    private Operation? onChangeToggleCustom;
    private List<Operation> changeToggledCustoms =
    [
        new() { Id = "back", OnName = "Back (2X)", OffName = "Back (1X)", OnImage = BitIconName.RewindTwoX, OffImage = BitIconName.Rewind },
        new() { Id = "play", OnTitle = "Resume", OffTitle = "Play", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
        new() { Id = "forward", OnName = "Forward (2X)", OffName = "Forward (1X)", OnImage = BitIconName.FastForwardTwoX, OffImage = BitIconName.FastForward, ReversedIcon = true }
    ];

    private List<Operation> eventsCustoms =
    [
        new() { Name = "Increase", Image = BitIconName.Add },
        new() { Name = "Reset", Image = BitIconName.Reset },
        new() { Name = "Decrease", Image = BitIconName.Remove }
    ];

    private List<Operation> externalIconCustoms =
    [
        new() { Name = "Add", IconInfo = "fa-solid fa-plus" },
        new() { Name = "Edit", IconInfo = BitIconInfo.Css("fa-solid fa-pen") },
        new() { Name = "Delete", IconInfo = BitIconInfo.Fa("solid trash") }
    ];

    private List<Operation> styleClassCustoms =
    [
        new()
        {
            Name = "Styled",
            Style = "color: tomato; border-color: brown; background-color: peachpuff;",
            Image = BitIconName.Brush,
        },
        new()
        {
            Name = "Classed",
            Class = "custom-item",
            Image = BitIconName.FormatPainter,
        }
    ];

    private List<Operation> rtlCustoms =
    [
        new() { Name = "اضافه کردن", Image = BitIconName.Add },
        new() { Name = "ویرایش", Image = BitIconName.Edit },
        new() { Name = "حذف", Image = BitIconName.Delete }
    ];

    protected override void OnInitialized()
    {
        eventsCustoms[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
        eventsCustoms[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
        eventsCustoms[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
    }
}
