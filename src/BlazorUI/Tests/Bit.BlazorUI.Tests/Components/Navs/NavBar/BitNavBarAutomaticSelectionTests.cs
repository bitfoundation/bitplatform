using System.Linq;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Navs.NavBar;

[TestClass]
public class BitNavBarAutomaticSelectionTests : BunitTestContext
{
    [TestMethod]
    public void BitNavBarShouldSelectItemMatchingCurrentUrlInAutomaticMode()
    {
        // In Automatic mode the options request the URL match as they register, but the match is now
        // deferred to a single pass in OnAfterRender (to avoid an O(n^2) match-per-registration mount).
        // This verifies that deferred pass still selects the option whose Url matches the current URL.
        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.NavigateTo("/profile");

        var component = RenderComponent<BitNavBarAutomaticTest>();

        var items = component.FindAll(".bit-nbr-itm");
        Assert.AreEqual(3, items.Count);
        Assert.IsFalse(items[0].ClassList.Contains("bit-nbr-sel"));
        Assert.IsTrue(items[1].ClassList.Contains("bit-nbr-sel"));
        Assert.IsFalse(items[2].ClassList.Contains("bit-nbr-sel"));
    }
}
