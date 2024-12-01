namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarCustomDemo
{
    private static readonly List<MenuItem> basicNavBarCustoms =
    [
        new() { Title = "Home", Icon = BitIconName.Home  },
        new() { Title = "Products", Icon = BitIconName.ProductVariant },
        new() { Title = "Academy", Icon = BitIconName.LearningTools },
        new() { Title = "Profile", Icon = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> basicNavBarCustomsDisabled =
    [
        new() { Title = "Home", Icon = BitIconName.Home  },
        new() { Title = "Products", Icon = BitIconName.ProductVariant },
        new() { Title = "Academy", Icon = BitIconName.LearningTools, Disabled = true },
        new() { Title = "Profile", Icon = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> basicNavBarCustomsClassStyle =
    [
        new() { Title = "Home", Icon = BitIconName.Home  },
        new() { Title = "Products", Icon = BitIconName.ProductVariant, CssClass = "custom-item" },
        new() { Title = "Academy", Icon = BitIconName.LearningTools, Style = "color: #b6ff00;font-weight: 600;" },
        new() { Title = "Profile", Icon = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> rtlCustomsItems =
    [
        new() { Title = "خانه", Icon = BitIconName.Home  },
        new() { Title = "محصولات", Icon = BitIconName.ProductVariant },
        new() { Title = "آکادمی", Icon = BitIconName.LearningTools },
        new() { Title = "پروفایل", Icon = BitIconName.Contact },
    ];

    private static IEnumerable<BitChoiceGroupItem<MenuItem>> choiceGroupItems =
         basicNavBarCustoms.Select(i => new BitChoiceGroupItem<MenuItem>() { Id = i.Title, Text = i.Title, IsEnabled = true, Value = i });

    private int countClick;
    private bool reselectable = true;
    private MenuItem selectedItem = basicNavBarCustoms[0];
    private MenuItem twoWaySelectedItem = basicNavBarCustoms[0];
    private MenuItem? eventsClickedItem;
    private MenuItem advancedSelectedItem = basicNavBarCustoms[1];
}
