namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarItemDemo
{
    private static readonly List<BitNavBarItem> basicNavBarItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home  },
        new() { Text = "Products", IconName = BitIconName.ProductVariant },
        new() { Text = "Academy", IconName = BitIconName.LearningTools },
        new() { Text = "Profile", IconName = BitIconName.Contact },
    ];

    private static readonly List<BitNavBarItem> basicNavBarItemsDisabled =
    [
        new() { Text = "Home", IconName = BitIconName.Home  },
        new() { Text = "Products", IconName = BitIconName.ProductVariant },
        new() { Text = "Academy", IconName = BitIconName.LearningTools, IsEnabled = false },
        new() { Text = "Profile", IconName = BitIconName.Contact },
    ];

    private static readonly List<BitNavBarItem> styleClassItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home  },
        new() { Text = "Products", IconName = BitIconName.ProductVariant, Class = "custom-item" },
        new() { Text = "Academy", IconName = BitIconName.LearningTools, Style = "color: #b6ff00;font-weight: 600;" },
        new() { Text = "Profile", IconName = BitIconName.Contact },
    ];

    private static readonly List<BitNavBarItem> rtlItems =
    [
        new() { Text = "خانه", IconName = BitIconName.Home  },
        new() { Text = "محصولات", IconName = BitIconName.ProductVariant },
        new() { Text = "آکادمی", IconName = BitIconName.LearningTools },
        new() { Text = "پروفایل", IconName = BitIconName.Contact },
    ];

    private static IEnumerable<BitChoiceGroupItem<BitNavBarItem>> choiceGroupItems =
         basicNavBarItems.Select(i => new BitChoiceGroupItem<BitNavBarItem>() { Id = i.Text, Text = i.Text, IsEnabled = i.IsEnabled, Value = i });

    private int countClick;
    private bool reselectable = true;
    private BitNavBarItem selectedItem = basicNavBarItems[0];
    private BitNavBarItem twoWaySelectedItem = basicNavBarItems[0];
    private BitNavBarItem? eventsClickedItem;
    private BitNavBarItem advancedSelectedItem = basicNavBarItems[1];
}
