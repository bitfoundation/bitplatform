using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Utilities;

/// <summary>
/// The BitParams cascade of component defaults in a real page (/e2e/params), on buttons, tags, badges and text
/// fields: a cascaded Color / Size / Variant (and a text field's Placeholder and Underlined) reaches each component
/// type through its own params object, in the prerendered HTML too, and paints it; a parameter set on the component
/// wins; the cascade changed at runtime (a list rebuilt from state, a property of an object changed in place, an
/// object removed, a value cleared) re-renders the components and gives them back their own values; a nested
/// BitParams refines its ancestors and an Isolated one ignores them (toggled at runtime too); and the base
/// parameters (Disabled with its handlers dropped, Class, Style, Dir, AriaLabel, HtmlAttributes) cascade like the rest.
/// </summary>
[TestClass]
public class BitParamsE2ETests : E2ETestBase
{
    private const string Route = "/e2e/params";

    [TestMethod]
    public async Task Prerender_TheCascadedDefaultsAreInTheServerHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "<button id=\"btn-cascaded\" class=\"bit-btn-otl bit-btn-err bit-btn-lg bit-btn\"");
        StringAssert.Contains(html, "<span id=\"tag-cascaded\" class=\"bit-tag-suc bit-tag-sm bit-tag-txt bit-tag\"");
        StringAssert.Contains(html, "id=\"status\">Loading");
    }

    [TestMethod]
    public async Task Basic_TheButtonParamsReachTheButton_AndPaintIt()
    {
        await OpenAsync(Route);

        var cascaded = Page.Locator("#btn-cascaded");
        var outside = Page.Locator("#btn-outside");

        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-btn-otl"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-btn-lg"));
        await Expect(outside).ToHaveAttributeAsync("class", "bit-btn-fil bit-btn");

        // Outline in the error color, at the large height, against the filled primary default.
        var error = await ResolveColorAsync("var(--bit-clr-err)");
        await Expect(cascaded).ToHaveCSSAsync("color", error);
        await Expect(cascaded).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        await Expect(outside).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
        Assert.IsTrue((await cascaded.BoundingBoxAsync())!.Height > (await outside.BoundingBoxAsync())!.Height, "The cascaded Large size did not make the button taller.");
    }

    [TestMethod]
    public async Task Basic_AParameterSetOnTheComponentWins_TheRestStillCascades()
    {
        await OpenAsync(Route);

        var own = Page.Locator("#btn-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-txt"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-inf"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-btn-err"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-lg"));
        await Expect(own).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-inf)"));

        await Expect(Page.Locator("#tag-own")).ToHaveClassAsync(HasClass("bit-tag-lg"));
        await Expect(Page.Locator("#tag-own")).ToHaveClassAsync(HasClass("bit-tag-suc"));
        Assert.IsTrue((await Page.Locator("#tag-own").BoundingBoxAsync())!.Height > (await Page.Locator("#tag-cascaded").BoundingBoxAsync())!.Height);
    }

    [TestMethod]
    public async Task Basic_EachComponentTypeTakesItsOwnParamsObject()
    {
        await OpenAsync(Route);

        var tag = Page.Locator("#tag-cascaded");
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-suc"));
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-sm"));
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-txt"));
        await Expect(tag).ToHaveCSSAsync("color", await ResolveColorAsync("var(--bit-clr-suc)"));
        await Expect(Page.Locator("#tag-outside")).ToHaveAttributeAsync("class", "bit-tag-fil bit-tag");

        var badge = Page.Locator("#badge-cascaded");
        await Expect(badge).ToHaveClassAsync(HasClass("bit-bdg-wrn"));
        await Expect(badge).ToHaveClassAsync(HasClass("bit-bdg-otl"));
        await Expect(Page.Locator("#badge-outside")).Not.ToHaveClassAsync(HasClass("bit-bdg-wrn"));

        await Expect(Page.Locator("#tf-cascaded input")).ToHaveAttributeAsync("placeholder", "Cascaded placeholder");
        await Expect(Page.Locator("#tf-cascaded")).ToHaveClassAsync(HasClass("bit-tfl-und"));
        await Expect(Page.Locator("#tf-own input")).ToHaveAttributeAsync("placeholder", "Own placeholder");
        await Expect(Page.Locator("#tf-own")).ToHaveClassAsync(HasClass("bit-tfl-und"));
    }

    [TestMethod]
    public async Task Runtime_AListRebuiltFromState_ReachesTheComponents_AndANullGivesThemBackTheirOwn()
    {
        await OpenAsync(Route);

        var button = Page.Locator("#btn-compact-target");
        var tag = Page.Locator("#tag-compact-target");
        var own = Page.Locator("#btn-compact-own");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-sm"));
        var heightBefore = (await button.BoundingBoxAsync())!.Height;

        await Page.ClickAsync("#btn-compact");
        await Expect(Page.Locator("#compact")).ToHaveTextAsync("True");

        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-sm"));
        await Expect(tag).ToHaveClassAsync(HasClass("bit-tag-sm"));
        await Expect(own).ToHaveClassAsync(HasClass("bit-btn-lg"));
        Assert.IsTrue((await button.BoundingBoxAsync())!.Height < heightBefore, "The cascaded Small size did not make the button shorter.");

        await Page.ClickAsync("#btn-compact");
        await Expect(Page.Locator("#compact")).ToHaveTextAsync("False");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-sm"));
        await Expect(tag).Not.ToHaveClassAsync(HasClass("bit-tag-sm"));
        Assert.AreEqual(heightBefore, (await button.BoundingBoxAsync())!.Height, 0.5);
    }

    [TestMethod]
    public async Task Runtime_APropertyOfAParamsObjectChangedInPlace_ReachesTheComponents()
    {
        await OpenAsync(Route);

        var button = Page.Locator("#btn-runtime");
        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-ter"));
        await Expect(button).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-ter)"));

        await Page.ClickAsync("#btn-mutate");

        await Expect(button).ToHaveClassAsync(HasClass("bit-btn-wrn"));
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-btn-ter"));
        await Expect(button).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-wrn)"));
    }

    [TestMethod]
    public async Task Runtime_ARemovedParamsObjectAndAClearedValue_GiveTheComponentsBackTheirOwnValues()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#tag-runtime")).ToHaveClassAsync(HasClass("bit-tag-err"));
        await Page.ClickAsync("#btn-remove-tag-params");
        await Expect(Page.Locator("#tag-runtime")).ToHaveAttributeAsync("class", "bit-tag-fil bit-tag");
        await Expect(Page.Locator("#btn-runtime")).ToHaveClassAsync(HasClass("bit-btn-ter"));

        await Page.ClickAsync("#btn-clear-color");
        await Expect(Page.Locator("#btn-runtime")).ToHaveAttributeAsync("class", "bit-btn-fil bit-btn");
        await Expect(Page.Locator("#btn-runtime")).ToHaveCSSAsync("background-color", await ResolveColorAsync("var(--bit-clr-pri)"));
    }

    [TestMethod]
    public async Task Nesting_ANestedBitParamsRefinesItsAncestors_AndIsolatedIgnoresThem()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#btn-outer")).ToHaveAttributeAsync("class", "bit-btn-otl bit-btn-sm bit-btn");
        await Expect(Page.Locator("#btn-nested")).ToHaveAttributeAsync("class", "bit-btn-otl bit-btn-err bit-btn-sm bit-btn");
        await Expect(Page.Locator("#btn-isolated")).ToHaveAttributeAsync("class", "bit-btn-fil bit-btn-suc bit-btn");
        await Expect(Page.Locator("#btn-isolated-empty")).ToHaveAttributeAsync("class", "bit-btn-fil bit-btn");

        // No longer isolated: the ancestors' Variant and Size come back under its own Color.
        await Page.ClickAsync("#btn-toggle-isolated");
        await Expect(Page.Locator("#isolated")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#btn-isolated")).ToHaveAttributeAsync("class", "bit-btn-otl bit-btn-suc bit-btn-sm bit-btn");

        await Page.ClickAsync("#btn-toggle-isolated");
        await Expect(Page.Locator("#btn-isolated")).ToHaveAttributeAsync("class", "bit-btn-fil bit-btn-suc bit-btn");
    }

    [TestMethod]
    public async Task Base_ACascadedDisabledDisablesTheComponents_AndARealClickRunsNoHandler()
    {
        await OpenAsync(Route);

        var button = Page.Locator("#btn-base");
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(button).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Page.Locator("#tf-base")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Page.Locator("#tf-base input")).ToBeDisabledAsync();

        await button.ClickAsync(new() { Force = true });
        await Page.ClickAsync("#btn-base-own");
        await Expect(Page.Locator("#base-own-clicks")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#base-clicks")).ToHaveTextAsync("0");

        // The cascade stops setting it: the button goes back to its own (enabled) value.
        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(button).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await button.ClickAsync();
        await Expect(Page.Locator("#base-clicks")).ToHaveTextAsync("1");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(button).ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Base_ClassStyleDirAriaLabelAndHtmlAttributesCascade_AndTheComponentsOwnWin()
    {
        await OpenAsync(Route);

        var button = Page.Locator("#btn-base");
        await Expect(button).ToHaveClassAsync(HasClass("e2e-prm-cascaded"));
        await Expect(button).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("margin-left", "3px");
        await Expect(button).ToHaveAttributeAsync("dir", "rtl");
        await Expect(button).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(button).ToHaveAttributeAsync("aria-label", "Cascaded label");
        await Expect(button).ToHaveAttributeAsync("data-cascaded", "yes");

        var own = Page.Locator("#btn-base-own");
        await Expect(own).ToHaveClassAsync(HasClass("e2e-prm-own"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("e2e-prm-cascaded"));
        await Expect(own).ToHaveCSSAsync("margin-left", "7px");
        await Expect(own).ToHaveAttributeAsync("dir", "ltr");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-dis"));
        // What it does not set itself still comes from the cascade.
        await Expect(own).ToHaveAttributeAsync("aria-label", "Cascaded label");
        await Expect(own).ToHaveAttributeAsync("data-cascaded", "yes");
    }

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", value);
    }
}
