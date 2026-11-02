using System.Resources;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.WorkbenchTests.Features.Catalog;

/// <summary>
/// A page is added to the Workbench through its catalog entry, so every entry must name texts that exist and design
/// options and switches that survive a link.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class WorkbenchPageCatalogTests
{
    private static readonly ResourceManager strings = new(typeof(WorkbenchStrings));

    private static readonly Regex token = new("^[a-z0-9-]{1,32}$");

    [TestMethod]
    public void EveryPage_Should_HaveAKeyOfItsOwnThatALinkCanCarry()
    {
        var keys = WorkbenchPageCatalog.Pages.Select(p => p.Key).ToArray();

        Assert.AreAllDistinct(keys);

        foreach (var key in keys)
        {
            Assert.MatchesRegex(token, key);
            var found = WorkbenchPageCatalog.Find(key.ToUpperInvariant());

            Assert.IsNotNull(found);
            Assert.AreEqual(key, found.Key);
        }

        Assert.IsNull(WorkbenchPageCatalog.Find("missing"));
        Assert.IsNull(WorkbenchPageCatalog.Find(null));
    }

    [TestMethod]
    public void EveryText_Should_ExistInTheWorkbenchStrings()
    {
        foreach (var page in WorkbenchPageCatalog.Pages)
        {
            string?[] keys = [page.TitleKey, page.DescriptionKey, page.CategoryKey, page.CaptionKey,
                              .. page.Variants.Select(v => v.NameKey),
                              .. page.Switches.Select(s => s.NameKey),
                              .. page.Switches.SelectMany(s => s.Options).Select(o => o.TextKey)];

            foreach (var key in keys.OfType<string>())
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(strings.GetString(key, CultureInfoManager.DefaultCulture)), $"{page.Key} names {key}, which WorkbenchStrings lacks.");
            }
        }
    }

    [TestMethod]
    public void EveryDesignOptionAndSwitch_Should_SurviveALink()
    {
        foreach (var page in WorkbenchPageCatalog.Pages)
        {
            foreach (var variant in page.Variants)
            {
                Assert.AreEqual(variant.Key, WorkbenchScenario.Parse($"?variant={variant.Key}").Variant, $"{page.Key}'s {variant.Key} design option is dropped from links.");
            }

            Assert.AreAllDistinct(page.Switches.Select(s => s.Key).ToArray());

            foreach (var pageSwitch in page.Switches)
            {
                Assert.AreAllDistinct(pageSwitch.Options.Select(o => o.Value).ToArray());

                foreach (var option in pageSwitch.Options)
                {
                    var scenario = WorkbenchScenario.Parse($"?sw-{pageSwitch.Key}={option.Value}");

                    Assert.AreEqual(option, pageSwitch.Resolve(scenario), $"{page.Key}'s {pageSwitch.Key}={option.Value} is dropped from links.");
                }
            }
        }
    }

    [TestMethod]
    public void TheFirstOption_Should_BeTheDefault()
    {
        var buildings = WorkbenchPageCatalog.Find("buildings")!;
        var amount = buildings.Switches.Single(s => s.Key == "amount");

        Assert.AreEqual("table", buildings.ResolveVariant(new())!.Key);
        Assert.AreEqual("cards", buildings.ResolveVariant(new() { Variant = "cards" })!.Key);
        Assert.AreEqual("table", buildings.ResolveVariant(new() { Variant = "unknown" })!.Key);
        Assert.AreEqual(amount.Options[0], amount.Resolve(new()));
        Assert.AreEqual(amount.Options[0], amount.Resolve(new WorkbenchScenario().WithSwitch("amount", "unknown")));
        Assert.IsNull(WorkbenchPageCatalog.Find("theme")!.ResolveVariant(new()));
    }

    [TestMethod]
    public void EveryPage_Should_BeAComponentWithoutARouteOfItsOwn()
    {
        foreach (var page in WorkbenchPageCatalog.Pages)
        {
            Assert.IsTrue(typeof(IComponent).IsAssignableFrom(page.ComponentType), $"{page.Key} isn't a component.");
            Assert.IsEmpty(page.ComponentType.GetCustomAttributes(typeof(RouteAttribute), inherit: false), $"{page.Key} has a route; pages are shown through /preview.");
            Assert.IsGreaterThan(0, page.Showcases.Length);
        }
    }
}
