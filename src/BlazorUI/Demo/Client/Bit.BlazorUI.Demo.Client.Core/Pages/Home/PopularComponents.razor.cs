namespace Bit.BlazorUI.Demo.Client.Core.Pages;

public partial class PopularComponents
{
    private DateTimeOffset? SelectedDate;

    private readonly List<PopularComponent> _components =
    [
        new()
        {
            Name = "DataGrid",
            Description = "Sort, filter, page and virtualize large data sets.",
            Url = "/components/datagrid",
            Code = """
                   <BitDataGrid Items="Products" Pageable PageSize="5" AriaLabel="Products">
                       <BitDataGridColumn Property="p => p.Name" Width="180px" />
                       <BitDataGridColumn Property="p => p.Category" />
                       <BitDataGridColumn Property="p => p.Price" Format="C2" Align="BitTextAlign.End" />
                       <BitDataGridColumn Property="p => p.Stock" Align="BitTextAlign.End" />
                   </BitDataGrid>

                   @code {
                       private record Product(string Name, string Category, decimal Price, int Stock);

                       private static List<Product> Products =
                       [
                           new("Smart Speaker", "Electronics", 89.99m, 120),
                           new("Classic Notebook", "Books", 12.50m, 340),
                           new("Eco Backpack", "Sports", 64.00m, 75),
                           new("Pro Camera", "Electronics", 899.00m, 12),
                           new("Vintage Lamp", "Home", 45.90m, 0),
                           new("Modern Jacket", "Clothing", 129.00m, 48),
                           new("Mini Drone", "Toys", 249.99m, 30),
                           new("Deluxe Blender", "Home", 159.00m, 22),
                           new("Compact Mug", "Home", 9.99m, 510),
                           new("Ultra Sneaker", "Sports", 119.00m, 64),
                       ];
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
            Name = "FullCalendar",
            Description = "Day, week, month and agenda views, with drag-and-drop events.",
            Url = "/components/fullcalendar",
            Code = """
                   <BitFullCalendar Events="Events" />

                   @code {
                       private List<BitFullCalendarEvent> Events = CreateEvents();

                       private static List<BitFullCalendarEvent> CreateEvents()
                       {
                           var today = DateTime.Today;
                           return
                           [
                               new() { Id = "1", Title = "Team Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(45), Color = "blue" },
                               new() { Id = "2", Title = "Design Review", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Color = "purple" },
                               new() { Id = "3", Title = "Sprint Planning", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = "orange" },
                               new() { Id = "4", Title = "Tech Conference", StartDate = today.AddDays(1).AddHours(9), EndDate = today.AddDays(3).AddHours(17), Color = "green" },
                               new() { Id = "5", Title = "Product Demo", StartDate = today.AddDays(-2).AddHours(14), EndDate = today.AddDays(-2).AddHours(15), Color = "red" },
                           ];
                       }
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
                           new() { Text = "Orange", Value = "f-ora", IsDisabled = true },
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
                                   new() { Text = "MSN", IsDisabled = true }
                               ]
                           },
                           new() { Text = "Documents", IsExpanded = true },
                           new() { Text = "Pages" },
                           new() { Text = "Notebook", IsDisabled = true },
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


    private record GridProduct(string Name, string Category, decimal Price, int Stock);
    private static List<GridProduct> GridProducts =
    [
        new("Smart Speaker", "Electronics", 89.99m, 120),
        new("Classic Notebook", "Books", 12.50m, 340),
        new("Eco Backpack", "Sports", 64.00m, 75),
        new("Pro Camera", "Electronics", 899.00m, 12),
        new("Vintage Lamp", "Home", 45.90m, 0),
        new("Modern Jacket", "Clothing", 129.00m, 48),
        new("Mini Drone", "Toys", 249.99m, 30),
        new("Deluxe Blender", "Home", 159.00m, 22),
        new("Compact Mug", "Home", 9.99m, 510),
        new("Ultra Sneaker", "Sports", 119.00m, 64),
    ];



    private List<BitFullCalendarEvent> CalendarEvents = CreateCalendarEvents();
    private static List<BitFullCalendarEvent> CreateCalendarEvents()
    {
        var today = DateTime.Today;
        return
        [
            new() { Id = "1", Title = "Team Standup", StartDate = today.AddHours(9), EndDate = today.AddHours(9).AddMinutes(45), Color = "blue" },
            new() { Id = "2", Title = "Design Review", StartDate = today.AddHours(10), EndDate = today.AddHours(11), Color = "purple" },
            new() { Id = "3", Title = "Sprint Planning", StartDate = today.AddHours(14), EndDate = today.AddHours(15).AddMinutes(30), Color = "orange" },
            new() { Id = "4", Title = "Tech Conference", StartDate = today.AddDays(1).AddHours(9), EndDate = today.AddDays(3).AddHours(17), Color = "green" },
            new() { Id = "5", Title = "Product Demo", StartDate = today.AddDays(-2).AddHours(14), EndDate = today.AddDays(-2).AddHours(15), Color = "red" },
        ];
    }



    private IEnumerable<string?> SelectedDropdownValues = ["f-app", "f-ban"];
    private static List<BitDropdownItem<string>> DropdownItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Orange", Value = "f-ora", IsDisabled = true },
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
                new() { Text = "MSN", IsDisabled = true }
            ]
        },
        new() { Text = "Documents", IsExpanded = true },
        new() { Text = "Pages" },
        new() { Text = "Notebook", IsDisabled = true },
        new() { Text = "Communication and Media" },
        new() { Text = "News", Title = "News", IconName = BitIconName.News },
    ];
}
