namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class NavMenu : IDisposable
{
    private bool _disposed;
    private bool _isNavOpen = false;
    private string _searchText = string.Empty;
    private List<BitNavItem> _flatNavItemList = default!;
    private List<BitNavItem> _filteredNavItems = default!;
    private readonly List<BitNavItem> _allNavItems = new()
    {
        new() { Text = "Overview", Url = "/overview" },
        new() { Text = "Getting started", Url = "/getting-started" },
        new()
        {
            Text = "Buttons",
            ChildItems = new()
            {
                new() { Text = "Button", Url = "/components/button" },
                new() { Text = "ActionButton", Url = "/components/actionbutton", AdditionalUrls = new string[] { "/components/action-button" } },
                new() { Text = "ButtonGroup", Url = "/components/button-group" },
                new() { Text = "CompoundButton", Url = "/components/compoundbutton", AdditionalUrls = new string[] { "/components/compound-button" } },
                new() { Text = "IconButton", Url = "/components/iconbutton", AdditionalUrls = new string[] { "/components/icon-button" } },
                new() { Text = "MenuButton", Url = "/components/menubutton", AdditionalUrls = new string[] { "/components/menu-button" } },
                new() { Text = "ToggleButton", Url = "/components/togglebutton", AdditionalUrls = new string[] { "/components/toggle-button" } }
            }
        },
        new()
        {
            Text = "Inputs",
            ChildItems = new()
            {
                new() { Text = "Checkbox", Url = "/components/checkbox", AdditionalUrls = new string[] { "/components/check-box" } },
                new() { Text = "ChoiceGroup", Url = "/components/choicegroup", AdditionalUrls = new string[] { "/components/choice-group" }, Description = "Radio, RadioButton" },
                new() { Text = "Dropdown", Url = "/components/dropdown", Description = "Select, MultiSelect, ComboBox" },
                new() { Text = "Rating", Url = "/components/rating" },
                new() { Text = "SearchBox", Url = "/components/searchbox", AdditionalUrls = new string[] { "/components/search-box" } },
                new() { Text = "Slider", Url = "/components/slider", Description = "Range" },
                new() { Text = "SpinButton", Url = "/components/spinbutton", AdditionalUrls = new string[] { "/components/spin-button" } },
                new() { Text = "TextField", Url = "/components/textfield", AdditionalUrls = new string[] { "/components/text-field" }, Description = "TextInput" },
                new() { Text = "NumberField", Url = "/components/numberfield", AdditionalUrls = new string[] { "/components/numerictextfield", "/components/numeric-text-field" }, Description = "NumberInput" },
                new() { Text = "OtpInput", Url = "/components/otpinput", AdditionalUrls = new string[] { "/components/otp-input" } },
                new() { Text = "Toggle", Url = "/components/toggle", Description = "Switch" }
            }
        },
        new()
        {
            Text = "Galleries & Pickers",
            ChildItems = new()
            {
                new() { Text = "Calendar", Url = "/components/calendar" },
                new() { Text = "ColorPicker", Url = "/components/colorpicker", AdditionalUrls = new string[] { "/components/color-picker" } },
                new() { Text = "DatePicker", Url = "/components/datepicker", AdditionalUrls = new string[] { "/components/date-picker" } },
                new() { Text = "DateRangePicker", Url = "/components/daterangepicker", AdditionalUrls = new string[] { "/components/date-range-picker" } },
                new() { Text = "FileUpload", Url = "/components/fileupload", AdditionalUrls = new string[] { "/components/file-upload" } },
                new() { Text = "TimePicker", Url = "/components/timepicker", AdditionalUrls = new string[] { "/components/time-picker" } },
                new() { Text = "CircularTimePicker", Url = "/components/circulartimepicker", AdditionalUrls = new string[] { "/components/circular-time-picker" } },
            }
        },
        new()
        {
            Text = "Items & Lists",
            ChildItems = new()
            {
                new() { Text = "BasicList", Url = "/components/basiclist", AdditionalUrls = new string[] { "/components/basic-list" } },
                new() { Text = "Carousel", Url = "/components/carousel" },
                new() { Text = "Swiper", Url = "/components/swiper" },
                new() { Text = "Timeline", Url = "/components/timeline" },
            }
        },
        new()
        {
            Text = "Commands, Menus & Navs",
            ChildItems = new()
            {
                new() { Text = "Breadcrumb", Url = "/components/breadcrumb" },
                new() { Text = "Nav", Url = "/components/nav", Description = "Tree" },
                new() { Text = "Pagination", Url = "/components/pagination" },
                new() { Text = "Pivot", Url = "/components/pivot", Description = "Tab" },
            }
        },
        new()
        {
            Text = "Notification & Engagement",
            ChildItems = new()
            {
                new() { Text = "Badge", Url = "/components/badge" },
                new() { Text = "MessageBar", Url = "/components/messagebar", AdditionalUrls = new string[] { "/components/message-bar" } },
                new() { Text = "Persona", Url = "/components/persona", Description = "Avatar" },
                new() { Text = "SnackBar", Url = "/components/snackbar", Description = "Toast" },
            }
        },
        new()
        {
            Text = "Progress",
            ChildItems = new()
            {
                new() { Text = "ProgressIndicator", Url = "/components/progressindicator", AdditionalUrls = new string[] { "/components/progress-indicator" } },
                new() { Text = "Shimmer", Url = "/components/shimmer", Description = "Skeleton" },
                new() { Text = "Spinner", Url = "/components/spinner", Description = "Busy, Waiting, Loading" },
                new() { Text = "Loading", Url = "/components/loading" }
            },
        },
        new()
        {
            Text = "Surfaces",
            ChildItems = new()
            {
                new() { Text = "Accordion", Url = "/components/accordion", Description = "Expander" },
                new() { Text = "Dialog", Url = "/components/dialog" },
                new() { Text = "Modal", Url = "/components/modal" },
                new() { Text = "Panel", Url = "/components/panel" },
                new() { Text = "ScrollablePane", Url = "/components/scrollablepane", Description = "ScrollView" },
                new() { Text = "Tooltip", Url = "/components/tooltip" },
            },
        },
        new()
        {
            Text = "Utilities",
            ChildItems = new()
            {
                new() { Text = "Icon", Url = "/components/icon" },
                new() { Text = "Image", Url = "/components/image" },
                new() { Text = "Label",  Url = "/components/label" },
                new() { Text = "Link", Url = "/components/link", Description = "Anchor" },
                new() { Text = "Overlay", Url = "/components/overlay" },
                new() { Text = "Separator", Url = "/components/separator" },
                new() { Text = "Stack", Url = "/components/stack" },
                new() { Text = "Sticky", Url = "/components/sticky" },
                new() { Text = "Typography", Url = "/components/typography" },
            },
        },
        new()
        {
            Text = "Extras",
            ChildItems = new()
            {
                new() { Text = "DataGrid", Url = "/components/datagrid", AdditionalUrls = new string[] { "/components/data-grid" } },
                new() { Text = "Chart", Url = "/components/chart" }
            }
        },
        new() { Text = "Iconography", Url = "/iconography" },
        new() { Text = "Theming", Url = "/theming" },
    };


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
