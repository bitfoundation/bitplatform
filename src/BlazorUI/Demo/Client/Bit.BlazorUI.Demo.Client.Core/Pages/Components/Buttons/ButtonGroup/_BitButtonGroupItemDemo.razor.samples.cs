namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupItemDemo
{
    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""disabledItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" IsEnabled=""false"" />";
    private readonly string example2CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];

private List<BitButtonGroupItem> disabledItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"", IsEnabled = false }, new() { Text = ""Delete"" }
];";

    private readonly string example3RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitButtonGroupItem> iconItems = 
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconItems"" IconOnly />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconItems"" IconOnly />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconItems"" IconOnly />

<BitButtonGroup Variant=""BitVariant.Fill"" Items=""onlyIconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""onlyIconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""onlyIconItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitButtonGroupItem> iconItems = 
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];

private List<BitButtonGroupItem> onlyIconItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""reversedIconItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""reversedIconItems"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""reversedIconItems"" />";
    private readonly string example5CsharpCode = @"
private List<BitButtonGroupItem> reversedIconItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add, ReversedIcon = true },
    new() { Text = ""Edit"", IconName = BitIconName.Edit, ReversedIcon = true },
    new() { Text = ""Delete"", IconName = BitIconName.Delete, ReversedIcon = true }
];";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Variant=""BitVariant.Fill"" Items=""toggledItems"" />
<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""toggledItems"" />
<BitButtonGroup Toggle Variant=""BitVariant.Text"" Items=""toggledItems"" />

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""toggledItems"" @bind-ToggleKey=""toggleKey"" />
<div>Toggle key: @toggleKey</div>
<BitButton OnClick=""@(() => toggleKey = ""forward"")"">Forward</BitButton>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""changeToggledItems"" DefaultToggleKey=""forward"" OnToggleChange=""(BitButtonGroupItem i) => onChangeToggleItem = i"" />
<div>Changed toggle: @onChangeToggleItem?.Key , @onChangeToggleItem?.IsToggled</div>

<BitButtonGroup Toggle FixedToggle Variant=""BitVariant.Outline"" Items=""fixedSingleItems"" DefaultToggleKey=""medium"" />";
    private readonly string example6CsharpCode = @"
private string? toggleKey = ""play"";
private List<BitButtonGroupItem> toggledItems =
[
    new() { Key = ""back"", OnText = ""Back (2X)"", OffText = ""Back (1X)"", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
    new() { Key = ""play"", OnText = ""Resume"", OffText = ""Play"", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
    new() { Key = ""forward"", OnText = ""Forward (2X)"", OffText = ""Forward (1X)"", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
];

private BitButtonGroupItem? onChangeToggleItem;
private List<BitButtonGroupItem> changeToggledItems =
[
    new() { Key = ""back"", OnText = ""Back (2X)"", OffText = ""Back (1X)"", OnIconName = BitIconName.RewindTwoX, OffIconName = BitIconName.Rewind },
    new() { Key = ""play"", OnText = ""Resume"", OffText = ""Play"", OnIconName = BitIconName.PlayResume, OffIconName = BitIconName.Play },
    new() { Key = ""forward"", OnText = ""Forward (2X)"", OffText = ""Forward (1X)"", OnIconName = BitIconName.FastForwardTwoX, OffIconName = BitIconName.FastForward, ReversedIcon = true }
];

private List<BitButtonGroupItem> fixedSingleItems =
[
    new() { Key = ""low"", Text = ""Low"" },
    new() { Key = ""medium"", Text = ""Medium"" },
    new() { Key = ""high"", Text = ""High"" }
];";

    private readonly string example7RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicItems"" Vertical />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicItems"" Vertical />";
    private readonly string example7CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example8RazorCode = @"
<BitButtonGroup Items=""basicItems"" OnItemClick=""(BitButtonGroupItem item) => clickedItem = item.Text"" />
<div>Clicked item: <b>@clickedItem</b></div>

<BitButtonGroup Items=""eventsItems"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? clickedItem;

private List<BitButtonGroupItem> eventsItems =
[
    new() { Text = ""Increase"", IconName = BitIconName.Add },
    new() { Text = ""Reset"", IconName = BitIconName.Reset },
    new() { Text = ""Decrease"", IconName = BitIconName.Remove }
];

protected override void OnInitialized()
{
    eventsItems[0].OnClick = _ => { clickCounter++; StateHasChanged(); };
    eventsItems[1].OnClick = _ => { clickCounter = 0; StateHasChanged(); };
    eventsItems[2].OnClick = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example9RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example10RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                Items=""formatItems""
                @bind-ToggleKeys=""formatKeys"" />
<div>Toggle keys: <b>@string.Join("", "", formatKeys ?? [])</b></div>

<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                MaxToggles=""2""
                Items=""maxToggleItems""
                DefaultToggleKeys=""@maxDefaultKeys"" />

<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                FixedToggle
                Items=""fixedToggleItems""
                DefaultToggleKeys=""@fixedDefaultKeys"" />";
    private readonly string example10CsharpCode = @"
private IEnumerable<string>? formatKeys = [""bold""];
private List<BitButtonGroupItem> formatItems =
[
    new() { Key = ""bold"", Text = ""Bold"", IconName = BitIconName.Bold },
    new() { Key = ""italic"", Text = ""Italic"", IconName = BitIconName.Italic },
    new() { Key = ""underline"", Text = ""Underline"", IconName = BitIconName.Underline }
];

private readonly string[] maxDefaultKeys = [""bold""];
private List<BitButtonGroupItem> maxToggleItems =
[
    new() { Key = ""bold"", Text = ""Bold"", IconName = BitIconName.Bold },
    new() { Key = ""italic"", Text = ""Italic"", IconName = BitIconName.Italic },
    new() { Key = ""underline"", Text = ""Underline"", IconName = BitIconName.Underline }
];

private readonly string[] fixedDefaultKeys = [""bold""];
private List<BitButtonGroupItem> fixedToggleItems =
[
    new() { Key = ""bold"", Text = ""Bold"", IconName = BitIconName.Bold },
    new() { Key = ""italic"", Text = ""Italic"", IconName = BitIconName.Italic },
    new() { Key = ""underline"", Text = ""Underline"", IconName = BitIconName.Underline }
];";

    private readonly string example11RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" Items=""justifiedItems"" />

<BitButtonGroup FullWidth Justified Variant=""BitVariant.Outline"" Items=""justifiedItems"" />";
    private readonly string example11CsharpCode = @"
private List<BitButtonGroupItem> justifiedItems =
[
    new() { Text = ""Day"" }, new() { Text = ""Week"" }, new() { Text = ""A whole month"" }
];";

    private readonly string example12RazorCode = @"
<BitButtonGroup Detached Variant=""BitVariant.Fill"" Items=""basicItems"" />

<BitButtonGroup Detached Variant=""BitVariant.Outline"" Items=""basicItems"" />

<BitButtonGroup Detached Gap=""1.5rem"" Variant=""BitVariant.Outline"" Items=""basicItems"" />";
    private readonly string example12CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example13RazorCode = @"
<BitButtonGroup Rounded Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Rounded Variant=""BitVariant.Outline"" Items=""basicItems"" />

<BitButtonGroup Rounded Detached Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Rounded Detached Variant=""BitVariant.Outline"" Items=""basicItems"" />";
    private readonly string example13CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example14RazorCode = @"
<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Wrap""
                Items=""overflowItems"" />

<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Scroll""
                Items=""overflowItems"" />

<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Scrollbar""
                Items=""overflowItems"" />";
    private readonly string example14CsharpCode = @"
private List<BitButtonGroupItem> overflowItems =
[
    new() { Text = ""January"" }, new() { Text = ""February"" }, new() { Text = ""March"" },
    new() { Text = ""April"" }, new() { Text = ""May"" }, new() { Text = ""June"" },
    new() { Text = ""July"" }, new() { Text = ""August"" }, new() { Text = ""September"" }
];";

    private readonly string example15RazorCode = @"
<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""indicatorSingleItems""
                DefaultToggleKey=""list"" />

<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                Items=""indicatorMultipleItems""
                DefaultToggleKeys=""@indicatorDefaultKeys"" />";
    private readonly string example15CsharpCode = @"
private List<BitButtonGroupItem> indicatorSingleItems =
[
    new() { Key = ""list"", Text = ""List"", IconName = BitIconName.BulletedList },
    new() { Key = ""grid"", Text = ""Grid"", IconName = BitIconName.GridViewMedium },
    new() { Key = ""tile"", Text = ""Tile"", IconName = BitIconName.Tiles }
];

private readonly string[] indicatorDefaultKeys = [""name"", ""size""];
private List<BitButtonGroupItem> indicatorMultipleItems =
[
    new() { Key = ""name"", Text = ""Name"" },
    new() { Key = ""size"", Text = ""Size"" },
    new() { Key = ""date"", Text = ""Date"" }
];";

    private readonly string example16RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""loadingItems"" OnItemClick=""HandleLoadingClick"" />

<BitButtonGroup Variant=""BitVariant.Fill"" Items=""badgeItems"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""badgeItems"" />";
    private readonly string example16CsharpCode = @"
private List<BitButtonGroupItem> loadingItems =
[
    new() { Key = ""save"", Text = ""Save"", IconName = BitIconName.Save },
    new() { Key = ""sync"", Text = ""Sync"", IconName = BitIconName.Sync },
    new() { Key = ""publish"", Text = ""Publish"", IconName = BitIconName.PublishContent }
];

private List<BitButtonGroupItem> badgeItems =
[
    new() { Text = ""Inbox"", IconName = BitIconName.Inbox, Badge = ""12"" },
    new() { Text = ""Drafts"", IconName = BitIconName.Edit, Badge = ""3"" },
    new() { Text = ""Sent"", IconName = BitIconName.Send }
];

private async Task HandleLoadingClick(BitButtonGroupItem item)
{
    item.IsLoading = true;
    StateHasChanged();

    await Task.Delay(2000);

    item.IsLoading = false;
    StateHasChanged();
}";

    private readonly string example17RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""linkItems"" />";
    private readonly string example17CsharpCode = @"
private List<BitButtonGroupItem> linkItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home, Href = ""/"" },
    new() { Text = ""Components"", IconName = BitIconName.Puzzle, Href = ""/components"" },
    new() { Text = ""GitHub"", IconName = BitIconName.OpenInNewWindow, Href = ""https://github.com/bitfoundation/bitplatform"", Target = ""_blank"" }
];";

    private readonly string example18RazorCode = @"
<style>
    .custom-template {
        gap: 0.25rem;
        display: flex;
        align-items: center;
        flex-flow: column nowrap;
    }
</style>


<BitButtonGroup Variant=""BitVariant.Outline"" Items=""templateItems"">
    <ItemTemplate Context=""item"">
        <div class=""custom-template"">
            <BitIcon IconName=""@item.IconName"" />
            <span>@item.Text</span>
        </div>
    </ItemTemplate>
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""itemTemplateItems"" />

@code {
    private readonly RenderFragment<BitButtonGroupItem> editItemTemplate = item =>
    @<div class=""custom-template"">
        <BitIcon IconName=""@BitIconName.Edit"" Color=""BitColor.Warning"" />
        <b>@item.Text</b>
    </div>;
}";
    private readonly string example18CsharpCode = @"
private List<BitButtonGroupItem> templateItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];

private List<BitButtonGroupItem> itemTemplateItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add },
    new() { Text = ""Edit"" },
    new() { Text = ""Delete"", IconName = BitIconName.Delete }
];

protected override void OnInitialized()
{
    itemTemplateItems[1].Template = editItemTemplate;
}";

    private readonly string example19RazorCode = @"
<BitButtonGroup IconOnly Variant=""BitVariant.Outline"" Items=""titleItems"" />

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""toggleTitleItems"" DefaultToggleKey=""mute"" />";
    private readonly string example19CsharpCode = @"
private List<BitButtonGroupItem> titleItems =
[
    new() { Text = ""Add"", IconName = BitIconName.Add, Title = ""Add a new record"", AriaLabel = ""Add"" },
    new() { Text = ""Edit"", IconName = BitIconName.Edit, Title = ""Edit the selected record"", AriaLabel = ""Edit"" },
    new() { Text = ""Delete"", IconName = BitIconName.Delete, Title = ""Delete the selected record"", AriaLabel = ""Delete"" }
];

private List<BitButtonGroupItem> toggleTitleItems =
[
    new()
    {
        Key = ""mute"",
        AriaLabel = ""Mute"",
        OnText = ""Muted"",
        OffText = ""Mute"",
        OnTitle = ""The sound is muted, click to unmute"",
        OffTitle = ""Click to mute the sound"",
        OnIconName = BitIconName.Volume0,
        OffIconName = BitIconName.Volume3
    },
    new()
    {
        Key = ""repeat"",
        AriaLabel = ""Repeat"",
        OnText = ""Repeating"",
        OffText = ""Repeat"",
        OnTitle = ""Repeat is on, click to turn it off"",
        OffTitle = ""Click to repeat the playlist"",
        OnIconName = BitIconName.RepeatOne,
        OffIconName = BitIconName.RepeatAll
    }
];";

    private readonly string example20RazorCode = @"
<BitButtonGroup AriaLabel=""Text alignment""
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""a11yItems""
                DefaultToggleKey=""start"" />

<BitButtonGroup AriaLabel=""Text alignment (selection follows focus)""
                SelectOnFocus
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""selectOnFocusItems""
                DefaultToggleKey=""start"" />

<BitButtonGroup AriaLabel=""Operations with a disabled button""
                DisabledInteractive
                Variant=""BitVariant.Outline""
                Items=""disabledItems"" />

<BitButtonGroup AriaLabel=""Operations""
                Navigable=""false""
                Variant=""BitVariant.Outline""
                Items=""basicItems"" />";
    private readonly string example20CsharpCode = @"
private List<BitButtonGroupItem> a11yItems =
[
    new() { Key = ""start"", Text = ""Start"", IconName = BitIconName.AlignLeft, AriaLabel = ""Align start"" },
    new() { Key = ""center"", Text = ""Center"", IconName = BitIconName.AlignCenter, AriaLabel = ""Align center"" },
    new() { Key = ""end"", Text = ""End"", IconName = BitIconName.AlignRight, AriaLabel = ""Align end"" }
];

private List<BitButtonGroupItem> selectOnFocusItems =
[
    new() { Key = ""start"", Text = ""Start"", IconName = BitIconName.AlignLeft, AriaLabel = ""Align start"" },
    new() { Key = ""center"", Text = ""Center"", IconName = BitIconName.AlignCenter, AriaLabel = ""Align center"" },
    new() { Key = ""end"", Text = ""End"", IconName = BitIconName.AlignRight, AriaLabel = ""Align end"" }
];

private List<BitButtonGroupItem> disabledItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"", IsEnabled = false }, new() { Text = ""Delete"" }
];

private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example21RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" Items=""basicItems"" />


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example21CsharpCode = @"
private List<BitButtonGroupItem> basicItems = 
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
    
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""externalIconItems"" />
    
<BitButtonGroup Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Items=""externalIconItems"" />

<BitButtonGroup Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Items=""externalIconItems"" />";
    private readonly string example22CsharpCode = @"
private List<BitButtonGroupItem> externalIconItems =
[
    new() { Text = ""Add"", Icon = ""fa-solid fa-plus"" },
    new() { Text = ""Edit"", Icon = BitIconInfo.Css(""fa-solid fa-pen"") },
    new() { Text = ""Delete"", Icon = BitIconInfo.Fa(""solid trash"") }
];";

    private readonly string example23RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""basicItems"" />

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""basicItems"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""basicItems"" />";
    private readonly string example23CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];";

    private readonly string example24RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class button {
        color: tomato;
        border-color: tomato;
    }

    .custom-class button:hover {
        color: unset;
        background-color: lightcoral;
    }

    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-btn {
        color: aliceblue;
        border-color: aliceblue;
        background-color: crimson;
    }
</style>


<BitButtonGroup Items=""basicItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" />
<BitButtonGroup Items=""basicItems"" Class=""custom-class"" Variant=""BitVariant.Outline"" />

<BitButtonGroup Items=""styleClassItems"" Variant=""BitVariant.Text"" />

<BitButtonGroup Items=""basicItems""
                Variant=""BitVariant.Text""
                Styles=""@(new() { Button = ""color: darkcyan; border-color: deepskyblue; background-color: azure;"" })"" />

<BitButtonGroup Items=""basicItems""
                Variant=""BitVariant.Text""
                Classes=""@(new() { Button = ""custom-btn"" })"" />";
    private readonly string example24CsharpCode = @"
private List<BitButtonGroupItem> basicItems =
[
    new() { Text = ""Add"" }, new() { Text = ""Edit"" }, new() { Text = ""Delete"" }
];

private List<BitButtonGroupItem> styleClassItems =
[
    new()
    {
        Text = ""Styled"",
        Style = ""color: tomato; border-color: brown; background-color: peachpuff;"",
        IconName = BitIconName.Brush,
    },
    new()
    {
        Text = ""Classed"",
        Class = ""custom-item"",
        IconName = BitIconName.FormatPainter,
    }
];";

    private readonly string example25RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Fill"" Items=""rtlItems"" />

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Outline"" Items=""rtlItems"" />

<BitButtonGroup Dir=""BitDir.Rtl"" Variant=""BitVariant.Text"" Items=""rtlItems"" />";
    private readonly string example25CsharpCode = @"
private List<BitButtonGroupItem> rtlItems =
[
    new() { Text = ""اضافه کردن"", IconName = BitIconName.Add },
    new() { Text = ""ویرایش"", IconName = BitIconName.Edit },
    new() { Text = ""حذف"", IconName = BitIconName.Delete }
];";
}
