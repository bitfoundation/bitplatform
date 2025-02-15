namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class PopularComponents
{
    private bool showCode;
    private DateTimeOffset? SelectedDate;

    private List<PopularComponent> _components =
    [
        new()
        {
            Name = "ColorPicker",
            Description = "The ColorPicker component is used to browse through and select colors.",
            Url = "/components/colorpicker"
        },
        new()
        {
            Name = "DatePicker",
            Description = "The DatePicker component offers a drop-down control that’s optimized for picking a single date from a calendar view.",
            Url = "/components/datepicker"
        },
        new()
        {
            Name = "FileUpload",
            Description = "The FileUpload component wraps the HTML file input element(s) and uploads them to a given URL.",
            Url = "/components/fileupload"
        },
        new()
        {
            Name = "Dropdown",
            Description = "The Dropdown component is a list in which the selected item is always visible while other items are visible on demand by clicking a dropdown button.",
            Url = "/components/dropdown"
        },
        new()
        {
            Name = "Nav (TreeList)",
            Description = "The Nav (TreeList) component provides links to the main areas of an app or site.",
            Url = "/components/nav"
        }
    ];

    private PopularComponent? SelectedComponent;

    protected override async Task OnInitAsync()
    {
        SelectedComponent = _components[0];
        await base.OnInitAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoid("highlightSnippet");
    }

    private void SelectComponent(PopularComponent com)
    {
        SelectedComponent = com;
        StateHasChanged();
    }


    private string Color = "rgb(0,101,239)";
    private double Alpha = 1;


    private string UploadUrl => $"{Configuration.GetApiServerAddress()}FileUpload/UploadChunkedFile";
    private string RemoveUrl => $"{Configuration.GetApiServerAddress()}FileUpload/RemoveFile";



    private IEnumerable<string?> SelectedDropdownValues = ["f-app", "f-ban"];
    private static List<BitDropdownItem<string>> DropdownItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Banana", Value = "f-ban" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" }
    ];



    private BitNavItem SelectedNavItem = NavItems[2];
    private static List<BitNavItem> NavItems =
    [
        new()
        {
            Text = "Home",
            ExpandAriaLabel = "Expand Home section",
            CollapseAriaLabel = "Collapse Home section",
            IsExpanded = true,
            ChildItems =
            [
                new() { Text = "Activity" },
                new() { Text = "MSN", IsEnabled = false }
            ]
        },
        new() { Text = "Documents", IsExpanded = true },
        new() { Text = "Pages" },
        new() { Text = "Notebook", IsEnabled = false },
        new() { Text = "Communication and Media" },
        new() { Text = "News", Title = "News", IconName = BitIconName.News },
    ];
}
