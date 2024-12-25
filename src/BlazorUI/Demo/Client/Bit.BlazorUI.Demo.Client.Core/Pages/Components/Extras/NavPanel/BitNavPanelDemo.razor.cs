
namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.NavPanel;

public partial class BitNavPanelDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
         new()
         {
            Name = "CanvasClass",
            Type = "string?",
            DefaultValue = "null",
            Description = "The CSS class of the canvas element(s).",
         },
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
         {
            Id = "pdf-reader-config",
            Title = "BitPdfReaderConfig",
            Parameters=
            [
                new()
                {
                    Name = "Id",
                    Type = "string",
                    DefaultValue = "Guid.NewGuid().ToString()",
                    Description = "The id of the pdf reader instance and its canvas element(s).",
                },
            ]
        }
    ];



    private bool basicIsOpen;
    private bool rtlIsOpen;
    private List<BitNavItem> basicNavItems =
        [
            new()
            {
                Text = "Home",
                IconName = BitIconName.Home,
                Url = "HomePage",
            },
            new()
            {
                Text = "AdminPanel",
                IconName = BitIconName.Admin,
                ChildItems =
                [
                    new() {
                        Text = "Dashboard",
                        IconName = BitIconName.BarChartVerticalFill,
                        Url = "DashboardPage",
                    },
                    new() {
                        Text = "Categories",
                        IconName = BitIconName.BuildQueue,
                        Url = "CategoriesPage",
                    },
                    new() {
                        Text = "Products",
                        IconName = BitIconName.Product,
                        Url = "ProductsPage",
                    }
                ]
            },
            new()
            {
                Text = "Todo",
                IconName = BitIconName.ToDoLogoOutline,
                Url = "TodoPage",
            },
            new()
            {
                Text = "Settings",
                IconName = BitIconName.Equalizer,
                Url = "SettingsPage"
            },
            new()
            {
                Text = "Terms",
                IconName = BitIconName.EntityExtraction,
                Url = "TermsPage",
            }
        ];



    private readonly string example1RazorCode = @"
";
    private readonly string example1CsharpCode = @"
";
}
