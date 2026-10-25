using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives the two other APIs of BitAccordionList at /e2e/accordion-list/apis in a real browser: a custom item class
/// mapped through NameSelectors (by selector and by member name - the key, title, description, body, disabled,
/// read-only, expanded, class, style, every icon, the hidden expander, the header's accessible name, the actions, the
/// three templates and the click callback), the bound ExpandedKey and the methods over it; and BitAccordionListOption
/// children (every parameter, its OnClick, an option added after the first render, the Options alias, generated keys,
/// the bound ExpandedKeys, ExpandAll / CollapseAll and an empty option list's EmptyContent).
/// </summary>
[TestClass]
public class BitAccordionListApisE2ETests : E2ETestBase
{
    private const string Route = "/e2e/accordion-list/apis";

    private ILocator Items(string list) => Page.Locator($"#{list} > .bit-acl-itm > .bit-acd");

    private ILocator Item(string list, int index) => Items(list).Nth(index);

    private ILocator Header(string list, int index) => Item(list, index).Locator(".bit-acd-hdr");

    private ILocator Panel(string list, int index) => Item(list, index).Locator(".bit-acd-con");

    private ILocator Log => Page.Locator("#api-log");

    private async Task ExpectExpandedAsync(string list, int index, bool expanded)
    {
        await Expect(Header(list, index)).ToHaveAttributeAsync("aria-expanded", expanded ? "true" : "false");
        if (expanded) await Expect(Panel(list, index)).ToBeVisibleAsync();
        else await Expect(Panel(list, index)).ToBeHiddenAsync();
    }

    // ---------- custom ----------

    [TestMethod]
    public async Task Custom_TheSelectors_MapEveryMemberOfTheItem()
    {
        await OpenAsync(Route);

        await Expect(Items("custom-list")).ToHaveCountAsync(7);
        var alpha = Item("custom-list", 0);
        await Expect(alpha.Locator(".bit-acd-ttl")).ToHaveTextAsync("Alpha");
        await Expect(alpha.Locator(".bit-acd-des")).ToHaveTextAsync("The first");
        await Expect(alpha.Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bit-icon--Settings"));
        await Expect(alpha).ToHaveClassAsync(HasClass("custom-alpha"));
        await Expect(alpha).ToHaveCSSAsync("margin-left", "9px");

        // The bound key opened alpha.
        await ExpectExpandedAsync("custom-list", 0, true);
        await Expect(Panel("custom-list", 0).Locator(".custom-alpha-body")).ToHaveTextAsync("Alpha body");

        await Expect(Item("custom-list", 1).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(Item("custom-list", 1).Locator(".custom-beta-action")).ToHaveCountAsync(1);

        await Expect(Header("custom-list", 2)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("custom-list", 2)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Header("custom-list", 3)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("custom-list", 3)).ToHaveAttributeAsync("tabindex", "0");

        var eps = Item("custom-list", 4);
        await Expect(Header("custom-list", 4)).ToHaveAttributeAsync("aria-label", "Epsilon by label");
        await Expect(eps.Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bi-star"));
        await Expect(eps.Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bi-plus"));
        await Expect(Item("custom-list", 5).Locator(".bit-acd-eiw")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Custom_AClick_RaisesTheItemsCallbackAndTheBoundKeyFollows()
    {
        await OpenAsync(Route);

        await Header("custom-list", 0).ClickAsync();
        await Expect(Log).ToHaveTextAsync("alpha,clicked:alpha");
        await Expect(Page.Locator("#custom-key")).ToHaveTextAsync("null");
        await ExpectExpandedAsync("custom-list", 0, false);

        await Header("custom-list", 1).ClickAsync();
        await Expect(Page.Locator("#custom-key")).ToHaveTextAsync("beta");
        await Expect(Item("custom-list", 1).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Remove"));

        await Header("custom-list", 4).ClickAsync();
        await Expect(Page.Locator("#custom-key")).ToHaveTextAsync("eps");
        await Expect(Item("custom-list", 4).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bi-dash"));
        await ExpectExpandedAsync("custom-list", 1, false);
    }

    [TestMethod]
    public async Task Custom_DisabledReadOnlyAndActions_BehaveAsTheyMap()
    {
        await OpenAsync(Route);

        await Header("custom-list", 2).DispatchEventAsync("click");
        await ExpectExpandedAsync("custom-list", 2, false);
        await Header("custom-list", 3).ClickAsync(new() { Force = true });
        await Expect(Log).ToHaveTextAsync("delta");
        await ExpectExpandedAsync("custom-list", 3, false);

        await Item("custom-list", 1).Locator(".custom-beta-action").ClickAsync();
        await Expect(Log).ToHaveTextAsync("delta,action:beta");
        await ExpectExpandedAsync("custom-list", 1, false);
    }

    [TestMethod]
    public async Task Custom_MethodsAndKeyboard_WorkOverTheMappedKeys()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-custom-toggle").ClickAsync();
        await ExpectExpandedAsync("custom-list", 1, true);
        await ExpectExpandedAsync("custom-list", 0, false);
        await Expect(Page.Locator("#custom-key")).ToHaveTextAsync("beta");

        await Page.Locator("#btn-custom-focus").ClickAsync();
        await Expect(Header("custom-list", 6)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Header("custom-list", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("custom-list", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("custom-list", 3)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Custom_SelectorsByMemberName_MapTheItemToo()
    {
        await OpenAsync(Route);

        await Expect(Item("custom-names-list", 0).Locator(".bit-acd-ttl")).ToHaveTextAsync("Named one");
        await Expect(Item("custom-names-list", 0).Locator(".bit-acd-des")).ToHaveTextAsync("By name");
        await ExpectExpandedAsync("custom-names-list", 1, true);
        await Expect(Panel("custom-names-list", 1)).ToHaveTextAsync("Named two body");

        await Header("custom-names-list", 0).ClickAsync();
        await ExpectExpandedAsync("custom-names-list", 0, true);
        await ExpectExpandedAsync("custom-names-list", 1, false);
    }

    [TestMethod]
    public async Task Custom_Templates_ReplaceTheirParts()
    {
        await OpenAsync(Route);

        await Expect(Item("custom-templates-list", 0).Locator(".bit-acd-ttl .tpl-caption")).ToHaveTextAsync("[Templated title]");
        await Expect(Item("custom-templates-list", 0).Locator(".bit-acd-eiw .tpl-arrow")).ToHaveTextAsync("v");
        await Expect(Item("custom-templates-list", 1).Locator(".bit-acd-hdr .tpl-head")).ToHaveTextAsync("Head of Templated header");
        await Expect(Item("custom-templates-list", 1).Locator(".bit-acd-ttl")).ToHaveCountAsync(0);
    }

    // ---------- options ----------

    [TestMethod]
    public async Task Option_EveryParameter_ReachesItsItem()
    {
        await OpenAsync(Route);

        await Expect(Items("option-list")).ToHaveCountAsync(6);
        var one = Item("option-list", 0);
        await Expect(one.Locator(".bit-acd-ttl")).ToHaveTextAsync("One");
        await Expect(one.Locator(".bit-acd-des")).ToHaveTextAsync("First option");
        await Expect(one.Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bit-icon--Settings"));
        await Expect(one).ToHaveClassAsync(HasClass("opt-one"));
        await Expect(one).ToHaveCSSAsync("margin-left", "7px");
        await Expect(Panel("option-list", 0).Locator(".opt-one-body")).ToHaveCountAsync(1);

        // The bound keys open Two (they win over an option's own IsExpanded, which opens B of a list without them).
        await ExpectExpandedAsync("option-list", 1, true);
        await ExpectExpandedAsync("option-isexpanded-list", 1, true);
        await ExpectExpandedAsync("option-isexpanded-list", 0, false);
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("two");
        await Expect(Item("option-list", 1).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Remove"));

        await Expect(Header("option-list", 2)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("option-list", 2)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Header("option-list", 3)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("option-list", 3)).ToHaveAttributeAsync("aria-label", "The fourth option");
        await Expect(Item("option-list", 3).Locator(".bit-acd-eiw")).ToHaveCountAsync(0);

        var five = Item("option-list", 4);
        await Expect(five.Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bi-star"));
        await Expect(five.Locator(".opt-five-title")).ToHaveTextAsync("Five!");
        await Expect(five.Locator(".opt-five-expander")).ToHaveTextAsync("+");
        await Expect(five.Locator(".bit-acd-act .opt-five-action")).ToHaveCountAsync(1);

        await Expect(Item("option-list", 5).Locator(".opt-six-header")).ToHaveTextAsync("Header of Six");
    }

    [TestMethod]
    public async Task Option_AClick_RaisesItsOnClickAndTheBoundKeysFollow()
    {
        await OpenAsync(Route);

        await Header("option-list", 0).ClickAsync();
        await Expect(Log).ToHaveTextAsync("click:one,expand:one");
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("one|two");

        await Header("option-list", 4).ClickAsync();
        await Expect(Panel("option-list", 4).Locator(".opt-five-body")).ToHaveTextAsync("Body of Five");
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("one|two|five");

        await Item("option-list", 4).Locator(".opt-five-action").ClickAsync();
        await Expect(Log).ToHaveTextAsync("click:one,expand:one,expand:five,action:five");

        await Page.Locator("#btn-option-collapse-all").ClickAsync();
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("");
        await Page.Locator("#btn-option-expand-all").ClickAsync();
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("one|two|four|five|six");
        await ExpectExpandedAsync("option-list", 2, false);
    }

    [TestMethod]
    public async Task Option_AddedAfterTheFirstRender_TakesItsPlaceInTheMarkupOrder()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-add-option").ClickAsync();
        await Expect(Items("option-list")).ToHaveCountAsync(7);
        await Expect(Item("option-list", 1).Locator(".bit-acd-ttl")).ToHaveTextAsync("Late");
        await Header("option-list", 1).ClickAsync();
        await Expect(Panel("option-list", 1).Locator(".opt-late-body")).ToBeVisibleAsync();
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("late|two");

        // The keyboard walks the headers in the order they are rendered in.
        await Header("option-list", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("option-list", 1)).ToBeFocusedAsync();

        await Page.Locator("#btn-add-option").ClickAsync();
        await Expect(Items("option-list")).ToHaveCountAsync(6);
        await Expect(Page.Locator("#option-keys")).ToHaveTextAsync("two");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Option_TheOptionsAliasAndGeneratedKeys_WorkLikeChildContent()
    {
        await OpenAsync(Route);

        await Expect(Items("option-alias-list")).ToHaveCountAsync(2);
        await ExpectExpandedAsync("option-alias-list", 1, true);
        await Expect(Panel("option-alias-list", 1).Locator(".gen-one")).ToHaveTextAsync("One");
        await ExpectExpandedAsync("option-alias-list", 0, false);
    }

    [TestMethod]
    public async Task Option_AnEmptyList_ShowsItsEmptyContentAfterTheOptionsHadTheirTurn()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#option-empty-content")).ToHaveTextAsync("No options");
        await Page.Locator("#btn-toggle-options").ClickAsync();
        await Expect(Items("option-empty-list")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#option-empty-content")).ToHaveCountAsync(0);
        await Page.Locator("#btn-toggle-options").ClickAsync();
        await Expect(Page.Locator("#option-empty-content")).ToBeVisibleAsync();
        await AssertNoServerErrorsAsync();
    }
}
