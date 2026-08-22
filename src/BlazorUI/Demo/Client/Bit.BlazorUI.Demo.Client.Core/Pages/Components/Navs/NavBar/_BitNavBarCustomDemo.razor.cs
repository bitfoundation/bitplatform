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

    private static readonly List<MenuItem> exactMatchCustoms =
    [
        new() { Title = "NavBar", ImageName = BitIconName.GlobalNavButton, Link = "/components/navbar" },
        new() { Title = "Nav", ImageName = BitIconName.BulletedList, Link = "/components/nav" },
    ];

    private static readonly List<MenuItem> prefixMatchCustoms =
    [
        new() { Title = "Components", ImageName = BitIconName.F12DevTools, Link = "/components" },
        new() { Title = "Iconography", ImageName = BitIconName.AppIconDefault, Link = "/iconography" },
    ];

    // The URL of a Wildcard or a Regex item is a pattern rather than a route, so these items are disabled:
    // they still light up on a match, but a click cannot navigate to a URL no page answers.
    private static readonly List<MenuItem> patternMatchCustoms =
    [
        new() { Title = "/components/*", ImageName = BitIconName.F12DevTools, Link = "/components/*", Matching = BitNavMatch.Wildcard, Disabled = true },
        new() { Title = "^/components/b", ImageName = BitIconName.Code, Link = "^/components/b", Matching = BitNavMatch.Regex, Disabled = true },
    ];

    private static readonly List<MenuItem> additionalUrlsCustoms =
    [
        new() { Title = "Navs", ImageName = BitIconName.GlobalNavButton, Link = "/components/nav", ExtraLinks = ["/components/navbar", "/components/breadcrumb"] },
        new() { Title = "Buttons", ImageName = BitIconName.ButtonControl, Link = "/components/button", ExtraLinks = ["/components/togglebutton"] },
    ];

    private static readonly List<MenuItem> unevenNavBarCustoms =
    [
        new() { Title = "Home", ImageName = BitIconName.Home  },
        new() { Title = "Products & services", ImageName = BitIconName.ProductVariant },
        new() { Title = "Academy", ImageName = BitIconName.LearningTools },
        new() { Title = "Me", ImageName = BitIconName.Contact },
    ];

    private static readonly List<MenuItem> badgeNavBarCustoms =
    [
        new() { Title = "Home", ImageName = BitIconName.Home  },
        new() { Title = "Inbox", ImageName = BitIconName.Mail, Counter = "12" },
        new() { Title = "Alerts", ImageName = BitIconName.Ringer, Counter = "99+", CounterLabel = "more than 99 unread alerts" },
        new() { Title = "Profile", ImageName = BitIconName.Contact, Marker = true, CounterLabel = "needs attention" },
    ];

    private static readonly List<MenuItem> selectedIconCustoms =
    [
        new() { Title = "Home", ImageName = BitIconName.Home, SelectedImageName = BitIconName.HomeSolid },
        new() { Title = "Inbox", ImageName = BitIconName.Mail, SelectedImageName = BitIconName.MailSolid },
        new() { Title = "Alerts", ImageName = BitIconName.Ringer, SelectedImageName = BitIconName.RingerSolid },
        new() { Title = "Favorites", ImageName = BitIconName.Heart, SelectedImageName = BitIconName.HeartFill },
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

    private int dynamicCustomsCount = 3;
    private MenuItem? dynamicSelectedCustom;
    private readonly List<MenuItem> dynamicNavBarCustoms =
    [
        new() { Title = "Home", ImageName = BitIconName.Home },
        new() { Title = "Products", ImageName = BitIconName.ProductVariant },
        new() { Title = "Profile", ImageName = BitIconName.Contact },
    ];

    private void AddDynamicCustom()
    {
        dynamicCustomsCount++;
        dynamicNavBarCustoms.Add(new() { Title = $"Item {dynamicCustomsCount}", ImageName = BitIconName.Tag });
    }

    private void RemoveDynamicCustom()
    {
        if (dynamicNavBarCustoms.Count == 0) return;

        dynamicNavBarCustoms.RemoveAt(dynamicNavBarCustoms.Count - 1);
    }

    private void ReverseDynamicCustoms() => dynamicNavBarCustoms.Reverse();

    private int countClick;
    private bool reselectable = true;
    private MenuItem selectedItem = basicNavBarCustoms[0];
    private MenuItem twoWaySelectedItem = basicNavBarCustoms[0];
    private MenuItem? eventsClickedItem;
    private MenuItem? eventsSelectedItem;
    private MenuItem advancedSelectedItem = basicNavBarCustoms[1];
}
