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

    private static readonly List<BitNavBarItem> exactMatchItems =
    [
        new() { Text = "NavBar", IconName = BitIconName.GlobalNavButton, Url = "/components/navbar" },
        new() { Text = "Nav", IconName = BitIconName.BulletedList, Url = "/components/nav" },
    ];

    private static readonly List<BitNavBarItem> prefixMatchItems =
    [
        new() { Text = "Components", IconName = BitIconName.F12DevTools, Url = "/components" },
        new() { Text = "Iconography", IconName = BitIconName.AppIconDefault, Url = "/iconography" },
    ];

    // The URL of a Wildcard or a Regex item is a pattern rather than a route, so these items are disabled:
    // they still light up on a match, but a click cannot navigate to a URL no page answers.
    private static readonly List<BitNavBarItem> patternMatchItems =
    [
        new() { Text = "/components/*", IconName = BitIconName.F12DevTools, Url = "/components/*", Match = BitNavMatch.Wildcard, IsEnabled = false },
        new() { Text = "^/components/n", IconName = BitIconName.Code, Url = "^/components/n", Match = BitNavMatch.Regex, IsEnabled = false },
    ];

    private static readonly List<BitNavBarItem> additionalUrlsItems =
    [
        new() { Text = "Navs", IconName = BitIconName.GlobalNavButton, Url = "/components/nav", AdditionalUrls = ["/components/navbar", "/components/breadcrumb"] },
        new() { Text = "Buttons", IconName = BitIconName.ButtonControl, Url = "/components/button", AdditionalUrls = ["/components/togglebutton"] },
    ];

    private static readonly List<BitNavBarItem> badgeNavBarItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home  },
        new() { Text = "Inbox", IconName = BitIconName.Mail, Badge = "12" },
        new() { Text = "Alerts", IconName = BitIconName.Ringer, Badge = "99+" },
        new() { Text = "Profile", IconName = BitIconName.Contact, Dot = true },
    ];

    private static readonly List<BitNavBarItem> styleClassItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home  },
        new() { Text = "Products", IconName = BitIconName.ProductVariant, Class = "custom-item" },
        new() { Text = "Academy", IconName = BitIconName.LearningTools, Style = "color: #b6ff00;font-weight: 600;" },
        new() { Text = "Profile", IconName = BitIconName.Contact },
    ];

    private static readonly List<BitNavBarItem> externalIconItems =
    [
        new() { Text = "Home", Icon = "fa-solid fa-house" },
        new() { Text = "Products", Icon = BitIconInfo.Css("fa-solid fa-box") },
        new() { Text = "Academy", Icon = BitIconInfo.Fa("solid graduation-cap") },
        new() { Text = "Profile", Icon = BitIconInfo.Fa("solid user") },
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
    private BitNavBarItem? eventsSelectedItem;
    private BitNavBarItem advancedSelectedItem = basicNavBarItems[1];
}
