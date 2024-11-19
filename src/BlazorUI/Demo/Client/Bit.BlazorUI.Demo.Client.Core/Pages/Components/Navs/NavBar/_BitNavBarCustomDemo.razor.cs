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



    private readonly string example1RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example1CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";

    private readonly string example2RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms"" IsEnabled=""false""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Items=""basicNavBarCustomsDisabled""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon },
                                    IsEnabled = { Selector = item => item.Disabled is false } })"" />";
    private readonly string example2CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsDisabled =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools, Disabled = true },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";
}
