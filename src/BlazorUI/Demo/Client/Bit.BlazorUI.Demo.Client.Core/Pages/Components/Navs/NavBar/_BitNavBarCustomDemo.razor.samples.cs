namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarCustomDemo
{
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

    private readonly string example3RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example3CsharpCode = @"
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

    private readonly string example4RazorCode = @"
<BitNavBar Color=""BitColor.Primary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Secondary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Tertiary""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Info""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Success""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Warning""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SevereWarning""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.Error""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Color=""BitColor.PrimaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryBackground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Color=""BitColor.PrimaryForeground""
            Items=""basicNavBarCustoms""
            DefaultSelectedItem=""basicNavBarCustoms[0]""
            Mode=""BitNavMode.Manual""
            NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryForeground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryForeground""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.PrimaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.SecondaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Color=""BitColor.TertiaryBorder""
           Items=""basicNavBarCustoms""
           DefaultSelectedItem=""basicNavBarCustoms[0]""
           Mode=""BitNavMode.Manual""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example4CsharpCode = @"
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

    private readonly string example5RazorCode = @"
<BitNavBar IconOnly
           Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example5CsharpCode = @"
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

    private readonly string example6RazorCode = @"
<BitNavBar Items=""basicNavBarCustoms""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"">
    <ItemTemplate Context=""custom"">
        <span style=""font-size:12px"">@custom.Title</span>
        <i class=""bit-icon bit-icon--@custom.Icon"" />
    </ItemTemplate>
</BitNavBar>";
    private readonly string example6CsharpCode = @"
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

    private readonly string example7RazorCode = @"
<style>
    .custom-class {
        margin: 1rem;
        border-radius: 1rem;
        box-shadow: aqua 0 0 1rem;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-item {
        color: #ff7800;
        font-weight: 600;
    }

    .custom-item-ico {
        font-weight: bold;
        color: darkmagenta;
    }

    .custom-item-txt {
        font-weight: bold;
        font-style: italic;
    }
</style>

<BitNavBar Items=""basicNavBarCustoms""
           Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Items=""basicNavBarCustoms""
           Class=""custom-class""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />

<BitNavBar Items=""basicNavBarCustomsClassStyle""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon },
                                    Class = { Selector = item => item.CssClass },
                                    Style = { Selector = item => item.Style }})"" />

<BitNavBar Items=""basicNavBarCustoms""
           Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />
<BitNavBar Items=""basicNavBarCustoms""
           Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"" })""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                    IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example7CsharpCode = @"
public class MenuItem
{
    public string? Title { get; set; }
    public string? Icon { get; set; }
    public string? CssClass { get; set; }
    public string? Style { get; set; }
}

private static readonly List<MenuItem> basicNavBarCustoms =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];

private static readonly List<MenuItem> basicNavBarCustomsClassStyle =
[
    new() { Title = ""Home"", Icon = BitIconName.Home  },
    new() { Title = ""Products"", Icon = BitIconName.ProductVariant, CssClass = ""custom-item"" },
    new() { Title = ""Academy"", Icon = BitIconName.LearningTools, Style = ""color: #b6ff00;font-weight: 600;"" },
    new() { Title = ""Profile"", Icon = BitIconName.Contact },
];";


    private readonly string example8RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl""
           Items=""rtlCustomsItems""
           NameSelectors=""@(new() { Text = { Selector = item => item.Title },
                                     IconName = { Selector = item => item.Icon } })"" />";
    private readonly string example8CsharpCode = @"
private static readonly List<MenuItem> rtlCustomsItems =
[
    new() { Title = ""خانه"", Icon = BitIconName.Home  },
    new() { Title = ""محصولات"", Icon = BitIconName.ProductVariant },
    new() { Title = ""آکادمی"", Icon = BitIconName.LearningTools },
    new() { Title = ""پروفایل"", Icon = BitIconName.Contact },
];";
}
