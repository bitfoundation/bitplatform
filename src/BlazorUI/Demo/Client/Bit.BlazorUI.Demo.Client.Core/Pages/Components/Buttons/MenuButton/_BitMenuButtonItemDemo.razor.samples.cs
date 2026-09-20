namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonItemDemo
{
    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" Items=""basicItems"" />";

    private readonly string example1CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" Items=""basicItems"" Split />";

    private readonly string example2CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" IsEnabled=""false"" />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" IsEnabled=""false"" />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" Split />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" Split />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" Split />

<BitMenuButton Text=""Fill"" Items=""basicItems"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Outline"" Items=""basicItems"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Text"" Items=""basicItems"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split />";

    private readonly string example3CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example4RazorCode = @"
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Fill"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Fill"" Split Sticky />

<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Outline"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Outline"" Split Sticky />

<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Text"" Sticky />
<BitMenuButton Items=""basicItems"" Variant=""BitVariant.Text"" Split Sticky />";

    private readonly string example4CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""IconName"" Items=""basicItemsIcon"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""ChevronDownIconName"" Items=""basicItemsIcon"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"" Split />

<BitMenuButton Text=""No icon"" Items=""basicItemsIcon"" IconName=""@BitIconName.Edit"" NoIcon />
<BitMenuButton Sticky Items=""basicItemsIcon"" NoIcon />";

    private readonly string example5CsharpCode = @"
private List<BitMenuButtonItem> basicItemsIcon =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];";

    private readonly string example6RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicItems"" Background=""BitColorKind.Primary"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Background=""BitColorKind.Secondary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Background=""BitColorKind.Tertiary"" />
<BitMenuButton Text=""Transparent"" Items=""basicItems"" Background=""BitColorKind.Transparent"" />

<BitMenuButton Text=""Scrolling menu"" Items=""longItems"" MaxHeight=""10rem"" />";

    private readonly string example6CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private List<BitMenuButtonItem> longItems =
    Enumerable.Range(1, 20).Select(i => new BitMenuButtonItem { Text = $""Item {i}"", Key = i.ToString() }).ToList();";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""Toggle"" Items=""basicItems"" Split Toggle />

<BitMenuButton Text=""DefaultIsToggled"" Items=""basicItems"" Split Toggle DefaultIsToggled=""true"" />

<BitMenuButton Text=""Two-way"" Items=""basicItems"" Split Toggle @bind-IsToggled=""itemIsToggled"" />
<BitCheckbox Label=""IsToggled"" @bind-Value=""itemIsToggled"" />

<div>OnToggleChange: @itemToggledValue</div>
<BitMenuButton Text=""OnToggleChange"" Items=""basicItems"" Split Toggle OnToggleChange=""v => itemToggledValue = v"" />";

    private readonly string example7CsharpCode = @"
private bool itemIsToggled;
private bool itemToggledValue;

private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example8RazorCode = @"
<BitMenuButton Text=""Columns"" Items=""checkableItems"" IconName=""@BitIconName.ColumnVerticalSection""
               CloseOnItemClick=""false"" OnClick=""(BitMenuButtonItem _) => StateHasChanged()"" />

<BitMenuButton Text=""Closes on click"" Items=""checkableItems2"" Variant=""BitVariant.Outline""
               CheckIconName=""@BitIconName.CheckboxCompositeReversed"" />

<BitMenuButton Text=""Sort by"" Items=""sortItems"" Variant=""BitVariant.Text"" IconName=""@BitIconName.Sort""
               OnClick=""(BitMenuButtonItem _) => StateHasChanged()"" />

<div>
    Visible columns: @string.Join("", "", checkableItems.Where(i => i.IsChecked).Select(i => i.Text)),
    sorted by @(sortItems.FirstOrDefault(i => i.IsChecked)?.Text)
</div>";

    private readonly string example8CsharpCode = @"
private List<BitMenuButtonItem> checkableItems =
[
    new() { Text = ""Name"", Key = ""name"", Checkable = true, IsChecked = true },
    new() { Text = ""Status"", Key = ""status"", Checkable = true, IsChecked = true },
    new() { Text = ""Owner"", Key = ""owner"", Checkable = true },
    new() { Text = ""Reset to defaults"", Key = ""reset"", IconName = BitIconName.Refresh }
];

private List<BitMenuButtonItem> checkableItems2 =
[
    new() { Text = ""Wrap lines"", Key = ""wrap"", Checkable = true, IsChecked = true },
    new() { Text = ""Show whitespace"", Key = ""whitespace"", Checkable = true }
];

private List<BitMenuButtonItem> sortItems =
[
    new() { Text = ""Name"", Key = ""name"", RadioGroup = ""sort"", IsChecked = true },
    new() { Text = ""Date modified"", Key = ""date"", RadioGroup = ""sort"" },
    new() { Text = ""Size"", Key = ""size"", RadioGroup = ""sort"" }
];

protected override void OnInitialized()
{
    checkableItems[^1].OnClick = _ =>
    {
        checkableItems[0].IsChecked = true;
        checkableItems[1].IsChecked = true;
        checkableItems[2].IsChecked = false;
    };
}";

    private readonly string example9RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton Items=""basicItems"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
</BitMenuButton>

<BitMenuButton Text=""Items"" Items=""itemTemplateItems"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Text (@item.Key)</span>
        </div>
    </ItemTemplate>
</BitMenuButton>

<BitMenuButton Text=""Items"" Items=""itemTemplateItems2"" />";

    private readonly string example9CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private List<BitMenuButtonItem> itemTemplateItems =
[
    new() { Text = ""Add"", Key = ""add-key"", IconName = BitIconName.Add },
    new() { Text = ""Edit"", Key = ""edit-key"", IconName = BitIconName.Edit },
    new() { Text = ""Delete"", Key = ""delete-key"", IconName = BitIconName.Delete }
];

private List<BitMenuButtonItem> itemTemplateItems2 = 
[
    new()
    {
        Text = ""Add"", Key = ""add-key"", IconName = BitIconName.Add,
        Template = (item => @<div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div>)
    },
    new ()
    {
        Text = ""Edit"", Key = ""edit-key"", IconName = BitIconName.Edit,
        Template = (item => @<div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div>)
    },
    new()
    {
        Text = ""Delete"", Key = ""delete-key"", IconName = BitIconName.Delete,
        Template = (item => @<div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div>)
    }
];";

    private readonly string example10RazorCode = @"
<BitMenuButton Text=""Items"" Items=""basicItems""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />

<BitMenuButton Split Text=""Items"" Items=""basicItemsOnClick""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""@((BitMenuButtonItem item) => eventsClickedItem = ""Main button clicked"")"" />


<BitMenuButton Sticky Items=""basicItems""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />

<BitMenuButton Sticky Split Items=""basicItemsOnClick""
               OnChange=""(BitMenuButtonItem item) => eventsChangedItem = item?.Key""
               OnClick=""(BitMenuButtonItem item) => eventsClickedItem = item?.Key"" />


<div>Changed item: @eventsChangedItem</div>
<div>Clicked item: @eventsClickedItem</div>";

    private readonly string example10CsharpCode = @"
private string? eventsClickedItem;
private string? eventsChangedItem;

private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private List<BitMenuButtonItem> basicItemsOnClick =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];

protected override void OnInitialized()
{
    Action<BitMenuButtonItem> onClick = item =>
    {
        eventsClickedItem = $""{item.Text}"";
        StateHasChanged();
    };

    basicItemsOnClick.ForEach(i => i.OnClick = onClick);
}";

    private readonly string example11RazorCode = @"
<BitMenuButton Split Sticky Items=""basicItems"" DefaultSelectedItem=""basicItems[1]"" />

<BitMenuButton Sticky Items=""basicItems"" @bind-SelectedItem=""twoWaySelectedItem"" />
<BitChoiceGroup Horizontal Items=""@choiceGroupItems"" @bind-Value=""@twoWaySelectedItem"" />

<BitMenuButton Sticky Items=""isSelectedItems"" />

<BitMenuButton Sticky Items=""basicItems"" IsOpen=""oneWayIsOpen"" />
<BitCheckbox Label=""One-way IsOpen"" @bind-Value=""oneWayIsOpen"" OnChange=""async _ => { await Task.Delay(2000); oneWayIsOpen = false; }"" />

<BitMenuButton Sticky Items=""basicItems"" @bind-IsOpen=""twoWayIsOpen"" />
<BitCheckbox Label=""Two-way IsOpen"" @bind-Value=""twoWayIsOpen"" />";

    private readonly string example11CsharpCode = @"
private BitMenuButtonItem twoWaySelectedItem = default!;
private bool oneWayIsOpen;
private bool twoWayIsOpen;

private static List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];

private static IEnumerable<BitChoiceGroupItem<BitMenuButtonItem>> choiceGroupItems =
    basicItems.Select(i => new BitChoiceGroupItem<BitMenuButtonItem>() { Id = i.Key, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

private List<BitMenuButtonItem> isSelectedItems =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2, IsSelected = true }
];

protected override void OnInitialized()
{
    twoWaySelectedItem = basicItems[2];
}";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""File"" Items=""groupedItems"" IconName=""@BitIconName.OpenFile"" />
<BitMenuButton Split Sticky Items=""groupedItems"" />";

    private readonly string example12CsharpCode = @"
private List<BitMenuButtonItem> groupedItems =
[
    new() { Text = ""Document"", IsHeader = true },
    new() { Text = ""New"", Key = ""new"", IconName = BitIconName.Add, SecondaryText = ""Ctrl+N"" },
    new() { Text = ""Open"", Key = ""open"", IconName = BitIconName.OpenFile, SecondaryText = ""Ctrl+O"" },
    new() { IsSeparator = true },
    new() { Text = ""Save"", Key = ""save"", IconName = BitIconName.Save, SecondaryText = ""Ctrl+S"" },
    new() { Text = ""Save as"", Key = ""save-as"", IconName = BitIconName.SaveAs },
    new() { IsSeparator = true },
    new() { Text = ""Danger zone"", IsHeader = true },
    new() { Text = ""Delete"", Key = ""delete"", IconName = BitIconName.Delete, SecondaryText = ""Del"" }
];";

    private readonly string example13RazorCode = @"
<BitMenuButton Text=""Links"" Items=""linkItems"" IconName=""@BitIconName.Globe"" />";

    private readonly string example13CsharpCode = @"
private static List<BitMenuButtonItem> linkItems =
[
    new() { Text = ""bit platform"", Key = ""bit"", IconName = BitIconName.Globe, Href = ""https://bitplatform.dev"", Target = ""_blank"", Title = ""The bit platform website"" },
    new() { Text = ""GitHub repo"", Key = ""github"", IconName = BitIconName.Link, Href = ""https://github.com/bitfoundation/bitplatform"", Target = ""_blank"", Title = ""The bit platform GitHub repository"" },
    new() { IsSeparator = true },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2, Title = ""A regular item"" }
];";

    private readonly string example14RazorCode = @"
<BitMenuButton Text=""Full width"" Items=""basicItemsIcon"" FullWidth />
<BitMenuButton Text=""Full width split"" Items=""basicItemsIcon"" FullWidth Split />";

    private readonly string example14CsharpCode = @"
private static List<BitMenuButtonItem> basicItemsIcon =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];";

    private readonly string example15RazorCode = @"
<BitMenuButton Split Text=""Save"" Items=""basicItemsIcon"" IconName=""@BitIconName.Save"" LoadingLabel=""Saving...""
               AutoLoading OnClick=""(BitMenuButtonItem _) => HandleOnSaveClick()"" />

<BitMenuButton Split Text=""Refresh"" Items=""basicItemsIcon"" IconName=""@BitIconName.Refresh""
               AutoLoading LoadingDelay=""500"" OnClick=""(BitMenuButtonItem _) => HandleOnRefreshClick()"" />

<BitMenuButton Text=""Loading"" Items=""basicItemsIcon"" IsLoading=""itemIsLoading"" />
<BitCheckbox Label=""IsLoading"" @bind-Value=""itemIsLoading"" />

<BitMenuButton Split Text=""Upload"" Items=""basicItemsIcon"" IconName=""@BitIconName.Upload""
               AutoLoading OnClick=""(BitMenuButtonItem _) => HandleOnSaveClick()"">
    <LoadingTemplate>
        <BitEllipsisLoading CustomSize=""24"" CustomColor=""currentColor"" />
    </LoadingTemplate>
</BitMenuButton>";

    private readonly string example15CsharpCode = @"
private bool itemIsLoading;

private static List<BitMenuButtonItem> basicItemsIcon =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];

private async Task HandleOnSaveClick() => await Task.Delay(2000);

private async Task HandleOnRefreshClick() => await Task.Delay(300);";

    private readonly string example16RazorCode = @"
<BitMenuButton Text=""Hover over me"" Title=""The menu button tooltip"" Items=""basicItemsIcon""
               AriaDescription=""Opens a menu of three commands."" />

<BitMenuButton Split Text=""Save"" Title=""Save the document"" ChevronDownTitle=""More save options""
               ChevronDownAriaLabel=""More save options"" Items=""basicItemsIcon"" />

<BitMenuButton AriaLabel=""More actions"" IconName=""@BitIconName.More"" Items=""ariaLabelItems"" Variant=""BitVariant.Text"" />

<BitMenuButton Text=""Disabled items"" Items=""basicItemsIcon"" DisabledInteractive Variant=""BitVariant.Outline"" />";

    private readonly string example16CsharpCode = @"
private List<BitMenuButtonItem> basicItemsIcon =
[
    new() { Text = ""Item A"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""Item B"", Key = ""B"", IconName = BitIconName.Emoji, IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"", IconName = BitIconName.Emoji2 }
];

private List<BitMenuButtonItem> ariaLabelItems =
[
    new() { Key = ""share"", IconName = BitIconName.Share, AriaLabel = ""Share this page"" },
    new() { Key = ""print"", IconName = BitIconName.Print, AriaLabel = ""Print this page"" }
];";

    private readonly string example17RazorCode = @"
<BitMenuButton Text=""TopAndBottom"" Items=""dropDirectionItems"" DropDirection=""BitDropDirection.TopAndBottom"" />
<BitMenuButton Text=""All"" Items=""dropDirectionItems"" DropDirection=""BitDropDirection.All"" />";

    private readonly string example17CsharpCode = @"
private static List<BitMenuButtonItem> dropDirectionItems =
    Enumerable.Range(1, 8).Select(i => new BitMenuButtonItem { Text = $""Item {i}"", Key = i.ToString() }).ToList();";

    private readonly string example18RazorCode = @"
<BitMenuButton Text=""Insert"" Items=""submenuItems"" IconName=""@BitIconName.Add""
               OnClick=""(BitMenuButtonItem? item) => submenuClickedItem = item?.Text"" />

<BitMenuButton Split Text=""Share"" Items=""shareItems"" Variant=""BitVariant.Outline""
               SubmenuIconName=""@BitIconName.ChevronRightMed""
               OnClick=""(BitMenuButtonItem? item) => submenuClickedItem = item?.Text"" />

<div>Clicked item: @submenuClickedItem</div>";

    private readonly string example18CsharpCode = @"
private string? submenuClickedItem;

private List<BitMenuButtonItem> submenuItems =
[
    new() { Text = ""Text box"", Key = ""text"", IconName = BitIconName.TextBox },
    new()
    {
        Text = ""Chart"", Key = ""chart"", IconName = BitIconName.BarChart4,
        ChildItems =
        [
            new() { Text = ""Common"", IsHeader = true },
            new() { Text = ""Bar"", Key = ""bar"", IconName = BitIconName.BarChartHorizontal },
            new() { Text = ""Line"", Key = ""line"", IconName = BitIconName.LineChart },
            new() { IsSeparator = true },
            new()
            {
                Text = ""More"", Key = ""more"", IconName = BitIconName.More,
                ChildItems =
                [
                    new() { Text = ""Scatter"", Key = ""scatter"" },
                    new() { Text = ""Bubble"", Key = ""bubble"", IsEnabled = false }
                ]
            }
        ]
    },
    new() { Text = ""Table"", Key = ""table"", IconName = BitIconName.Table }
];

private List<BitMenuButtonItem> shareItems =
[
    new() { Text = ""Copy link"", Key = ""copy"", IconName = BitIconName.Link, SecondaryText = ""Ctrl+C"" },
    new()
    {
        Text = ""Send to"", Key = ""send"", IconName = BitIconName.Send,
        ChildItems =
        [
            new() { Text = ""Email"", Key = ""email"", IconName = BitIconName.Mail, SecondaryText = ""Ctrl+E"" },
            new() { Text = ""Teams"", Key = ""teams"", IconName = BitIconName.TeamsLogo },
            new() { Text = ""Printer"", Key = ""printer"", IconName = BitIconName.Print, IsEnabled = false }
        ]
    }
];";

    private readonly string example19RazorCode = @"
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" />

<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split />


<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" />

<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split />


<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" />

<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split />


<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" />

<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split />


<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" />

<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split />


<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" />

<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split />


<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" />

<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split />


<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" />

<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split />


<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" />

<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" Split />


<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" />

<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" Split />


<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" />

<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" Split />


<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" />

<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" Split />


<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" />

<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" Split />


<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" />

<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" Split />


<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" />

<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" Split />


<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" />

<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" Split />


<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" />

<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" Split />


<div><b>Disabled</b>:</div>

<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" IsEnabled=""false"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" IsEnabled=""false"" />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" IsEnabled=""false"" />

<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Primary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" IsEnabled=""false"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" IsEnabled=""false"" />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" IsEnabled=""false"" />

<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Secondary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" IsEnabled=""false"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" IsEnabled=""false"" />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IsEnabled=""false"" />

<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Tertiary"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" IsEnabled=""false"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" IsEnabled=""false"" />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" IsEnabled=""false"" />

<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Info"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" IsEnabled=""false"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" IsEnabled=""false"" />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" IsEnabled=""false"" />

<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Success"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" IsEnabled=""false"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" IsEnabled=""false"" />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" IsEnabled=""false"" />

<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Warning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" IsEnabled=""false"" Split />


<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" />

<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SevereWarning"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" IsEnabled=""false"" Split />


<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" IsEnabled=""false"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" IsEnabled=""false"" />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" IsEnabled=""false"" />

<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" IsEnabled=""false"" Split />
<BitMenuButton Text=""Error"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" IsEnabled=""false"" Split />


<div style=""background:var(--bit-clr-fg-sec);padding:1rem"">
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" />

<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" IsEnabled=""false"" Split />

<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" />

<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" IsEnabled=""false"" Split />

<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" />

<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryBackground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" IsEnabled=""false"" Split />
</div>


<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" />

<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" IsEnabled=""false"" Split />


<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" />

<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" IsEnabled=""false"" Split />


<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" />

<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryForeground"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" IsEnabled=""false"" Split />


<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" />

<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""PrimaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" IsEnabled=""false"" Split />


<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" />

<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""SecondaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" IsEnabled=""false"" Split />


<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" />

<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" Split />
<BitMenuButton Text=""TertiaryBorder"" Items=""basicItems"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" IsEnabled=""false"" Split />";

    private readonly string example19CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example20RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitMenuButton Text=""Actions"" 
               Items=""externalIconItems"" 
               Icon=""@(""fa-solid fa-house"")"" />

<BitMenuButton Split 
               Text=""Actions"" 
               Items=""externalIconItems"" 
               Icon=""@(""fa-brands fa-github"")"" />



<BitMenuButton Text=""Actions"" 
               Items=""externalIconItems"" 
               Color=""BitColor.Secondary""
               Variant=""BitVariant.Outline"" 
               Icon=""@BitIconInfo.Css(""fa-solid fa-house"")"" />

<BitMenuButton Split 
               Text=""Actions"" 
               Items=""externalIconItems"" 
               Color=""BitColor.Secondary""
               Variant=""BitVariant.Outline"" 
               Icon=""@BitIconInfo.Css(""fa-brands fa-github"")"" />



<BitMenuButton Text=""Actions"" 
               Items=""externalIconItems"" 
               Color=""BitColor.Tertiary""
               Variant=""BitVariant.Text"" 
               Icon=""@BitIconInfo.Fa(""solid house"")"" />

<BitMenuButton Split
               Text=""Actions""
               Items=""externalIconItems""
               Color=""BitColor.Tertiary""
               Variant=""BitVariant.Text""
               Icon=""@BitIconInfo.Fa(""brands github"")""
               ChevronDownIcon=""@BitIconInfo.Fa(""solid angles-down"")"" />";

    private readonly string example20CsharpCode = @"
private static List<BitMenuButtonItem> externalIconItems =
[
    new() { Text = ""Add"", Icon = ""fa-solid fa-plus"" },
    new() { Text = ""Edit"", Icon = BitIconInfo.Css(""fa-solid fa-pen"") },
    new() { Text = ""Delete"", Icon = BitIconInfo.Fa(""solid trash"") }
];";

    private readonly string example21RazorCode = @"
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"" />
<BitMenuButton Text=""Small"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Small"" />

<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Medium"" />
<BitMenuButton Text=""Medium"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Medium"" />

<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"" />
<BitMenuButton Text=""Large"" Items=""basicItems"" Variant=""BitVariant.Text"" Size=""BitSize.Large"" />";

    private readonly string example21CsharpCode = @"
private List<BitMenuButtonItem> basicItems =
[
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
];";

    private readonly string example22RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class > button {
        color: tomato;
        border-color: tomato;
        background: transparent;
    }

    .custom-class > button:hover {
        background-color: #ff63473b;
    }


    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }


    .custom-button {
        color: deepskyblue;
        background: transparent;
    }

    .custom-opened .custom-button {
        color: cornflowerblue;
    }

    .custom-callout {
        border-radius: 1rem;
        border-color: lightgray;
        backdrop-filter: blur(20px);
        background-color: transparent;
        box-shadow: darkgray 0 0 0.5rem;
    }

    .custom-item-button {
        border-bottom: 1px solid gray;
    }

    .custom-item-button:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    .custom-callout li:last-child .custom-item-button {
        border-bottom: none;
    }
</style>


<BitMenuButton Text=""Styled Button"" Items=""basicItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: aqua 0 0 1rem; overflow: hidden;"" />
<BitMenuButton Text=""Classed Button"" Items=""basicItems"" Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitMenuButton Text=""Non-Sticky"" Items=""itemStyleClassItems"" Variant=""BitVariant.Text"" />
<BitMenuButton Text=""Sticky"" Sticky Items=""itemStyleClassItems"" Variant=""BitVariant.Text"" />


<BitMenuButton Text=""Classes"" Items=""basicItems"" IconName=""@BitIconName.FormatPainter"" Variant=""BitVariant.Text""
               Classes=""@(new() { OperatorButton = ""custom-button"",
                                  Opened = ""custom-opened"",
                                  Callout = ""custom-callout"",
                                  ItemButton = ""custom-item-button"" })"" />

<BitMenuButton Text=""Styles"" Items=""basicItems"" IconName=""@BitIconName.Brush""
               Styles=""@(new() { Root = ""--button-background: tomato; background: var(--button-background); border-color: var(--button-background); border-radius: 0.25rem;"",
                                 Opened = ""--button-background: orangered;"",
                                 OperatorButton = ""background: var(--button-background);"",
                                 ItemButton = ""background: lightcoral;"",
                                 Callout = ""border-radius: 0.25rem; box-shadow: lightgray 0 0 0.5rem;"" })"" />

<BitMenuButton Text=""Branded"" IconName=""@BitIconName.Brush"" Items=""basicItemsIcon""
               Style=""--bit-MenuButton-background: #6d28d9;
                      --bit-MenuButton-hover-background: #5b21b6;
                      --bit-MenuButton-active-background: #4c1d95;
                      --bit-MenuButton-radius: 2rem;
                      --bit-MenuButton-padding: 0.5rem 1.25rem;""
               Styles=""@(new() { Callout = ""--bit-MenuButton-callout-radius: 0.75rem;"" +
                                           ""--bit-MenuButton-item-min-height: 2.5rem;"" +
                                           ""--bit-MenuButton-item-hover-background: #ede9fe;"" +
                                           ""--bit-MenuButton-item-focus-color: #6d28d9;"" })"" />";

    private readonly string example22CsharpCode = @"
private List<BitMenuButtonItem> basicItems = new()
{
    new() { Text = ""Item A"", Key = ""A"" },
    new() { Text = ""Item B"", Key = ""B"", IsEnabled = false },
    new() { Text = ""Item C"", Key = ""C"" }
};

private static List<BitMenuButtonItem> itemStyleClassItems =
[
    new() { Text = ""Item A (Default)"", Key = ""A"", IconName = BitIconName.Emoji, Style = ""color: brown"" },
    new() { Text = ""Item C (Styled)"", Key = ""B"", IconName = BitIconName.Emoji, Style = ""color: tomato; border-color: brown; background-color: peachpuff;"" },
    new() { Text = ""Item B (Classed)"", Key = ""C"", IconName = BitIconName.Emoji2, Class = ""custom-item"" }
];";

    private readonly string example23RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlItemsIcon"" IconName=""@BitIconName.Edit"" />
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" Items=""rtlItemsIcon"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"" Split />";

    private readonly string example23CsharpCode = @"
 private static List<BitMenuButtonItem> rtlItemsIcon =
[
    new() { Text = ""گزینه الف"", Key = ""A"", IconName = BitIconName.Emoji },
    new() { Text = ""گزینه ب"", Key = ""B"", IconName = BitIconName.Emoji },
    new() { Text = ""گزینه ج"", Key = ""C"", IconName = BitIconName.Emoji2 }
];";
}
