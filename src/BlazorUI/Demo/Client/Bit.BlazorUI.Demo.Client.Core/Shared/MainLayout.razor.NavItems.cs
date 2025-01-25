namespace Bit.BlazorUI.Demo.Client.Core.Shared;

public partial class MainLayout
{

    private readonly List<BitNavItem> _navItems =
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
                new() { Text = "Dropdown", Url = "/components/dropdown", Description = "Select, MultiSelect, ComboBox", Data = "Chips" },
                new() { Text = "FileUpload", Url = "/components/fileupload", AdditionalUrls = ["/components/file-upload"] },
                new() { Text = "NumberField", Url = "/components/numberfield", AdditionalUrls = ["/components/numerictextfield", "/components/numeric-text-field"], Description = "NumberInput" },
                new() { Text = "OtpInput", Url = "/components/otpinput", AdditionalUrls = ["/components/otp-input"] },
                new() { Text = "Rating", Url = "/components/rating" },
                new() { Text = "SearchBox", Url = "/components/searchbox", AdditionalUrls = ["/components/search-box"], Data = "AutoComplete" },
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
                new() { Text = "Stack", Url = "/components/stack", Description = "WrapPanel" },
            ],
        },
        new()
        {
            Text = "Lists",
            ChildItems =
            [
                new() { Text = "BasicList", Url = "/components/basiclist", AdditionalUrls = ["/components/basic-list"] },
                new() { Text = "Carousel", Url = "/components/carousel", Description = "Slideshow" },
                new() { Text = "Swiper", Url = "/components/swiper", Description = "Touch slider" },
                new() { Text = "Timeline", Url = "/components/timeline" },
            ]
        },
        new()
        {
            Text = "Navs",
            ChildItems =
            [
                new() { Text = "Breadcrumb", Url = "/components/breadcrumb" },
                new() { Text = "DropMenu", Url = "/components/dropmenu" },
                new() { Text = "Nav", Url = "/components/nav", Description = "Tree" },
                new() { Text = "NavBar", Url = "/components/navbar", Description = "NavMenu, TabPanel" },
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
                new() { Text = "Card", Url = "/components/card" },
                new() { Text = "Collapse", Url = "/components/collapse" },
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
                new() { Text = "PullToRefresh", Url = "/components/pulltorefresh" },
                new() { Text = "Separator", Url = "/components/separator" },
                new() { Text = "Sticky", Url = "/components/sticky" },
                new() { Text = "SwipeTrap", Url = "/components/swipetrap" },
                new() { Text = "Text", Url = "/components/text" },
            ],
        },
        new()
        {
            Text = "Extras",
            ChildItems =
            [
                new() { Text = "AppShell", Url = "/components/appshell" },
                new() { Text = "Chart", Url = "/components/chart" },
                new() { Text = "DataGrid", Url = "/components/datagrid", AdditionalUrls = ["/components/data-grid"] },
                new() { Text = "ErrorBoundary", Url = "/components/errorboundary" },
                new() { Text = "InfiniteScrolling", Url = "/components/infinitescrolling" },
                new() { Text = "MessageBox", Url = "/components/messagebox" },
                new() { Text = "NavPanel", Url = "/components/navpanel" },
                new() { Text = "PdfReader", Url = "/components/pdfreader" },
                new() { Text = "ProPanel", Url = "/components/propanel" },
                new()
                {
                    Text = "Services",
                    ChildItems =
                    [
                        new() { Text = "ModalService", Url = "/components/modalservice" },
                    ]
                },
            ]
        },
        new() { Text = "Iconography", Url = "/iconography" },
        new() { Text = "Theming", Url = "/theming" },
    ];
}
