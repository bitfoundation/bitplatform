namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class NavMenu : IDisposable
{
    private bool _disposed;
    private bool _isNavOpen = false;
    private string _searchText = string.Empty;
    private List<BitNavItem> _flatNavItemList = default!;
    private List<BitNavItem> _filteredNavItems = default!;
    private readonly List<BitNavItem> _allNavItems =
    [
        new() { Text = "Overview", Url = "/overview" },
        new() { Text = "Getting started", Url = "/getting-started" },
        new()
        {
            Text = "Buttons",
            ChildItems =
            [
                new() { Text = "ActionButton", Url = "/components/actionbutton", AdditionalUrls = ["/components/action-button"] },
                new() { Text = "Button", Url = "/components/button" },
                new() { Text = "ButtonGroup", Url = "/components/button-group" },
                new() { Text = "MenuButton", Url = "/components/menubutton", AdditionalUrls = ["/components/menu-button"] },
                new() { Text = "ToggleButton", Url = "/components/togglebutton", AdditionalUrls = ["/components/toggle-button"] }
            ]
        },
        new()
        {
            Text = "Inputs",
            ChildItems =
            [
                new() { Text = "Calendar", Url = "/components/calendar" },
                new() { Text = "Checkbox", Url = "/components/checkbox", AdditionalUrls = ["/components/check-box"] },
                new() { Text = "ChoiceGroup", Url = "/components/choicegroup", AdditionalUrls = ["/components/choice-group"], Description = "Radio, RadioButton" },
                new() { Text = "Dropdown", Url = "/components/dropdown", Description = "Select, MultiSelect, ComboBox" },
                new() { Text = "FileUpload", Url = "/components/fileupload", AdditionalUrls = ["/components/file-upload"] },
                new() { Text = "NumberField", Url = "/components/numberfield", AdditionalUrls = ["/components/numerictextfield", "/components/numeric-text-field"], Description = "NumberInput" },
                new() { Text = "OtpInput", Url = "/components/otpinput", AdditionalUrls = ["/components/otp-input"] },
                new() { Text = "Rating", Url = "/components/rating" },
                new() { Text = "SearchBox", Url = "/components/searchbox", AdditionalUrls = ["/components/search-box"] },
                new() { Text = "Slider", Url = "/components/slider", Description = "Range" },
                new() { Text = "SpinButton", Url = "/components/spinbutton", AdditionalUrls = ["/components/spin-button"] },
                new() { Text = "TextField", Url = "/components/textfield", AdditionalUrls = ["/components/text-field"], Description = "TextInput" },
                new() { Text = "Toggle", Url = "/components/toggle", Description = "Switch" },
                new()
                {
                    Text = "Pickers",
                    ChildItems =
                    [
                        new() { Text = "CircularTimePicker", Url = "/components/circulartimepicker", AdditionalUrls = ["/components/circular-time-picker"] },
                        new() { Text = "ColorPicker", Url = "/components/colorpicker", AdditionalUrls = ["/components/color-picker"] },
                        new() { Text = "DatePicker", Url = "/components/datepicker", AdditionalUrls = ["/components/date-picker"] },
                        new() { Text = "DateRangePicker", Url = "/components/daterangepicker", AdditionalUrls = ["/components/date-range-picker"] },
                        new() { Text = "TimePicker", Url = "/components/timepicker", AdditionalUrls = ["/components/time-picker"] },
                    ]
                },
            ]
        },
        new()
        {
            Text = "Layouts",
            ChildItems =
            [
                new() { Text = "Footer", Url = "/components/footer" },
                new() { Text = "Grid", Url = "/components/grid" },
                new() { Text = "Header", Url = "/components/header" },
                new() { Text = "Layout", Url = "/components/layout" },
                new() { Text = "Spacer", Url = "/components/spacer" },
                new() { Text = "Stack", Url = "/components/stack" },
            ],
        },
        new()
        {
            Text = "Lists",
            ChildItems =
            [
                new() { Text = "BasicList", Url = "/components/basiclist", AdditionalUrls = ["/components/basic-list"] },
                new() { Text = "Carousel", Url = "/components/carousel" },
                new() { Text = "Swiper", Url = "/components/swiper" },
                new() { Text = "Timeline", Url = "/components/timeline" },
            ]
        },
        new()
        {
            Text = "Navs",
            ChildItems =
            [
                new() { Text = "Breadcrumb", Url = "/components/breadcrumb" },
                new() { Text = "Nav", Url = "/components/nav", Description = "Tree" },
                new() { Text = "Pagination", Url = "/components/pagination" },
                new() { Text = "Pivot", Url = "/components/pivot", Description = "Tab" },
            ]
        },
        new()
        {
            Text = "Notifications",
            ChildItems =
            [
                new() { Text = "Badge", Url = "/components/badge" },
                new() { Text = "Message", Url = "/components/message", AdditionalUrls = ["/components/messagebar", "/components/message-bar"], Description = "Alert, MessageBar" },
                new() { Text = "Persona", Url = "/components/persona", Description = "Avatar" },
                new() { Text = "SnackBar", Url = "/components/snackbar", Description = "Toast" },
                new() { Text = "Tag", Url = "/components/tag", Description = "Chip" },
            ]
        },
        new()
        {
            Text = "Progress",
            ChildItems =
            [
                new() { Text = "Loading", Url = "/components/loading" },
                new() { Text = "Progress", Url = "/components/progress", AdditionalUrls = ["/components/progressindicator", "/components/progress-indicator", "/components/progressbar"], Description = "ProgressIndicator, ProgressBar" },
                new() { Text = "Shimmer", Url = "/components/shimmer", Description = "Skeleton" },
            ],
        },
        new()
        {
            Text = "Surfaces",
            ChildItems =
            [
                new() { Text = "Accordion", Url = "/components/accordion", Description = "Expander" },
                new() { Text = "Callout", Url = "/components/callout", Description = "Popover, Popup" },
                new() { Text = "Dialog", Url = "/components/dialog" },
                new() { Text = "Modal", Url = "/components/modal" },
                new() { Text = "Panel", Url = "/components/panel" },
                new() { Text = "ScrollablePane", Url = "/components/scrollablepane", Description = "ScrollView" },
                new() { Text = "Splitter", Url = "/components/splitter" },
                new() { Text = "Tooltip", Url = "/components/tooltip" },
            ],
        },
        new()
        {
            Text = "Utilities",
            ChildItems =
            [
                new() { Text = "Element", Url = "/components/element" },
                new() { Text = "Icon", Url = "/components/icon" },
                new() { Text = "Image", Url = "/components/image" },
                new() { Text = "Label",  Url = "/components/label" },
                new() { Text = "Link", Url = "/components/link", Description = "Anchor" },
                new() { Text = "Overlay", Url = "/components/overlay" },
                new() { Text = "Separator", Url = "/components/separator" },
                new() { Text = "Sticky", Url = "/components/sticky" },
                new() { Text = "Typography", Url = "/components/typography" },
            ],
        },
        new()
        {
            Text = "Extras",
            ChildItems =
            [
                new() { Text = "DataGrid", Url = "/components/datagrid", AdditionalUrls = ["/components/data-grid"] },
                new() { Text = "Chart", Url = "/components/chart" }
            ]
        },
        new() { Text = "Iconography", Url = "/iconography" },
        new() { Text = "Theming", Url = "/theming" },
    ];


    [Inject] public NavManuService _navMenuService { get; set; } = default!;


    protected override async Task OnInitAsync()
    {
        _flatNavItemList = Flatten(_allNavItems).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));

        HandleClear();
        _navMenuService.OnToggleMenu += ToggleMenu;

        await base.OnInitAsync();
    }


    private async Task ToggleMenu()
    {
        try
        {
            _isNavOpen = !_isNavOpen;

            await JSRuntime.ToggleBodyOverflow(_isNavOpen);
        }
        catch (Exception ex)
        {
            ExceptionHandler.Handle(ex);
        }
        finally
        {
            StateHasChanged();
        }
    }

    private void HandleClear()
    {
        _filteredNavItems = _allNavItems;
    }

    private void HandleChange(string text)
    {
        _searchText = text;
        _filteredNavItems = _allNavItems;
        if (string.IsNullOrEmpty(text)) return;

        _filteredNavItems = _flatNavItemList.FindAll(item => text.Split(' ').Where(t => t.HasValue()).Any(t => $"{item.Text} {item.Description}".Contains(t, StringComparison.InvariantCultureIgnoreCase)));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (item.Url.HasNoValue()) return;

        _searchText = string.Empty;
        _filteredNavItems = _allNavItems;

        await ToggleMenu();
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);

    public override void Dispose()
    {
        if (_disposed) return;

        _navMenuService.OnToggleMenu -= ToggleMenu;

        _disposed = true;

        base.Dispose();
    }
}
