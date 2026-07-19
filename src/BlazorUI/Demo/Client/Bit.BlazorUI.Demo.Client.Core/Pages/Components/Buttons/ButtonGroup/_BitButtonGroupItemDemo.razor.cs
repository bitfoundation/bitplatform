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
        new() { Key = "play", OnText = "Resume", OffText = "Play", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
        new() { Key = "forward", OnText = "Forward (2X)", OffText = "Forward (1X)", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
    ];

    private BitButtonGroupItem? onChangeToggleItem;
    private List<BitButtonGroupItem> changeToggledItems =
    [
        new() { Key = "back", OnText = "Back (2X)", OffText = "Back (1X)", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
        new() { Key = "play", OnText = "Resume", OffText = "Play", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
        new() { Key = "forward", OnText = "Forward (2X)", OffText = "Forward (1X)", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
    ];

    private List<BitButtonGroupItem> fixedSingleItems =
    [
        new() { Key = "low", Text = "Low" },
        new() { Key = "medium", Text = "Medium" },
        new() { Key = "high", Text = "High" }
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

    private IEnumerable<string>? formatKeys = ["bold"];
    private List<BitButtonGroupItem> formatItems =
    [
        new() { Key = "bold", Text = "Bold", IconName = BitIconName.Bold },
        new() { Key = "italic", Text = "Italic", IconName = BitIconName.Italic },
        new() { Key = "underline", Text = "Underline", IconName = BitIconName.Underline }
    ];

    private readonly string[] maxDefaultKeys = ["bold"];
    private List<BitButtonGroupItem> maxToggleItems =
    [
        new() { Key = "bold", Text = "Bold", IconName = BitIconName.Bold },
        new() { Key = "italic", Text = "Italic", IconName = BitIconName.Italic },
        new() { Key = "underline", Text = "Underline", IconName = BitIconName.Underline }
    ];

    private readonly string[] fixedDefaultKeys = ["bold"];
    private List<BitButtonGroupItem> fixedToggleItems =
    [
        new() { Key = "bold", Text = "Bold", IconName = BitIconName.Bold },
        new() { Key = "italic", Text = "Italic", IconName = BitIconName.Italic },
        new() { Key = "underline", Text = "Underline", IconName = BitIconName.Underline }
    ];

    private List<BitButtonGroupItem> justifiedItems =
    [
        new() { Text = "Day" }, new() { Text = "Week" }, new() { Text = "A whole month" }
    ];

    private List<BitButtonGroupItem> overflowItems =
    [
        new() { Text = "January" }, new() { Text = "February" }, new() { Text = "March" },
        new() { Text = "April" }, new() { Text = "May" }, new() { Text = "June" },
        new() { Text = "July" }, new() { Text = "August" }, new() { Text = "September" }
    ];

    private List<BitButtonGroupItem> indicatorSingleItems =
    [
        new() { Key = "list", Text = "List", IconName = BitIconName.BulletedList },
        new() { Key = "grid", Text = "Grid", IconName = BitIconName.GridViewMedium },
        new() { Key = "tile", Text = "Tile", IconName = BitIconName.Tiles }
    ];

    private readonly string[] indicatorDefaultKeys = ["name", "size"];
    private List<BitButtonGroupItem> indicatorMultipleItems =
    [
        new() { Key = "name", Text = "Name" },
        new() { Key = "size", Text = "Size" },
        new() { Key = "date", Text = "Date" }
    ];

    private List<BitButtonGroupItem> loadingItems =
    [
        new() { Key = "save", Text = "Save", IconName = BitIconName.Save },
        new() { Key = "sync", Text = "Sync", IconName = BitIconName.Sync },
        new() { Key = "publish", Text = "Publish", IconName = BitIconName.PublishContent }
    ];

    private List<BitButtonGroupItem> badgeItems =
    [
        new() { Text = "Inbox", IconName = BitIconName.Inbox, Badge = "12" },
        new() { Text = "Drafts", IconName = BitIconName.Edit, Badge = "3" },
        new() { Text = "Sent", IconName = BitIconName.Send }
    ];

    private List<BitButtonGroupItem> linkItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home, Href = "/" },
        new() { Text = "Components", IconName = BitIconName.Puzzle, Href = "/components" },
        new() { Text = "GitHub", IconName = BitIconName.OpenInNewWindow, Href = "https://github.com/bitfoundation/bitplatform", Target = "_blank" }
    ];

    private List<BitButtonGroupItem> a11yItems =
    [
        new() { Key = "start", Text = "Start", IconName = BitIconName.AlignLeft, AriaLabel = "Align start" },
        new() { Key = "center", Text = "Center", IconName = BitIconName.AlignCenter, AriaLabel = "Align center" },
        new() { Key = "end", Text = "End", IconName = BitIconName.AlignRight, AriaLabel = "Align end" }
    ];

    private List<BitButtonGroupItem> selectOnFocusItems =
    [
        new() { Key = "start", Text = "Start", IconName = BitIconName.AlignLeft, AriaLabel = "Align start" },
        new() { Key = "center", Text = "Center", IconName = BitIconName.AlignCenter, AriaLabel = "Align center" },
        new() { Key = "end", Text = "End", IconName = BitIconName.AlignRight, AriaLabel = "Align end" }
    ];

    private List<BitButtonGroupItem> templateItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { Text = "Edit", IconName = BitIconName.Edit },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    ];

    // The Template of the middle item is a razor template, so it is authored in the .razor file
    // and attached to its item in OnInitialized below.
    private List<BitButtonGroupItem> itemTemplateItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add },
        new() { Text = "Edit" },
        new() { Text = "Delete", IconName = BitIconName.Delete }
    ];

    private List<BitButtonGroupItem> titleItems =
    [
        new() { Text = "Add", IconName = BitIconName.Add, Title = "Add a new record", AriaLabel = "Add" },
        new() { Text = "Edit", IconName = BitIconName.Edit, Title = "Edit the selected record", AriaLabel = "Edit" },
        new() { Text = "Delete", IconName = BitIconName.Delete, Title = "Delete the selected record", AriaLabel = "Delete" }
    ];

    private List<BitButtonGroupItem> toggleTitleItems =
    [
        new()
        {
            Key = "mute",
            AriaLabel = "Mute",
            OnText = "Muted",
            OffText = "Mute",
            OnTitle = "The sound is muted, click to unmute",
            OffTitle = "Click to mute the sound",
            OnIconName = BitIconName.Volume0,
            OffIconName = BitIconName.Volume3
        },
        new()
        {
            Key = "repeat",
            AriaLabel = "Repeat",
            OnText = "Repeating",
            OffText = "Repeat",
            OnTitle = "Repeat is on, click to turn it off",
            OffTitle = "Click to repeat the playlist",
            OnIconName = BitIconName.RepeatOne,
            OffIconName = BitIconName.RepeatAll
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

        itemTemplateItems[1].Template = editItemTemplate;
    }

    private async Task HandleLoadingClick(BitButtonGroupItem item)
    {
        item.IsLoading = true;
        StateHasChanged();

        await Task.Delay(2000);

        item.IsLoading = false;
        StateHasChanged();
    }
}
