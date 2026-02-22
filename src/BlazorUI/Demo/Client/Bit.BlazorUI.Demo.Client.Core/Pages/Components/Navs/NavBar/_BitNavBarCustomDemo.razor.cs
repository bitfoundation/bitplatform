namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarCustomDemo
{
    private static readonly List<MenuItem> basicNavBarCustoms =
    [
        new() { Title = "Home", ImageName = BitIconName.Home  },
        new() { Title = "Products", ImageName = BitIconName.ProductVariant },
        new() { Title = "Academy", ImageName = BitIconName.LearningTools },
        new() { Title = "Profile", ImageName = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> basicNavBarCustomsDisabled =
    [
        new() { Title = "Home", ImageName = BitIconName.Home  },
        new() { Title = "Products", ImageName = BitIconName.ProductVariant },
        new() { Title = "Academy", ImageName = BitIconName.LearningTools, Disabled = true },
        new() { Title = "Profile", ImageName = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> basicNavBarCustomsClassStyle =
    [
        new() { Title = "Home", ImageName = BitIconName.Home  },
        new() { Title = "Products", ImageName = BitIconName.ProductVariant, CssClass = "custom-item" },
        new() { Title = "Academy", ImageName = BitIconName.LearningTools, Style = "color: #b6ff00;font-weight: 600;" },
        new() { Title = "Profile", ImageName = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> externalIconCustoms =
    [
        new() { Title = "Home", Image = "fa-solid fa-house" },
        new() { Title = "Products", Image = BitIconInfo.Css("fa-solid fa-box") },
        new() { Title = "Academy", Image = BitIconInfo.Fa("solid graduation-cap") },
        new() { Title = "Profile", Image = BitIconInfo.Fa("solid user") },
    ];

    private static readonly List<MenuItem> rtlCustomsItems =
    [
        new() { Title = "خانه", ImageName = BitIconName.Home  },
        new() { Title = "محصولات", ImageName = BitIconName.ProductVariant },
        new() { Title = "آکادمی", ImageName = BitIconName.LearningTools },
        new() { Title = "پروفایل", ImageName = BitIconName.Contact },
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
