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
        new() { Id = "play", OnName = "Resume", OffName = "Play", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
        new() { Id = "forward", OnName = "Forward (2X)", OffName = "Forward (1X)", OnImage = BitIconName.FastForwardTwoX, OffImage = BitIconName.FastForward, ReversedIcon = true }
    ];

    private Operation? onChangeToggleCustom;
    private List<Operation> changeToggledCustoms =
    [
        new() { Id = "back", OnName = "Back (2X)", OffName = "Back (1X)", OnImage = BitIconName.RewindTwoX, OffImage = BitIconName.Rewind },
        new() { Id = "play", OnName = "Resume", OffName = "Play", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
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

    private BitButtonGroupNameSelectors<Operation> multiNameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        IsToggled = { Name = nameof(Operation.IsSelected) }
    };

    private readonly string[] defaultKeys = ["bold"];
    private IEnumerable<string>? formatKeys = ["bold"];
    private List<Operation> formatCustoms =
    [
        new() { Id = "bold", Name = "Bold", Image = BitIconName.Bold },
        new() { Id = "italic", Name = "Italic", Image = BitIconName.Italic },
        new() { Id = "underline", Name = "Underline", Image = BitIconName.Underline }
    ];

    private List<Operation> maxToggleCustoms =
    [
        new() { Id = "bold", Name = "Bold", Image = BitIconName.Bold },
        new() { Id = "italic", Name = "Italic", Image = BitIconName.Italic },
        new() { Id = "underline", Name = "Underline", Image = BitIconName.Underline }
    ];

    private List<Operation> fixedToggleCustoms =
    [
        new() { Id = "bold", Name = "Bold", Image = BitIconName.Bold },
        new() { Id = "italic", Name = "Italic", Image = BitIconName.Italic },
        new() { Id = "underline", Name = "Underline", Image = BitIconName.Underline }
    ];

    private List<Operation> justifiedCustoms =
    [
        new() { Name = "Day" }, new() { Name = "Week" }, new() { Name = "A whole month" }
    ];

    private List<Operation> overflowCustoms =
    [
        new() { Name = "January" }, new() { Name = "February" }, new() { Name = "March" },
        new() { Name = "April" }, new() { Name = "May" }, new() { Name = "June" },
        new() { Name = "July" }, new() { Name = "August" }, new() { Name = "September" }
    ];

    private List<Operation> indicatorSingleCustoms =
    [
        new() { Id = "list", Name = "List", Image = BitIconName.BulletedList },
        new() { Id = "grid", Name = "Grid", Image = BitIconName.GridViewMedium },
        new() { Id = "tile", Name = "Tile", Image = BitIconName.Tiles }
    ];

    private readonly string[] indicatorDefaultKeys = ["name", "size"];
    private List<Operation> indicatorMultipleCustoms =
    [
        new() { Id = "name", Name = "Name" },
        new() { Id = "size", Name = "Size" },
        new() { Id = "date", Name = "Date" }
    ];

    private BitButtonGroupNameSelectors<Operation> loadingNameSelectors = new()
    {
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        IsLoading = { Selector = i => i.IsBusy }
    };
    private List<Operation> loadingCustoms =
    [
        new() { Name = "Save", Image = BitIconName.Save },
        new() { Name = "Sync", Image = BitIconName.Sync },
        new() { Name = "Publish", Image = BitIconName.PublishContent }
    ];

    private BitButtonGroupNameSelectors<Operation> badgeNameSelectors = new()
    {
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        Badge = { Selector = i => i.Count }
    };
    private List<Operation> badgeCustoms =
    [
        new() { Name = "Inbox", Image = BitIconName.Inbox, Count = "12" },
        new() { Name = "Drafts", Image = BitIconName.Edit, Count = "3" },
        new() { Name = "Sent", Image = BitIconName.Send }
    ];

    private BitButtonGroupNameSelectors<Operation> linkNameSelectors = new()
    {
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        Href = { Selector = i => i.Url },
        Target = { Selector = i => i.UrlTarget }
    };
    private List<Operation> linkCustoms =
    [
        new() { Name = "Home", Image = BitIconName.Home, Url = "/" },
        new() { Name = "Components", Image = BitIconName.Puzzle, Url = "/components" },
        new() { Name = "GitHub", Image = BitIconName.OpenInNewWindow, Url = "https://github.com/bitfoundation/bitplatform", UrlTarget = "_blank" }
    ];

    private BitButtonGroupNameSelectors<Operation> a11yNameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        AriaLabel = { Selector = i => i.Label },
        IsToggled = { Name = nameof(Operation.IsSelected) }
    };
    private List<Operation> a11yCustoms =
    [
        new() { Id = "start", Name = "Start", Image = BitIconName.AlignLeft, Label = "Align start" },
        new() { Id = "center", Name = "Center", Image = BitIconName.AlignCenter, Label = "Align center" },
        new() { Id = "end", Name = "End", Image = BitIconName.AlignRight, Label = "Align end" }
    ];

    private List<Operation> selectOnFocusCustoms =
    [
        new() { Id = "start", Name = "Start", Image = BitIconName.AlignLeft, Label = "Align start" },
        new() { Id = "center", Name = "Center", Image = BitIconName.AlignCenter, Label = "Align center" },
        new() { Id = "end", Name = "End", Image = BitIconName.AlignRight, Label = "Align end" }
    ];

    private List<Operation> fixedSingleCustoms =
    [
        new() { Id = "low", Name = "Low" },
        new() { Id = "medium", Name = "Medium" },
        new() { Id = "high", Name = "High" }
    ];

    private List<Operation> templateCustoms =
    [
        new() { Name = "Add", Image = BitIconName.Add },
        new() { Name = "Edit", Image = BitIconName.Edit },
        new() { Name = "Delete", Image = BitIconName.Delete }
    ];

    private BitButtonGroupNameSelectors<Operation> templateNameSelectors = new()
    {
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        Template = { Selector = i => i.Content }
    };
    // The Content of the middle item is a razor template, so it is authored in the .razor file
    // and attached to its item in OnInitialized below.
    private List<Operation> itemTemplateCustoms =
    [
        new() { Name = "Add", Image = BitIconName.Add },
        new() { Name = "Edit" },
        new() { Name = "Delete", Image = BitIconName.Delete }
    ];

    private BitButtonGroupNameSelectors<Operation> titleNameSelectors = new()
    {
        Text = { Selector = i => i.Name },
        IconName = { Selector = i => i.Image },
        Title = { Selector = i => i.Tooltip },
        AriaLabel = { Selector = i => i.Label }
    };
    private List<Operation> titleCustoms =
    [
        new() { Name = "Add", Image = BitIconName.Add, Tooltip = "Add a new record", Label = "Add" },
        new() { Name = "Edit", Image = BitIconName.Edit, Tooltip = "Edit the selected record", Label = "Edit" },
        new() { Name = "Delete", Image = BitIconName.Delete, Tooltip = "Delete the selected record", Label = "Delete" }
    ];

    private BitButtonGroupNameSelectors<Operation> toggleTitleNameSelectors = new()
    {
        Key = { Selector = i => i.Id },
        AriaLabel = { Selector = i => i.Label },
        OnText = { Selector = i => i.OnName },
        OffText = { Selector = i => i.OffName },
        OnTitle = { Selector = i => i.OnTitle },
        OffTitle = { Selector = i => i.OffTitle },
        OnIconName = { Selector = i => i.OnImage },
        OffIconName = { Selector = i => i.OffImage },
        IsToggled = { Name = nameof(Operation.IsSelected) }
    };
    private List<Operation> toggleTitleCustoms =
    [
        new()
        {
            Id = "mute",
            Label = "Mute",
            OnName = "Muted",
            OffName = "Mute",
            OnTitle = "The sound is muted, click to unmute",
            OffTitle = "Click to mute the sound",
            OnImage = BitIconName.Volume0,
            OffImage = BitIconName.Volume3
        },
        new()
        {
            Id = "repeat",
            Label = "Repeat",
            OnName = "Repeating",
            OffName = "Repeat",
            OnTitle = "Repeat is on, click to turn it off",
            OffTitle = "Click to repeat the playlist",
            OnImage = BitIconName.RepeatOne,
            OffImage = BitIconName.RepeatAll
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

        itemTemplateCustoms[1].Content = editItemTemplate;
    }

    private async Task HandleLoadingClick(Operation item)
    {
        item.IsBusy = true;
        StateHasChanged();

        await Task.Delay(2000);

        item.IsBusy = false;
        StateHasChanged();
    }
}
