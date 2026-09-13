namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class PopularComponents
{
    private DateTimeOffset? SelectedDate;

    private readonly List<PopularComponent> _components =
    [
        new()
        {
            Name = "ColorPicker",
            Description = "Browse and pick a color, with hue, saturation and alpha.",
            Url = "/components/colorpicker",
            Code = """
                   <BitColorPicker ShowPreview ShowAlphaSlider
                                   @bind-Alpha="Alpha"
                                   @bind-Color="Color" />

                   @code {
                       private double Alpha = 1;
                       private string Color = "rgb(0,101,239)";
                   }
                   """
        },
        new()
        {
            Name = "DatePicker",
            Description = "A drop-down calendar for picking a single date.",
            Url = "/components/datepicker",
            Code = """
                   <BitDatePicker @bind-Value="SelectedDate"
                                  Placeholder="Select a date"
                                  ShowWeekNumbers />

                   @code {
                       private DateTimeOffset? SelectedDate;
                   }
                   """
        },
        new()
        {
            Name = "FileUpload",
            Description = "Chunked, resumable uploads with per-file progress.",
            Url = "/components/fileupload",
            Code = """
                   <BitFileUpload AutoUpload
                                  Multiple
                                  ChunkedUpload
                                  Label="Select files"
                                  UploadUrl="@UploadUrl"
                                  RemoveUrl="@RemoveUrl"
                                  MaxSize="1024 * 1024 * 500" />

                   @code {
                       private string UploadUrl = "/UploadFile";
                       private string RemoveUrl = "/RemoveFile";
                   }
                   """
        },
        new()
        {
            Name = "Dropdown",
            Description = "Single or multi select, with grouping and search.",
            Url = "/components/dropdown",
            Code = """
                   <BitDropdown Label="Multi-select Dropdown"
                                MultiSelect
                                Items="DropdownItems"
                                @bind-Values="SelectedValues"
                                Placeholder="Select options" />

                   @code {
                       private IEnumerable<string?> SelectedValues = ["f-app", "f-ban"];

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
                   }
                   """
        },
        new()
        {
            Name = "Nav (TreeList)",
            Description = "Links to the main areas of an app, or a tree view.",
            Url = "/components/nav",
            Code = """
                   <BitNav Items="NavItems"
                           Mode="BitNavMode.Manual"
                           @bind-SelectedItem="SelectedNavItem" />

                   @code {
                       private BitNavItem SelectedNavItem = NavItems[2];

                       private static List<BitNavItem> NavItems =
                       [
                           new()
                           {
                               Text = "Home",
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
                   """
        }
    ];

    private PopularComponent? SelectedComponent;

    protected override async Task OnInitAsync()
    {
        SelectedComponent = _components[0];
        await base.OnInitAsync();
    }

    /// <summary>
    /// The id of this section's code box, so the highlight pass below can be aimed at it.
    /// </summary>
    private const string CodeBoxId = "home-showcase-code";

    /// <summary>
    /// Prism has to be re-run after every selection, because the code box holds a different sample
    /// each time - but only over this one block. Highlighting the whole document (which is what an
    /// id-less call does) would re-tokenize every other code box on the home page on every click.
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JSRuntime.InvokeVoid("highlightSnippet", CodeBoxId);

        await base.OnAfterRenderAsync(firstRender);
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
