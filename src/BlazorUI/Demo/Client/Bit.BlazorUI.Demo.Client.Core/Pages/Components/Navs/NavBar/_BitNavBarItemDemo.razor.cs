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



    private readonly string example1RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" />";
    private readonly string example1CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example2RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" IsEnabled=""false"" />

<BitNavBar Items=""basicNavBarItemsDisabled"" />";
    private readonly string example2CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> basicNavBarItemsDisabled =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools, IsEnabled = false },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";
}
