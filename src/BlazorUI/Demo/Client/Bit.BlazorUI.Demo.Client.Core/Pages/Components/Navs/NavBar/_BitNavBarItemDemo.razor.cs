namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.NavBar;

public partial class _BitNavBarItemDemo
{
    private static readonly List<BitNavBarItem> basicNavBarItems =
    [
        new() { Text = "Home", IconName = BitIconName.Home, Url = "https://bitplatform.dev/" },
        new() { Text = "BlazorUI", IconName = BitIconName.F12DevTools, Url = "https://bitplatform.dev/components" },
        new() { Text = "Academy", IconName = BitIconName.LearningTools, Url = "https://bitplatform.dev/#", IsEnabled = false },
        new() { Text = "Contact", IconName = BitIconName.Contact, Url = "https://bitplatform.dev/contact-us" },
    ];

    private readonly string example1RazorCode = @"
";
    private readonly string example1CsharpCode = @"
";
}
