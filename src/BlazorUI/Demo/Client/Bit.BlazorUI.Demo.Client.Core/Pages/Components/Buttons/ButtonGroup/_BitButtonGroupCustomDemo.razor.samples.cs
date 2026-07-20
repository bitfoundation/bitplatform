namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.ButtonGroup;

public partial class _BitButtonGroupCustomDemo
{
    private readonly string example1RazorCode = @"
<BitButtonGroup Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example1CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example2RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""disabledCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""disabledCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" IsEnabled=""false"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""disabledCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms""  NameSelectors=""nameSelector"" IsEnabled=""false"" />";
    private readonly string example2CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];

private List<Operation> disabledCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"", IsEnabled = false }, new() { Name = ""Delete"" }
];";

    private readonly string example3RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />";
    private readonly string example3CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
}

private List<Operation> iconCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add },
    new() { Name = ""Edit"", Image = BitIconName.Edit },
    new() { Name = ""Delete"", Image = BitIconName.Delete }
];";

    private readonly string example4RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" IconOnly />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" IconOnly />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""iconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" IconOnly />


<BitButtonGroup Variant=""BitVariant.Fill"" Items=""onlyIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""onlyIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""onlyIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />";
    private readonly string example4CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
}

private List<Operation> iconCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add },
    new() { Name = ""Edit"", Image = BitIconName.Edit },
    new() { Name = ""Delete"", Image = BitIconName.Delete }
];

private List<Operation> onlyIconCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add },
    new() { Image = BitIconName.Edit },
    new() { Name = ""Delete"", Image = BitIconName.Delete }
];";

    private readonly string example5RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""reversedIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                            IconName = { Selector = i => i.Image },
                                            ReversedIcon = { Selector = i => i.ReversedIcon } })"" />

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""reversedIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                            IconName = { Selector = i => i.Image },
                                            ReversedIcon = { Selector = i => i.ReversedIcon } })"" />

<BitButtonGroup Variant=""BitVariant.Text"" Items=""reversedIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                            IconName = { Selector = i => i.Image },
                                            ReversedIcon = { Selector = i => i.ReversedIcon } })"" />";
    private readonly string example5CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public bool ReversedIcon { get; set; }
}

private List<Operation> reversedIconCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add, ReversedIcon = true },
    new() { Name = ""Edit"", Image = BitIconName.Edit, ReversedIcon = true },
    new() { Name = ""Delete"", Image = BitIconName.Delete, ReversedIcon = true }
];";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Items=""toggledCustoms"" Variant=""BitVariant.Fill"" NameSelectors=""toggledNameSelectors"" />
<BitButtonGroup Toggle Items=""toggledCustoms"" Variant=""BitVariant.Outline"" NameSelectors=""toggledNameSelectors"" />
<BitButtonGroup Toggle Items=""toggledCustoms"" Variant=""BitVariant.Text"" NameSelectors=""toggledNameSelectors"" />

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""toggledCustoms"" NameSelectors=""toggledNameSelectors"" @bind-ToggleKey=""toggleKey"" />
<div>Toggle key: @toggleKey</div>
<BitButton OnClick=""@(() => toggleKey = ""forward"")"">Forward</BitButton>

<BitButtonGroup Toggle Variant=""BitVariant.Outline"" Items=""changeToggledCustoms"" NameSelectors=""toggledNameSelectors"" DefaultToggleKey=""forward"" OnToggleChange=""(Operation o) => onChangeToggleCustom = o"" />
<div>Changed toggle: @onChangeToggleCustom?.Id , @onChangeToggleCustom?.IsSelected</div>

<BitButtonGroup Toggle FixedToggle Variant=""BitVariant.Outline"" Items=""fixedSingleCustoms"" NameSelectors=""multiNameSelectors"" DefaultToggleKey=""medium"" />";
    private readonly string example6CsharpCode = @"
public class Operation
{
    public string? Id { get; set; }
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? OnImage { get; set; }
    public string? OffImage { get; set; }
    public string? OnName { get; set; }
    public string? OffName { get; set; }
    public string? OnTitle { get; set; }
    public string? OffTitle { get; set; }
    public bool ReversedIcon { get; set; }
    public bool IsSelected { get; set; }
}

private BitButtonGroupNameSelectors<Operation> multiNameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    IsToggled = { Name = nameof(Operation.IsSelected) }
};

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

private string? toggleKey = ""play"";
private List<Operation> toggledCustoms =
[
    new() { Id = ""back"", OnName = ""Back (2X)"", OffName = ""Back (1X)"", OnImage = BitIconName.RewindTwoX, OffImage = BitIconName.Rewind },
    new() { Id = ""play"", OnName = ""Resume"", OffName = ""Play"", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
    new() { Id = ""forward"", OnName = ""Forward (2X)"", OffName = ""Forward (1X)"", OnImage = BitIconName.FastForwardTwoX, OffImage = BitIconName.FastForward, ReversedIcon = true }
];

private Operation? onChangeToggleCustom;
private List<Operation> changeToggledCustoms =
[
    new() { Id = ""back"", OnName = ""Back (2X)"", OffName = ""Back (1X)"", OnImage = BitIconName.RewindTwoX, OffImage = BitIconName.Rewind },
    new() { Id = ""play"", OnName = ""Resume"", OffName = ""Play"", OnImage = BitIconName.PlayResume, OffImage = BitIconName.Play },
    new() { Id = ""forward"", OnName = ""Forward (2X)"", OffName = ""Forward (1X)"", OnImage = BitIconName.FastForwardTwoX, OffImage = BitIconName.FastForward, ReversedIcon = true }
];

private List<Operation> fixedSingleCustoms =
[
    new() { Id = ""low"", Name = ""Low"" },
    new() { Id = ""medium"", Name = ""Medium"" },
    new() { Id = ""high"", Name = ""High"" }
];";

    private readonly string example7RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />
<BitButtonGroup Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" Vertical />";
    private readonly string example7CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example8RazorCode = @"
<BitButtonGroup Items=""basicCustoms""
                NameSelectors=""nameSelector""
                OnItemClick=""(Operation item) => clickedCustom = item.Name"" />
<div>Clicked item: <b>@clickedCustom</b></div>

<BitButtonGroup Items=""eventsCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image },
                                         OnClick = { Selector = i => i.Clicked } })"" />
<div>Click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public Action<Operation>? Clicked { get; set; }
}

private int clickCounter;

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];

private List<Operation> eventsCustoms =
[
    new() { Name = ""Increase"", Image = BitIconName.Add },
    new() { Name = ""Reset"", Image = BitIconName.Reset },
    new() { Name = ""Decrease"", Image = BitIconName.Remove }
];

protected override void OnInitialized()
{
    eventsCustoms[0].Clicked = _ => { clickCounter++; StateHasChanged(); };
    eventsCustoms[1].Clicked = _ => { clickCounter = 0; StateHasChanged(); };
    eventsCustoms[2].Clicked = _ => { clickCounter--; StateHasChanged(); };
}";

    private readonly string example9RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup FullWidth Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example9CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example10RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                Items=""formatCustoms""
                NameSelectors=""multiNameSelectors""
                @bind-ToggleKeys=""formatKeys"" />
<div>Toggle keys: <b>@string.Join("", "", formatKeys ?? [])</b></div>

<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                MaxToggles=""2""
                Items=""maxToggleCustoms""
                NameSelectors=""multiNameSelectors""
                DefaultToggleKeys=""@defaultKeys"" />

<BitButtonGroup Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                FixedToggle
                Items=""fixedToggleCustoms""
                NameSelectors=""multiNameSelectors""
                DefaultToggleKeys=""@defaultKeys"" />";
    private readonly string example10CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> multiNameSelectors = new()
{
    Key = { Selector = i => i.Id },
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    IsToggled = { Name = nameof(Operation.IsSelected) }
};

private readonly string[] defaultKeys = [""bold""];
private IEnumerable<string>? formatKeys = [""bold""];
private List<Operation> formatCustoms =
[
    new() { Id = ""bold"", Name = ""Bold"", Image = BitIconName.Bold },
    new() { Id = ""italic"", Name = ""Italic"", Image = BitIconName.Italic },
    new() { Id = ""underline"", Name = ""Underline"", Image = BitIconName.Underline }
];";

    private readonly string example11RazorCode = @"
<BitButtonGroup FullWidth Variant=""BitVariant.Outline"" Items=""justifiedCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup FullWidth Justified Variant=""BitVariant.Outline"" Items=""justifiedCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example11CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

private List<Operation> justifiedCustoms =
[
    new() { Name = ""Day"" }, new() { Name = ""Week"" }, new() { Name = ""A whole month"" }
];";

    private readonly string example12RazorCode = @"
<BitButtonGroup Detached Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Detached Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Detached Gap=""1.5rem"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example12CsharpCode = @"
private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example13RazorCode = @"
<BitButtonGroup Rounded Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Rounded Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Rounded Detached Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Rounded Detached Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example13CsharpCode = @"
private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example14RazorCode = @"
<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Wrap""
                Items=""overflowCustoms""
                NameSelectors=""nameSelector"" />

<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Scroll""
                Items=""overflowCustoms""
                NameSelectors=""nameSelector"" />

<BitButtonGroup FullWidth
                Variant=""BitVariant.Outline""
                Overflow=""BitButtonGroupOverflow.Scrollbar""
                Items=""overflowCustoms""
                NameSelectors=""nameSelector"" />";
    private readonly string example14CsharpCode = @"
private List<Operation> overflowCustoms =
[
    new() { Name = ""January"" }, new() { Name = ""February"" }, new() { Name = ""March"" },
    new() { Name = ""April"" }, new() { Name = ""May"" }, new() { Name = ""June"" },
    new() { Name = ""July"" }, new() { Name = ""August"" }, new() { Name = ""September"" }
];";

    private readonly string example15RazorCode = @"
<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""indicatorSingleCustoms""
                NameSelectors=""multiNameSelectors""
                DefaultToggleKey=""list"" />

<BitButtonGroup ShowSelectionIndicator
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Multiple""
                Items=""indicatorMultipleCustoms""
                NameSelectors=""multiNameSelectors""
                DefaultToggleKeys=""@indicatorDefaultKeys"" />";
    private readonly string example15CsharpCode = @"
private List<Operation> indicatorSingleCustoms =
[
    new() { Id = ""list"", Name = ""List"", Image = BitIconName.BulletedList },
    new() { Id = ""grid"", Name = ""Grid"", Image = BitIconName.GridViewMedium },
    new() { Id = ""tile"", Name = ""Tile"", Image = BitIconName.Tiles }
];

private readonly string[] indicatorDefaultKeys = [""name"", ""size""];
private List<Operation> indicatorMultipleCustoms =
[
    new() { Id = ""name"", Name = ""Name"" },
    new() { Id = ""size"", Name = ""Size"" },
    new() { Id = ""date"", Name = ""Date"" }
];";

    private readonly string example16RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline""
                Items=""loadingCustoms""
                NameSelectors=""loadingNameSelectors""
                OnItemClick=""HandleLoadingClick"" />

<BitButtonGroup Variant=""BitVariant.Fill"" Items=""badgeCustoms"" NameSelectors=""badgeNameSelectors"" />
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""badgeCustoms"" NameSelectors=""badgeNameSelectors"" />";
    private readonly string example16CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> loadingNameSelectors = new()
{
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    IsLoading = { Selector = i => i.IsBusy }
};
private List<Operation> loadingCustoms =
[
    new() { Name = ""Save"", Image = BitIconName.Save },
    new() { Name = ""Sync"", Image = BitIconName.Sync },
    new() { Name = ""Publish"", Image = BitIconName.PublishContent }
];

private BitButtonGroupNameSelectors<Operation> badgeNameSelectors = new()
{
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    Badge = { Selector = i => i.Count }
};
private List<Operation> badgeCustoms =
[
    new() { Name = ""Inbox"", Image = BitIconName.Inbox, Count = ""12"" },
    new() { Name = ""Drafts"", Image = BitIconName.Edit, Count = ""3"" },
    new() { Name = ""Sent"", Image = BitIconName.Send }
];

private async Task HandleLoadingClick(Operation item)
{
    item.IsBusy = true;
    StateHasChanged();

    await Task.Delay(2000);

    item.IsBusy = false;
    StateHasChanged();
}";

    private readonly string example17RazorCode = @"
<BitButtonGroup Variant=""BitVariant.Outline"" Items=""linkCustoms"" NameSelectors=""linkNameSelectors"" />";
    private readonly string example17CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> linkNameSelectors = new()
{
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    Href = { Selector = i => i.Url },
    Target = { Selector = i => i.UrlTarget }
};
private List<Operation> linkCustoms =
[
    new() { Name = ""Home"", Image = BitIconName.Home, Url = ""/"" },
    new() { Name = ""Components"", Image = BitIconName.Puzzle, Url = ""/components"" },
    new() { Name = ""GitHub"", Image = BitIconName.OpenInNewWindow, Url = ""https://github.com/bitfoundation/bitplatform"", UrlTarget = ""_blank"" }
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


<BitButtonGroup Variant=""BitVariant.Outline"" Items=""templateCustoms"" NameSelectors=""nameSelector"">
    <ItemTemplate Context=""item"">
        <div class=""custom-template"">
            <BitIcon IconName=""@item.Image"" />
            <span>@item.Name</span>
        </div>
    </ItemTemplate>
</BitButtonGroup>

<BitButtonGroup Variant=""BitVariant.Outline"" Items=""itemTemplateCustoms"" NameSelectors=""templateNameSelectors"" />

@code {
    private readonly RenderFragment<Operation> editItemTemplate = item =>
    @<div class=""custom-template"">
        <BitIcon IconName=""@BitIconName.Edit"" Color=""BitColor.Warning"" />
        <b>@item.Name</b>
    </div>;
}";
    private readonly string example18CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public RenderFragment<Operation>? Content { get; set; }
}

private List<Operation> templateCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add },
    new() { Name = ""Edit"", Image = BitIconName.Edit },
    new() { Name = ""Delete"", Image = BitIconName.Delete }
];

private BitButtonGroupNameSelectors<Operation> templateNameSelectors = new()
{
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    Template = { Selector = i => i.Content }
};
private List<Operation> itemTemplateCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add },
    new() { Name = ""Edit"" },
    new() { Name = ""Delete"", Image = BitIconName.Delete }
];

protected override void OnInitialized()
{
    itemTemplateCustoms[1].Content = editItemTemplate;
}";

    private readonly string example19RazorCode = @"
<BitButtonGroup IconOnly Variant=""BitVariant.Outline"" Items=""titleCustoms"" NameSelectors=""titleNameSelectors"" />

<BitButtonGroup Toggle
                Variant=""BitVariant.Outline""
                Items=""toggleTitleCustoms""
                NameSelectors=""toggleTitleNameSelectors""
                DefaultToggleKey=""mute"" />";
    private readonly string example19CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> titleNameSelectors = new()
{
    Text = { Selector = i => i.Name },
    IconName = { Selector = i => i.Image },
    Title = { Selector = i => i.Tooltip },
    AriaLabel = { Selector = i => i.Label }
};
private List<Operation> titleCustoms =
[
    new() { Name = ""Add"", Image = BitIconName.Add, Tooltip = ""Add a new record"", Label = ""Add"" },
    new() { Name = ""Edit"", Image = BitIconName.Edit, Tooltip = ""Edit the selected record"", Label = ""Edit"" },
    new() { Name = ""Delete"", Image = BitIconName.Delete, Tooltip = ""Delete the selected record"", Label = ""Delete"" }
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
        Id = ""mute"",
        Label = ""Mute"",
        OnName = ""Muted"",
        OffName = ""Mute"",
        OnTitle = ""The sound is muted, click to unmute"",
        OffTitle = ""Click to mute the sound"",
        OnImage = BitIconName.Volume0,
        OffImage = BitIconName.Volume3
    },
    new()
    {
        Id = ""repeat"",
        Label = ""Repeat"",
        OnName = ""Repeating"",
        OffName = ""Repeat"",
        OnTitle = ""Repeat is on, click to turn it off"",
        OffTitle = ""Click to repeat the playlist"",
        OnImage = BitIconName.RepeatOne,
        OffImage = BitIconName.RepeatAll
    }
];";

    private readonly string example20RazorCode = @"
<BitButtonGroup AriaLabel=""Text alignment""
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""a11yCustoms""
                NameSelectors=""a11yNameSelectors""
                DefaultToggleKey=""start"" />

<BitButtonGroup AriaLabel=""Text alignment (selection follows focus)""
                SelectOnFocus
                Variant=""BitVariant.Outline""
                SelectionMode=""BitButtonGroupSelectionMode.Single""
                Items=""selectOnFocusCustoms""
                NameSelectors=""a11yNameSelectors""
                DefaultToggleKey=""start"" />

<BitButtonGroup AriaLabel=""Operations with a disabled button""
                DisabledInteractive
                Variant=""BitVariant.Outline""
                Items=""disabledCustoms""
                NameSelectors=""nameSelector"" />

<BitButtonGroup AriaLabel=""Operations""
                Navigable=""false""
                Variant=""BitVariant.Outline""
                Items=""basicCustoms""
                NameSelectors=""nameSelector"" />";
    private readonly string example20CsharpCode = @"
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
    new() { Id = ""start"", Name = ""Start"", Image = BitIconName.AlignLeft, Label = ""Align start"" },
    new() { Id = ""center"", Name = ""Center"", Image = BitIconName.AlignCenter, Label = ""Align center"" },
    new() { Id = ""end"", Name = ""End"", Image = BitIconName.AlignRight, Label = ""Align end"" }
];

private List<Operation> disabledCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"", IsEnabled = false }, new() { Name = ""Delete"" }
];";

    private readonly string example21RazorCode = @"
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />


<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example21CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];";

    private readonly string example22RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
    
<BitButtonGroup Variant=""BitVariant.Fill"" Items=""externalIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Icon = { Selector = i => i.IconInfo } })"" />
    
<BitButtonGroup Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Items=""externalIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Icon = { Selector = i => i.IconInfo } })"" />

<BitButtonGroup Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Items=""externalIconCustoms""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         Icon = { Selector = i => i.IconInfo } })"" />";
    private readonly string example22CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public BitIconInfo? IconInfo { get; set; }
}

private List<Operation> externalIconCustoms =
[
    new() { Name = ""Add"", IconInfo = ""fa-solid fa-plus"" },
    new() { Name = ""Edit"", IconInfo = BitIconInfo.Css(""fa-solid fa-pen"") },
    new() { Name = ""Delete"", IconInfo = BitIconInfo.Fa(""solid trash"") }
];";

    private readonly string example23RazorCode = @"
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />

<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Fill"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />
<BitButtonGroup Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""basicCustoms"" NameSelectors=""nameSelector"" />";
    private readonly string example23CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
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


<BitButtonGroup Items=""basicCustoms"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" NameSelectors=""nameSelector"" />
<BitButtonGroup Items=""basicCustoms"" Class=""custom-class"" Variant=""BitVariant.Outline"" NameSelectors=""nameSelector"" />

<BitButtonGroup Items=""styleClassCustoms""
                Variant=""BitVariant.Text""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Items=""basicCustoms""
                Variant=""BitVariant.Text""
                NameSelectors=""nameSelector""
                Styles=""@(new() { Button = ""color: darkcyan; border-color: deepskyblue; background-color: azure;"" })"" />

<BitButtonGroup Items=""basicCustoms""
                Variant=""BitVariant.Text""
                NameSelectors=""nameSelector""
                Classes=""@(new() { Button = ""custom-btn"" })"" />";
    private readonly string example24CsharpCode = @"
private BitButtonGroupNameSelectors<Operation> nameSelector = new() { Text = { Selector = i => i.Name } };

public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<Operation> basicCustoms =
[
    new() { Name = ""Add"" }, new() { Name = ""Edit"" }, new() { Name = ""Delete"" }
];

private List<Operation> styleClassCustoms =
[
    new()
    {
        Name = ""Styled"",
        Style = ""color: tomato; border-color: brown; background-color: peachpuff;"",
        Image = BitIconName.Brush,
    },
    new()
    {
        Name = ""Classed"",
        Class = ""custom-item"",
        Image = BitIconName.FormatPainter,
    }
];";

    private readonly string example25RazorCode = @"
<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Fill""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Outline""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />

<BitButtonGroup Dir=""BitDir.Rtl""
                Items=""rtlCustoms""
                Variant=""BitVariant.Text""
                NameSelectors=""@(new() { Text = { Selector = i => i.Name },
                                         IconName = { Selector = i => i.Image } })"" />";
    private readonly string example25CsharpCode = @"
public class Operation
{
    public string? Name { get; set; }
    public string? Image { get; set; }
}

private List<Operation> rtlCustoms =
[
    new() { Name = ""اضافه کردن"", Image = BitIconName.Add },
    new() { Name = ""ویرایش"", Image = BitIconName.Edit },
    new() { Name = ""حذف"", Image = BitIconName.Delete }
];";
}
