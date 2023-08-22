using Bit.BlazorUI.Demo.Client.Core.Services;
using Microsoft.AspNetCore.Components.Routing;

namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class NavMenu
{
    private bool isNavOpen = false;
    private readonly List<BitNavItem> allNavLinks = new()
    {
        new() { Text = "Overview", Url = "/overview" },
        new() { Text = "Getting started", Url = "/getting-started" },
        new()
        {
            Text = "Buttons",
            ChildItems = new()
            {
                new() { Text = "Button", Url = "/components/button" },
                new() { Text = "ActionButton", Url = "/components/actionbutton" },
                new() { Text = "CompoundButton", Url = "/components/compoundbutton" },
                new() { Text = "IconButton", Url = "/components/iconbutton" },
                new() { Text = "LoadingButton", Url = "/components/loadingbutton" },
                new() { Text = "MenuButton", Url = "/components/menubutton" },
                new() { Text = "SplitButton", Url = "/components/splitbutton" },
                new() { Text = "ToggleButton", Url = "/components/togglebutton" }
            }
        },
        new()
        {
            Text = "Basic Inputs",
            ChildItems = new()
            {
                new() { Text = "CheckBox", Url = "/components/checkbox" },
                new() { Text = "ChoiceGroup", Url = "/components/choicegroup" },
                new() { Text = "Dropdown", Url = "/components/dropdown" },
                new() { Text = "Label",  Url = "/components/label" },
                new() { Text = "Link", Url = "/components/link" },
                new() { Text = "Rating", Url = "/components/rating" },
                new() { Text = "SearchBox", Url = "/components/searchbox" },
                new() { Text = "Slider", Url = "/components/slider" },
                new() { Text = "SpinButton", Url = "/components/spinbutton" },
                new() { Text = "TextField", Url = "/components/textfield" },
                new() { Text = "NumericTextField", Url = "/components/numerictextfield" },
                new() { Text = "OtpInput", Url = "/components/otpinput" },
                new() { Text = "Toggle (Switch)", Url = "/components/toggle" }
            }
        },
        new()
        {
            Text = "Galleries & Pickers",
            ChildItems = new()
            {
                new() { Text = "Calendar", Url = "/components/calendar" },
                new() { Text = "ColorPicker", Url = "/components/colorpicker" },
                new() { Text = "DatePicker", Url = "/components/datepicker" },
                new() { Text = "DateRangePicker", Url = "/components/daterangepicker" },
                new() { Text = "FileUpload", Url = "/components/fileupload" },
                new() { Text = "TimePicker", Url = "/components/timepicker" },
            }
        },
        new()
        {
            Text = "Items & Lists",
            ChildItems = new()
            {
                new() { Text = "BasicList", Url = "/components/basiclist" },
                new() { Text = "Carousel", Url = "/components/carousel" },
                new() { Text = "Swiper", Url = "/components/swiper" },
                new() { Text = "Persona (AvatarView)", Url = "/components/persona" }
            }
        },
        new()
        {
            Text = "Commands, Menus & Navs",
            ChildItems = new()
            {
                new() { Text = "Breadcrumb", Url = "/components/breadcrumb" },
                new() { Text = "Nav (TreeList)", Url = "/components/nav" },
                new() { Text = "Pivot (Tab)", Url = "/components/pivot" },
            }
        },
        new()
        {
            Text = "Notification & Engagement",
            ChildItems = new()
            {
                new() { Text = "MessageBar", Url = "/components/messagebar" },
                new() { Text = "SnackBar", Url = "/components/snackbar" },
            }
        },
        new()
        {
            Text = "Progress",
            ChildItems = new()
            {
                new() { Text = "ProgressIndicator", Url = "/components/progressindicator" },
                new() { Text = "Spinner (BusyIndicator)", Url = "/components/spinner" },
                new() { Text = "Loading", Url = "/components/loading" }
            },
        },
        new()
        {
            Text = "Surfaces",
            ChildItems = new()
            {
                new() { Text = "Accordion (Expander)", Url = "/components/accordion" },
                new() { Text = "Modal", Url = "/components/modal" },
                new() { Text = "Panel", Url = "/components/panel" },
                new() { Text = "Typography", Url = "/components/typography" },
            },
        },
        new()
        {
            Text = "Utilities",
            ChildItems = new()
            {
                new() { Text = "Icon", Url = "/components/icon" },
                new() { Text = "Overlay", Url = "/components/overlay" },
            },
        },
        new()
        {
            Text = "Extras",
            ChildItems = new()
            {
                new() { Text = "DataGrid", Url = "/components/datagrid" },
                new() { Text = "Chart", Url = "/components/chart" }
            }
        },
        new() { Text = "Iconography", Url = "/iconography" },
        new() { Text = "Theming", Url = "/theming" },
    };

    private List<BitNavItem> filteredNavLinks = default!;
    private string searchText = string.Empty;

    [Inject] public NavManuService _menuService { get; set; } = default!;

    public string? CurrentUrl { get; set; }

    protected override async Task OnInitAsync()
    {
        HandleClear();
        _menuService.OnToggleMenu += ToggleMenu;
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        NavigationManager.LocationChanged += OnLocationChanged;

        await base.OnInitAsync();
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        CurrentUrl = NavigationManager.Uri.Replace(NavigationManager.BaseUri, "/", StringComparison.Ordinal);
        StateHasChanged();
    }

    async Task ToggleMenu()
    {
        try
        {
            isNavOpen = !isNavOpen;

            await JSRuntime.ToggleBodyOverflow(isNavOpen);
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
        filteredNavLinks = allNavLinks;
    }

    private void HandleChange(string text)
    {
        HandleClear();
        searchText = text;
        if (string.IsNullOrEmpty(text)) return;

        var flatNavLinkList = Flatten(allNavLinks).ToList().FindAll(link => !string.IsNullOrEmpty(link.Url));
        filteredNavLinks = flatNavLinkList.FindAll(link => link.Text.Contains(text, StringComparison.InvariantCultureIgnoreCase));
    }

    private async Task HandleOnItemClick(BitNavItem item)
    {
        if (item.Url.HasNoValue()) return;

        searchText = string.Empty;

        HandleClear();
        await ToggleMenu();
    }

    private static IEnumerable<BitNavItem> Flatten(IEnumerable<BitNavItem> e) => e.SelectMany(c => Flatten(c.ChildItems)).Concat(e);
}
