namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarItemDemo
{
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

    private readonly string example3RazorCode = @"
<BitNavBar Mode=""BitNavMode.Manual""
           Items=""basicNavBarItems""
           DefaultSelectedItem=""basicNavBarItems[0]"" />";
    private readonly string example3CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example4RazorCode = @"
<BitNavBar Color=""BitColor.Primary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Secondary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Tertiary"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Info"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Success"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Warning"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SevereWarning"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.Error"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />

<BitNavBar Color=""BitColor.PrimaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryBackground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />

<BitNavBar Color=""BitColor.PrimaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryForeground"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.PrimaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.SecondaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />
<BitNavBar Color=""BitColor.TertiaryBorder"" Items=""basicNavBarItems"" DefaultSelectedItem=""basicNavBarItems[0]"" Mode=""BitNavMode.Manual"" />";
    private readonly string example4CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example5RazorCode = @"
<BitNavBar Items=""basicNavBarItems"" IconOnly />";
    private readonly string example5CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";

    private readonly string example6RazorCode = @"
<BitNavBar Items=""basicNavBarItems"">
    <ItemTemplate Context=""item"">
        <span style=""font-size:12px"">@item.Text</span>
        <i class=""bit-icon bit-icon--@item.IconName"" />
    </ItemTemplate>
</BitNavBar>";
    private readonly string example6CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
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

<BitNavBar Items=""basicNavBarItems"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: tomato 0 0 1rem;"" />
<BitNavBar Items=""basicNavBarItems"" Class=""custom-class"" />

<BitNavBar Items=""styleClassItems"" />

<BitNavBar Items=""basicNavBarItems"" Classes=""@(new() { ItemIcon = ""custom-item-ico"", ItemText = ""custom-item-txt"" })"" />
<BitNavBar Items=""basicNavBarItems"" Styles=""@(new() { ItemIcon = ""color: aqua;"", ItemText = ""color: tomato;"" })"" />";
    private readonly string example7CsharpCode = @"
private static readonly List<BitNavBarItem> basicNavBarItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];

private static readonly List<BitNavBarItem> styleClassItems =
[
    new() { Text = ""Home"", IconName = BitIconName.Home  },
    new() { Text = ""Products"", IconName = BitIconName.ProductVariant, Class = ""custom-item"" },
    new() { Text = ""Academy"", IconName = BitIconName.LearningTools, Style = ""color: #b6ff00;font-weight: 600;"" },
    new() { Text = ""Profile"", IconName = BitIconName.Contact },
];";


    private readonly string example8RazorCode = @"
<BitNavBar Dir=""BitDir.Rtl"" Items=""rtlItems"" />";
    private readonly string example8CsharpCode = @"
private static readonly List<BitNavBarItem> rtlItems =
[
    new() { Text = ""خانه"", IconName = BitIconName.Home  },
    new() { Text = ""محصولات"", IconName = BitIconName.ProductVariant },
    new() { Text = ""آکادمی"", IconName = BitIconName.LearningTools },
    new() { Text = ""پروفایل"", IconName = BitIconName.Contact },
];";
}
