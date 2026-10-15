using System.Globalization;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// Drives BitAccordionList (the BitAccordionListItem API) at /e2e/accordion-list in a real browser: single and
/// multiple expand, MaxExpanded, the default and the per-item expanded keys, Collapsible, every icon parameter (the
/// swap, the rotation, the placement, hiding it, external icons), the actions, the four templates and their
/// per-item overrides, disabled and read-only items and lists, the callbacks in the order they are raised,
/// OnToggling with its reasons, refusal and busy state, both two-way bindings and every public method, LazyContent
/// and UnmountOnCollapse, MaxHeight and TransitionDuration (with reduced motion), HiddenUntilFound with a real
/// fragment reveal, ExpandOnPrint, ScrollIntoViewOnExpand in a real scroll box, Joined / Gap / NoBorder, EmptyContent,
/// the heading levels and regions, the keyboard navigation between the headers, the BitParams cascade, Background /
/// Border / Size, Style / Class / Styles / Classes of every part, the --bit-AccordionList-* and --bit-Accordion-*
/// variables, RTL, prerendering, a changing collection and unmounting. The Custom and Option APIs are covered by
/// BitAccordionListApisE2ETests.
/// </summary>
[TestClass]
public class BitAccordionListE2ETests : E2ETestBase
{
    private const string Route = "/e2e/accordion-list";

    private ILocator Items(string list) => Page.Locator($"#{list} > .bit-acl-itm > .bit-acd");

    private ILocator Item(string list, int index) => Items(list).Nth(index);

    private ILocator Header(string list, int index) => Item(list, index).Locator(".bit-acd-hdr");

    private ILocator Panel(string list, int index) => Item(list, index).Locator(".bit-acd-con");

    private ILocator Log => Page.Locator("#acl-log");

    private async Task ExpectExpandedAsync(string list, int index, bool expanded)
    {
        await Expect(Header(list, index)).ToHaveAttributeAsync("aria-expanded", expanded ? "true" : "false");

        if (expanded)
        {
            await Expect(Item(list, index)).ToHaveClassAsync(HasClass("bit-acd-exp"));
            await Expect(Panel(list, index)).ToBeVisibleAsync();
        }
        else
        {
            await Expect(Item(list, index)).Not.ToHaveClassAsync(HasClass("bit-acd-exp"));
            await Expect(Panel(list, index)).ToBeHiddenAsync();
        }
    }

    private async Task ExpectExpandedSetAsync(string list, params int[] expanded)
    {
        var count = await Items(list).CountAsync();
        for (var i = 0; i < count; i++)
        {
            await ExpectExpandedAsync(list, i, expanded.Contains(i));
        }
    }

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_TheItems_RenderAHeadingAButtonAndARegionEachAllCollapsed()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-basic")).ToHaveClassAsync(HasClass("bit-acl"));
        await Expect(Items("acl-basic")).ToHaveCountAsync(3);
        await Expect(Item("acl-basic", 0).Locator(".bit-acd-ttl")).ToHaveTextAsync("General settings");
        await Expect(Item("acl-basic", 0).Locator(".bit-acd-des")).ToHaveTextAsync("The general settings");

        for (var i = 0; i < 3; i++)
        {
            var heading = Item("acl-basic", i).Locator("[role=heading]");
            await Expect(heading).ToHaveAttributeAsync("aria-level", "3");
            var header = Header("acl-basic", i);
            await Expect(header).ToHaveAttributeAsync("type", "button");
            await Expect(header).ToHaveAttributeAsync("tabindex", "0");

            var panelId = await Panel("acl-basic", i).GetAttributeAsync("id");
            await Expect(header).ToHaveAttributeAsync("aria-controls", panelId!);
            await Expect(Panel("acl-basic", i)).ToHaveAttributeAsync("role", "region");
            await Expect(Panel("acl-basic", i)).ToHaveAttributeAsync("aria-labelledby", new Regex(".+"));
        }

        await ExpectExpandedSetAsync("acl-basic");
        await Expect(Page.Locator("#acl-basic").GetByRole(AriaRole.Button, new() { Name = "General settings" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("#acl-basic").GetByRole(AriaRole.Region)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Basic_ClickingAHeader_ExpandsItAndCollapsesTheOneThatWasOpen()
    {
        await OpenAsync(Route);

        await Header("acl-basic", 0).ClickAsync();
        await ExpectExpandedSetAsync("acl-basic", 0);
        await Expect(Panel("acl-basic", 0)).ToHaveTextAsync("General body");
        Assert.IsTrue((await Panel("acl-basic", 0).BoundingBoxAsync())!.Height > 10);

        await Header("acl-basic", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-basic", 1);

        // Collapsible by default: the open item closes from its own header.
        await Header("acl-basic", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-basic");
    }

    [TestMethod]
    public async Task Basic_EnterAndSpaceOnAHeader_ToggleIt()
    {
        await OpenAsync(Route);

        await Header("acl-basic", 2).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await ExpectExpandedAsync("acl-basic", 2, true);
        await Page.Keyboard.PressAsync("Space");
        await ExpectExpandedAsync("acl-basic", 2, false);
    }

    // ---------- multiple ----------

    [TestMethod]
    public async Task Multiple_SeveralPanels_StayOpenTogether()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-multiple")).ToHaveClassAsync(HasClass("bit-acl-mlt"));
        await Expect(Page.Locator("#acl-basic")).Not.ToHaveClassAsync(HasClass("bit-acl-mlt"));
        for (var i = 0; i < 3; i++) await Header("acl-multiple", i).ClickAsync();
        await ExpectExpandedSetAsync("acl-multiple", 0, 1, 2);

        await Header("acl-multiple", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-multiple", 0, 2);
    }

    [TestMethod]
    public async Task MaxExpanded_OpeningOneMore_ClosesThePanelOpenTheLongest()
    {
        await OpenAsync(Route);

        await Header("acl-max", 1).ClickAsync();
        await Header("acl-max", 0).ClickAsync();
        await ExpectExpandedSetAsync("acl-max", 0, 1);

        await Header("acl-max", 2).ClickAsync();
        await ExpectExpandedSetAsync("acl-max", 0, 2);
        await Expect(Log).ToHaveTextAsync("max:collapse:Users");
    }

    [TestMethod]
    public async Task MaxExpanded_OutsideMultiple_MeansNothing()
    {
        await OpenAsync(Route);

        await Header("acl-max-single", 0).ClickAsync();
        await Header("acl-max-single", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-max-single", 1);
    }

    // ---------- defaults & collapsible ----------

    [TestMethod]
    public async Task Defaults_DefaultExpandedKeyKeysAndAnItemsIsExpanded_StartOpen()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-default-key", 1);
        await ExpectExpandedSetAsync("acl-default-keys", 0, 2);
        await ExpectExpandedSetAsync("acl-item-expanded", 1);

        // The cap applies to the default keys too: the ones beyond it are dropped.
        await ExpectExpandedSetAsync("acl-default-max", 0);
    }

    [TestMethod]
    public async Task Collapsible_False_KeepsOnePanelOpenAndItsHeaderReportsItself()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-not-collapsible", 0);
        await Expect(Header("acl-not-collapsible", 0)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("acl-not-collapsible", 1)).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));

        await Header("acl-not-collapsible", 0).ClickAsync(new() { Force = true });
        await ExpectExpandedSetAsync("acl-not-collapsible", 0);

        await Header("acl-not-collapsible", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-not-collapsible", 1);
        await Expect(Header("acl-not-collapsible", 1)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Header("acl-not-collapsible", 0)).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
    }

    // ---------- icons ----------

    [TestMethod]
    public async Task Icons_PerItem_LeadingIconExpanderIconSwapAndHide()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-icon-items", 0).Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bit-icon--Settings"));
        await Expect(Item("acl-icon-items", 1).Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bit-icon--Contact"));
        await Expect(Item("acl-icon-items", 0).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--ChevronDownSmall"));
        await Expect(Item("acl-icon-items", 2).Locator(".bit-acd-eiw")).ToHaveCountAsync(0);

        var swap = Item("acl-icon-items", 1).Locator("i.bit-acd-eic");
        await Expect(swap).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Header("acl-icon-items", 1).ClickAsync();
        await Expect(swap).ToHaveClassAsync(HasClass("bit-icon--Remove"));
        await Expect(Item("acl-icon-items", 1).Locator(".bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));

        // The leading icon sits before the title.
        var icon = await Item("acl-icon-items", 0).Locator("i.bit-acd-ico").BoundingBoxAsync();
        var title = await Item("acl-icon-items", 0).Locator(".bit-acd-ttl").BoundingBoxAsync();
        Assert.IsTrue(icon!.X < title!.X);
    }

    [TestMethod]
    public async Task Icons_TheListsExpanderIcons_AreSwappedWhileOpenWithoutTurning()
    {
        await OpenAsync(Route);

        var icon = Item("acl-icon-swap", 0).Locator("i.bit-acd-eic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Header("acl-icon-swap", 0).ClickAsync();
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Remove"));
        await Expect(Item("acl-icon-swap", 0).Locator(".bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(Item("acl-icon-swap", 1).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
    }

    [TestMethod]
    public async Task Icons_TheDefaultChevron_TurnsOverUnlessNoExpanderRotation()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-basic", 0).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--ChevronRight"));
        await Header("acl-basic", 0).ClickAsync();
        var wrapper = Item("acl-basic", 0).Locator(".bit-acd-eiw");
        await Expect(wrapper).ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(wrapper).Not.ToHaveCSSAsync("transform", "none");

        await Header("acl-icon-norotate", 0).ClickAsync();
        await ExpectExpandedAsync("acl-icon-norotate", 0, true);
        await Expect(Item("acl-icon-norotate", 0).Locator(".bit-acd-eiw")).Not.ToHaveClassAsync(HasClass("bit-ico--r180"));
        await Expect(Item("acl-icon-norotate", 0).Locator(".bit-acd-eiw")).ToHaveCSSAsync("transform", "none");
    }

    [TestMethod]
    public async Task Icons_ExpanderIconPlacementStart_PutsTheExpanderBeforeTheTitle()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-icon-start", 0)).ToHaveClassAsync(HasClass("bit-acd-sei"));
        var expander = await Item("acl-icon-start", 0).Locator(".bit-acd-eiw").BoundingBoxAsync();
        var title = await Item("acl-icon-start", 0).Locator(".bit-acd-ttl").BoundingBoxAsync();
        Assert.IsTrue(expander!.X < title!.X, "The expander leads.");

        var endExpander = await Item("acl-basic", 0).Locator(".bit-acd-eiw").BoundingBoxAsync();
        var endTitle = await Item("acl-basic", 0).Locator(".bit-acd-ttl").BoundingBoxAsync();
        Assert.IsTrue(endExpander!.X > endTitle!.X, "By default it ends the header.");
    }

    [TestMethod]
    public async Task Icons_HideExpanderIcon_RemovesItFromEveryHeader()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-icon-hide .bit-acd-eiw")).ToHaveCountAsync(0);
        await Header("acl-icon-hide", 0).ClickAsync();
        await ExpectExpandedAsync("acl-icon-hide", 0, true);
    }

    [TestMethod]
    public async Task Icons_ExternalIcons_WinOverTheNamesAndAnItemsOwnWinsOverTheLists()
    {
        await OpenAsync(Route);

        var first = Item("acl-icon-external", 0).Locator("i.bit-acd-eic");
        await Expect(first).ToHaveClassAsync(HasClass("fa-angle-down"));
        await Expect(first).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(first).Not.ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Header("acl-icon-external", 0).ClickAsync();
        await Expect(first).ToHaveClassAsync(HasClass("bi-chevron-up"));

        await Expect(Item("acl-icon-external", 0).Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("fa-gear"));
        await Expect(Item("acl-icon-external", 1).Locator("i.bit-acd-ico")).ToHaveClassAsync(HasClass("bi-person"));
        await Expect(Item("acl-icon-external", 1).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bi-plus"));
    }

    // ---------- actions & templates ----------

    [TestMethod]
    public async Task Actions_TheTemplate_RendersBesideTheHeaderAndDoesNotToggleIt()
    {
        await OpenAsync(Route);

        var actions = Item("acl-actions", 0).Locator(".bit-acd-act");
        await Expect(actions.Locator(".acl-action")).ToHaveTextAsync("More about General settings");
        await Expect(Item("acl-actions", 0).Locator(".bit-acd-hdr .acl-action")).ToHaveCountAsync(0);
        await Expect(Item("acl-actions", 0).Locator("[role=heading] .acl-action")).ToHaveCountAsync(0);

        await actions.Locator(".acl-action").ClickAsync();
        await Expect(Page.Locator("#acl-actioned")).ToHaveTextAsync("General settings");
        await ExpectExpandedAsync("acl-actions", 0, false);

        // An item's own actions win over the template.
        await Expect(Item("acl-actions", 1).Locator(".acl-action")).ToHaveCountAsync(0);
        await Item("acl-actions", 1).Locator(".acl-own-action").ClickAsync();
        await Expect(Page.Locator("#acl-actioned")).ToHaveTextAsync("own");
        await ExpectExpandedAsync("acl-actions", 1, false);
    }

    [TestMethod]
    public async Task Templates_HeaderAndBody_ReplaceTheHeaderAndThePanelAndAnItemsOwnWin()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-header-body", 0).Locator(".bit-acd-hdr .acl-custom-header")).ToHaveTextAsync("General settings (custom)");
        await Expect(Item("acl-header-body", 0).Locator(".bit-acd-ttl")).ToHaveCountAsync(0);
        await Expect(Item("acl-header-body", 0).Locator(".bit-acd-eiw")).ToHaveCountAsync(0);
        await Header("acl-header-body", 0).ClickAsync();
        await Expect(Panel("acl-header-body", 0).Locator(".acl-custom-body")).ToHaveTextAsync("Body of General settings");

        await Expect(Item("acl-header-body", 1).Locator(".acl-own-header")).ToHaveTextAsync("Own header");
        await Expect(Item("acl-header-body", 1).Locator(".acl-custom-header")).ToHaveCountAsync(0);
        await Header("acl-header-body", 1).ClickAsync();
        await Expect(Panel("acl-header-body", 1).Locator(".acl-own-body")).ToHaveTextAsync("Own body wins");
        await Expect(Panel("acl-header-body", 1).Locator(".acl-custom-body")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Templates_TitleAndExpander_ReplaceOnlyTheirPart()
    {
        await OpenAsync(Route);

        var item = Item("acl-title-expander", 0);
        await Expect(item.Locator(".bit-acd-ttl .acl-custom-title")).ToHaveTextAsync("[General settings]");
        await Expect(item.Locator(".bit-acd-des")).ToHaveTextAsync("The general settings");
        await Expect(item.Locator(".bit-acd-eiw .acl-custom-expander")).ToHaveTextAsync("v");
        await Expect(item.Locator("i.bit-acd-eic")).ToHaveCountAsync(0);
    }

    // ---------- disabled & read-only ----------

    [TestMethod]
    public async Task States_ADisabledItem_IsOutOfTheTabOrderAndAnswersNothing()
    {
        await OpenAsync(Route);

        var header = Header("acl-states", 1);
        await Expect(header).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(header).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Item("acl-states", 1)).ToHaveClassAsync(HasClass("bit-dis"));

        await header.ClickAsync(new() { Force = true });
        await header.DispatchEventAsync("click");
        await ExpectExpandedAsync("acl-states", 1, false);
        await Expect(Log).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task States_AReadOnlyItem_KeepsItsPlaceAndColorsAndStillReportsTheClick()
    {
        await OpenAsync(Route);

        var header = Header("acl-states", 2);
        await ExpectExpandedAsync("acl-states", 2, true);
        await Expect(header).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(header).ToHaveAttributeAsync("tabindex", "0");
        await Expect(Item("acl-states", 2)).ToHaveClassAsync(HasClass("bit-acd-rdo"));
        await Expect(Item("acl-states", 2)).Not.ToHaveClassAsync(HasClass("bit-dis"));
        Assert.AreEqual(await ComputedStyleAsync("#acl-states > .bit-acl-itm:nth-child(1) .bit-acd-ttl", "color"),
                        await ComputedStyleAsync("#acl-states > .bit-acl-itm:nth-child(3) .bit-acd-ttl", "color"));

        await header.ClickAsync(new() { Force = true });
        await Expect(Log).ToHaveTextAsync("states:click:locked");
        await ExpectExpandedAsync("acl-states", 2, true);

        // The live item next to them works as usual.
        await Header("acl-states", 0).ClickAsync();
        await ExpectExpandedSetAsync("acl-states", 0, 2);
    }

    [TestMethod]
    public async Task Disabled_TheList_TurnsEveryHeaderOffUntilEnabled()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#acl-disabled");
        await Expect(list).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 3; i++)
        {
            await Expect(Header("acl-disabled", i)).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(Header("acl-disabled", i)).ToHaveAttributeAsync("tabindex", "-1");
        }

        await Header("acl-disabled", 0).DispatchEventAsync("click");
        await ExpectExpandedAsync("acl-disabled", 0, false);
        await Expect(Log).ToHaveTextAsync("");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(list).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Header("acl-disabled", 0)).ToHaveAttributeAsync("tabindex", "0");
        await Header("acl-disabled", 0).ClickAsync();
        await ExpectExpandedAsync("acl-disabled", 0, true);
        await Expect(Log).ToHaveTextAsync("disabled:click:General settings");
    }

    [TestMethod]
    public async Task ReadOnly_TheList_LeavesEveryPanelWhereItIs()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-readonly", 0);
        await Header("acl-readonly", 1).ClickAsync(new() { Force = true });
        await Header("acl-readonly", 0).ClickAsync(new() { Force = true });
        await Expect(Log).ToHaveTextAsync("readonly:click:users,readonly:click:general");
        await ExpectExpandedSetAsync("acl-readonly", 0);
        for (var i = 0; i < 3; i++)
        {
            await Expect(Header("acl-readonly", i)).ToHaveAttributeAsync("aria-disabled", "true");
            await Expect(Header("acl-readonly", i)).ToHaveAttributeAsync("tabindex", "0");
        }
    }

    // ---------- events ----------

    [TestMethod]
    public async Task Events_AClick_RaisesTheClickThenTheExpandAndTheCollapseOfTheOneItClosed()
    {
        await OpenAsync(Route);

        await Header("acl-events", 0).ClickAsync();
        await Expect(Log).ToHaveTextAsync("ev:click:One,ev:item-click:One,ev:expand:One,ev:toggle:One");

        await Page.Locator("#btn-clear-log").ClickAsync();
        await Header("acl-events", 1).ClickAsync();
        await Expect(Log).ToHaveTextAsync("ev:click:Two,ev:item-click:Two,ev:expand:Two,ev:toggle:Two,ev:collapse:One,ev:toggle:One");

        await Page.Locator("#btn-clear-log").ClickAsync();
        await Header("acl-events", 1).ClickAsync();
        await Expect(Log).ToHaveTextAsync("ev:click:Two,ev:item-click:Two,ev:collapse:Two,ev:toggle:Two");
    }

    // ---------- OnToggling ----------

    [TestMethod]
    public async Task OnToggling_ReportsTheRequestWithItsReason()
    {
        await OpenAsync(Route);

        await Header("acl-toggling", 2).ClickAsync();
        await Expect(Page.Locator("#acl-toggling-report")).ToHaveTextAsync("advanced:expanding:Click");
        await ExpectExpandedAsync("acl-toggling", 2, true);

        // The implicit collapse of single-expand mode is part of the expansion and is not offered.
        await Header("acl-toggling", 0).ClickAsync();
        await Expect(Log).ToHaveTextAsync(new Regex("^tg:toggling:advanced:True:Click,tg:toggle:advanced,tg:toggling:general:True:Click,tg:toggle:general,tg:toggle:advanced$"));

        await Header("acl-toggling", 0).ClickAsync();
        await Expect(Page.Locator("#acl-toggling-report")).ToHaveTextAsync("general:collapsing:Click");

        await Page.Locator("#btn-toggling-method").ClickAsync();
        await Expect(Page.Locator("#acl-toggling-report")).ToHaveTextAsync("advanced:expanding:Method");
        await ExpectExpandedAsync("acl-toggling", 2, true);
    }

    [TestMethod]
    public async Task OnToggling_Cancel_LeavesTheItemWhereItIs()
    {
        await OpenAsync(Route);

        await Page.Locator("#chk-lock").CheckAsync();
        await Header("acl-toggling", 1).ClickAsync();
        await Expect(Page.Locator("#acl-toggling-report")).ToHaveTextAsync("users:expanding:Click");
        await ExpectExpandedSetAsync("acl-toggling");
        await Expect(Log).ToHaveTextAsync("tg:toggling:users:True:Click");
    }

    [TestMethod]
    public async Task OnToggling_WhileAwaited_TheHeaderIsBusyWithASpinner()
    {
        await OpenAsync(Route);

        await Page.Locator("#chk-slow").CheckAsync();
        await Header("acl-toggling", 1).ClickAsync();

        await Expect(Header("acl-toggling", 1)).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(Header("acl-toggling", 1)).ToHaveClassAsync(HasClass("bit-acd-bsy"));
        await Expect(Item("acl-toggling", 1).Locator(".bit-acd-spn")).ToBeVisibleAsync();
        await Expect(Item("acl-toggling", 1).Locator("i.bit-acd-eic")).ToHaveCountAsync(0);
        await ExpectExpandedAsync("acl-toggling", 1, false);

        await Page.Locator("#btn-release-toggling").ClickAsync();
        await ExpectExpandedAsync("acl-toggling", 1, true);
        await Expect(Header("acl-toggling", 1)).Not.ToHaveAttributeAsync("aria-busy", new Regex(".*"));
        await Expect(Item("acl-toggling", 1).Locator(".bit-acd-spn")).ToHaveCountAsync(0);
    }

    // ---------- binding & methods ----------

    [TestMethod]
    public async Task BindExpandedKey_FollowsThePageAndReportsTheUser()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-bind-key", 1);
        await Page.Locator("#btn-bind-general").ClickAsync();
        await ExpectExpandedSetAsync("acl-bind-key", 0);

        await Header("acl-bind-key", 2).ClickAsync();
        await Expect(Page.Locator("#acl-bound-key")).ToHaveTextAsync("advanced");
        await ExpectExpandedSetAsync("acl-bind-key", 2);

        await Header("acl-bind-key", 2).ClickAsync();
        await Expect(Page.Locator("#acl-bound-key")).ToHaveTextAsync("null");

        await Page.Locator("#btn-bind-advanced").ClickAsync();
        await ExpectExpandedSetAsync("acl-bind-key", 2);
        await Page.Locator("#btn-bind-null").ClickAsync();
        await ExpectExpandedSetAsync("acl-bind-key");
    }

    [TestMethod]
    public async Task Methods_ExpandAllCollapseAllToggleExpandCollapse_DriveTheListAndTheBoundKeys()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-expand-all").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods", 0, 1, 3);
        await Expect(Page.Locator("#acl-bound-keys")).ToHaveTextAsync("general|users|advanced");

        await Page.Locator("#btn-collapse-all").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods");
        await Expect(Page.Locator("#acl-bound-keys")).ToHaveTextAsync("");

        await Page.Locator("#btn-toggle-users").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods", 1);
        await Page.Locator("#btn-expand-general").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods", 0, 1);
        await Expect(Page.Locator("#acl-bound-keys")).ToHaveTextAsync("general|users");

        await Page.Locator("#btn-read-state").ClickAsync();
        await Expect(Page.Locator("#acl-methods-state")).ToHaveTextAsync("general|users;users=True;none=False");

        await Page.Locator("#btn-collapse-general").ClickAsync();
        await Page.Locator("#btn-toggle-users").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods");

        // A method is the app's own way in, which a disabled item does not turn away; CollapseAll closes it too.
        await Page.Locator("#btn-expand-disabled").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods", 2);
        await Page.Locator("#btn-collapse-all").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods");

        // The bound keys set by the page.
        await Page.Locator("#btn-set-keys").ClickAsync();
        await ExpectExpandedSetAsync("acl-methods", 3);
        await Header("acl-methods", 0).ClickAsync();
        await Expect(Page.Locator("#acl-bound-keys")).ToHaveTextAsync("general|advanced");
    }

    [TestMethod]
    public async Task Methods_FocusItemAndFocusAsync_MoveTheFocusToAHeader()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-focus-advanced").ClickAsync();
        await Expect(Header("acl-methods", 3)).ToBeFocusedAsync();

        await Page.Locator("#btn-focus-first").ClickAsync();
        await Expect(Header("acl-methods", 0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Methods_ExpandAllOutsideMultiple_DoesNothing()
    {
        await OpenAsync(Route);

        await Page.Locator("#btn-expand-all-single").ClickAsync();
        await ExpectExpandedSetAsync("acl-single-methods");
    }

    // ---------- content rendering ----------

    [TestMethod]
    public async Task Content_PanelsRenderUpFrontByDefault()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-eager .eager-body")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#acl-eager .eager-body")).ToBeHiddenAsync();
    }

    [TestMethod]
    public async Task LazyContent_APanel_RendersOnItsFirstOpenAndStays()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-lazy .lazy-body")).ToHaveCountAsync(0);
        await Header("acl-lazy", 0).ClickAsync();
        await Expect(Page.Locator("#acl-lazy .lazy-body")).ToBeVisibleAsync();
        await Header("acl-lazy", 0).ClickAsync();
        await ExpectExpandedAsync("acl-lazy", 0, false);
        await Expect(Page.Locator("#acl-lazy .lazy-body")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task UnmountOnCollapse_APanel_LosesItsContentOnEveryClose()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-unmount .unmount-input")).ToHaveCountAsync(0);
        await Header("acl-unmount", 0).ClickAsync();
        await Page.Locator("#acl-unmount .unmount-input").FillAsync("typed");
        await Header("acl-unmount", 0).ClickAsync();
        await Expect(Page.Locator("#acl-unmount .unmount-input")).ToHaveCountAsync(0);

        await Header("acl-unmount", 0).ClickAsync();
        await Expect(Page.Locator("#acl-unmount .unmount-input")).ToHaveValueAsync("");
    }

    // ---------- MaxHeight & transition ----------

    [TestMethod]
    public async Task MaxHeight_APanel_ScrollsInsideTheItemAndTakesATabStop()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-maxheight", 0)).ToHaveClassAsync(HasClass("bit-acd-mxh"));
        var panel = Panel("acl-maxheight", 0);
        await Expect(panel).ToHaveCSSAsync("max-height", "100px");
        await Expect(panel).ToHaveCSSAsync("overflow-y", "auto");
        await Expect(panel).ToHaveAttributeAsync("tabindex", "0");
        Assert.IsTrue(await panel.EvaluateAsync<bool>("e => e.scrollHeight > e.clientHeight"));
        Assert.IsTrue(await panel.EvaluateAsync<bool>("e => { const s = getComputedStyle(e); return e.getBoundingClientRect().height <= 100.5 + parseFloat(s.paddingTop) + parseFloat(s.paddingBottom); }"));

        // A collapsed one takes no tab stop.
        await Expect(Panel("acl-maxheight", 1)).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));

        // The keyboard scrolls the focused panel rather than moving to another header.
        await panel.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.WaitForFunctionAsync("e => e.scrollTop > 0", await panel.ElementHandleAsync());
        await Expect(panel).ToBeFocusedAsync();
    }

    [DataTestMethod]
    [DataRow("acl-dur-0", "0s")]
    [DataRow("acl-dur-1500", "1.5s")]
    public async Task TransitionDuration_SetsTheExpandAnimation(string list, string duration)
    {
        await OpenAsync(Route);

        var container = Item(list, 0).Locator(".bit-acd-cnt");
        await Expect(container).ToHaveCSSAsync("transition-duration", duration);
        await Expect(container).ToHaveCSSAsync("transition-property", "grid-template-rows");
    }

    [TestMethod]
    public async Task TransitionDuration_TheExpansion_RunsAsATransitionOfTheGridRow()
    {
        await OpenAsync(Route);

        var container = Item("acl-dur-1500", 0).Locator(".bit-acd-cnt");
        await Header("acl-dur-1500", 0).ClickAsync();

        // The row grows over the duration rather than all at once.
        await Page.WaitForFunctionAsync(
            """
            e => e.getAnimations().some(a => a.transitionProperty === 'grid-template-rows' && a.playState === 'running'
                                         && a.effect.getComputedTiming().duration === 1500)
            """, await container.ElementHandleAsync());
        await ExpectExpandedAsync("acl-dur-1500", 0, true);
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionUnlessForceAnimation()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        var reduced = ParseSeconds(await ComputedStyleAsync("#acl-dur-1500 > .bit-acl-itm > .bit-acd .bit-acd-cnt", "transition-duration"));
        Assert.IsTrue(reduced < 0.01, $"Reduced motion collapses the duration, was {reduced}s.");
        await Expect(Item("acl-dur-forced", 0).Locator(".bit-acd-cnt")).ToHaveCSSAsync("transition-duration", "0.4s");
    }

    // ---------- find & print ----------

    [TestMethod]
    public async Task HiddenUntilFound_ACollapsedPanel_IsHiddenUntilFoundAndADisabledOneIsNot()
    {
        await OpenAsync(Route);

        await Expect(Panel("acl-find", 0)).ToHaveAttributeAsync("hidden", "until-found");
        await Expect(Panel("acl-find", 1)).ToHaveAttributeAsync("hidden", "until-found");
        await Expect(Panel("acl-find", 2)).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));

        // The panels have to be in the DOM to be found, so LazyContent and UnmountOnCollapse are ignored.
        await Expect(Page.Locator("#acl-find-lazy .huf-lazy-body")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task HiddenUntilFound_AFragmentNavigationIntoAPanel_ExpandsItThroughTheList()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not implement hidden=until-found.");

        await OpenAsync(Route);

        await Page.EvaluateAsync("() => { location.hash = 'faq-refund-target'; }");

        await ExpectExpandedAsync("acl-find", 1, true);
        await Expect(Log).ToHaveTextAsync("find:Reveal:refund:True");
        await Expect(Page.Locator("#faq-refund-target")).ToBeInViewportAsync();
        await Expect(Panel("acl-find", 1)).Not.ToHaveAttributeAsync("hidden", new Regex(".*"));

        // Single-expand: a reveal of another panel closes this one.
        await Page.EvaluateAsync("() => { location.hash = 'faq-reset-target'; }");
        await ExpectExpandedAsync("acl-find", 0, true);
        await ExpectExpandedAsync("acl-find", 1, false);
    }

    [TestMethod]
    public async Task ExpandOnPrint_ThePrintMedia_OpensEveryPanel()
    {
        await OpenAsync(Route);

        await Expect(Panel("acl-print", 0)).ToBeHiddenAsync();
        await Expect(Item("acl-print", 0)).ToHaveClassAsync(HasClass("bit-acd-eop"));
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        for (var i = 0; i < 3; i++)
        {
            await Expect(Panel("acl-print", i)).ToBeVisibleAsync();
        }

        // A list without it prints its collapsed panels as bare headers.
        await Expect(Panel("acl-basic", 0)).ToBeHiddenAsync();
    }

    // ---------- scroll into view ----------

    [TestMethod]
    public async Task ScrollIntoViewOnExpand_APanelOpenedAtTheBottomWithTheTransition_IsScrolledIntoView()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#acl-scroll-box");
        await box.ScrollIntoViewIfNeededAsync();
        await Header("acl-scroll", 4).ScrollIntoViewIfNeededAsync();
        var before = await box.EvaluateAsync<double>("e => e.scrollTop");

        await Header("acl-scroll", 4).ClickAsync();
        await ExpectExpandedAsync("acl-scroll", 4, true);
        await Page.WaitForFunctionAsync("([e, before]) => e.scrollTop > before + 20", new object[] { await box.ElementHandleAsync(), before });
    }

    [TestMethod]
    public async Task ScrollIntoViewOnExpand_APanelOpenedAtTheBottom_IsScrolledIntoView()
    {
        await OpenAsync(Route);

        var box = Page.Locator("#acl-scroll-instant-box");
        await box.ScrollIntoViewIfNeededAsync();
        await Header("acl-scroll-instant", 4).ScrollIntoViewIfNeededAsync();
        var before = await box.EvaluateAsync<double>("e => e.scrollTop");

        await Header("acl-scroll-instant", 4).ClickAsync();
        await ExpectExpandedAsync("acl-scroll-instant", 4, true);
        await Page.WaitForFunctionAsync("([e, before]) => e.scrollTop > before + 20", new object[] { await box.ElementHandleAsync(), before });

        // Without it the box stays where it was.
        var other = Page.Locator("#acl-noscroll-box");
        await Header("acl-noscroll", 4).ScrollIntoViewIfNeededAsync();
        var otherBefore = await other.EvaluateAsync<double>("e => e.scrollTop");
        await Header("acl-noscroll", 4).ClickAsync();
        await ExpectExpandedAsync("acl-noscroll", 4, true);
        await NextFramesAsync();
        await NextFramesAsync();
        Assert.AreEqual(otherBefore, await other.EvaluateAsync<double>("e => e.scrollTop"), 1);
    }

    // ---------- joined, gap, border ----------

    [TestMethod]
    public async Task Joined_TheItems_FormOneSurfaceWithOneLineBetweenThem()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#acl-joined");
        await Expect(list).ToHaveClassAsync(HasClass("bit-acl-jnd"));
        await Expect(list).ToHaveCSSAsync("row-gap", "0px");

        var first = Item("acl-joined", 0);
        var middle = Item("acl-joined", 1);
        var last = Item("acl-joined", 2);
        Assert.AreNotEqual("0px", await first.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius"));
        await Expect(first).ToHaveCSSAsync("border-bottom-left-radius", "0px");
        await Expect(first).ToHaveCSSAsync("border-bottom-width", "0px");
        await Expect(middle).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(middle).ToHaveCSSAsync("border-bottom-left-radius", "0px");
        await Expect(last).ToHaveCSSAsync("border-top-left-radius", "0px");
        Assert.AreNotEqual("0px", await last.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomLeftRadius"));
        Assert.AreNotEqual("0px", await last.EvaluateAsync<string>("e => getComputedStyle(e).borderBottomWidth"));

        // The items touch.
        var a = await first.BoundingBoxAsync();
        var b = await middle.BoundingBoxAsync();
        Assert.AreEqual(a!.Y + a.Height, b!.Y, 0.6);
    }

    [TestMethod]
    public async Task Joined_NoBorder_DrawsADividerBetweenTheItems()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-joined-noborder", 0)).ToHaveClassAsync(HasClass("bit-acd-nbd"));
        await Expect(Item("acl-joined-noborder", 0)).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreNotEqual("0px", await Item("acl-joined-noborder", 1).EvaluateAsync<string>("e => getComputedStyle(e).borderTopWidth"));
        await Expect(Item("acl-joined-noborder", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-brd-pri)"));
    }

    [TestMethod]
    public async Task Gap_SpacesTheItemsAndTheDefaultGapIsSmall()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-gap")).ToHaveCSSAsync("row-gap", "16px");
        var a = await Item("acl-gap", 0).BoundingBoxAsync();
        var b = await Item("acl-gap", 1).BoundingBoxAsync();
        Assert.AreEqual(16, b!.Y - (a!.Y + a.Height), 0.6);
        await Expect(Item("acl-gap", 0)).ToHaveClassAsync(HasClass("bit-acd-nbd"));

        var defaultGap = ParsePx(await ComputedStyleAsync("#acl-default-gap", "row-gap"));
        Assert.IsTrue(defaultGap > 0 && defaultGap < 16, $"The default gap is a small step, was {defaultGap}.");
    }

    // ---------- empty ----------

    [TestMethod]
    public async Task EmptyContent_ShowsWhileThereAreNoItems()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-empty-content")).ToHaveTextAsync("There is nothing to show here yet.");
        await Expect(Items("acl-empty")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-empty").ClickAsync();
        await Expect(Items("acl-empty")).ToHaveCountAsync(3);
        await Expect(Page.Locator("#acl-empty-content")).ToHaveCountAsync(0);

        await Page.Locator("#btn-toggle-empty").ClickAsync();
        await Expect(Page.Locator("#acl-empty-content")).ToBeVisibleAsync();
    }

    // ---------- accessibility & keyboard ----------

    [TestMethod]
    public async Task Accessibility_HeadingLevelRegionAndTheListsName()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#acl-a11y");
        await Expect(list).ToHaveAttributeAsync("role", "group");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Application settings");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Application settings" })).ToHaveCountAsync(1);
        await Expect(Item("acl-a11y", 0).Locator("[role=heading]")).ToHaveAttributeAsync("aria-level", "2");
        await Expect(Panel("acl-a11y", 0)).Not.ToHaveAttributeAsync("role", new Regex(".*"));
        await Expect(Panel("acl-a11y", 0)).Not.ToHaveAttributeAsync("aria-labelledby", new Regex(".*"));

        await Expect(Item("acl-heading-clamp", 0).Locator("[role=heading]")).ToHaveAttributeAsync("aria-level", "6");
        await Expect(Page.Locator("#acl-basic")).Not.ToHaveAttributeAsync("role", new Regex(".*"));

        await Expect(Page.Locator("#acl-labelledby")).ToHaveAttributeAsync("role", "group");
        await Expect(Page.GetByRole(AriaRole.Group, new() { Name = "Labelled from outside" })).ToHaveCountAsync(1);
        await Expect(Header("acl-labelledby", 1)).ToHaveAttributeAsync("aria-label", "Settings by icon");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Settings by icon" })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task Keyboard_TheArrowsHomeAndEnd_MoveBetweenTheHeadersSkippingADisabledOne()
    {
        await OpenAsync(Route);

        await Header("acl-nav", 0).FocusAsync();
        var scrollY = await Page.EvaluateAsync<double>("() => scrollY");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("acl-nav", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("acl-nav", 3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("acl-nav", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Header("acl-nav", 3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Header("acl-nav", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(Header("acl-nav", 3)).ToBeFocusedAsync();

        // The keys move the focus, not the page.
        Assert.AreEqual(scrollY, await Page.EvaluateAsync<double>("() => scrollY"), 1);

        // A modified key belongs to the page.
        await Page.Keyboard.PressAsync("Control+Home");
        await Expect(Header("acl-nav", 3)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_TheKeysInsideAPanel_BelongToItsContent()
    {
        await OpenAsync(Route);

        await Header("acl-nav", 0).ClickAsync();
        await ExpectExpandedAsync("acl-nav", 0, true);
        var input = Page.Locator("#acl-nav .nav-input");
        await input.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("End");
        await Expect(input).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_NoNavigationLoop_StopsAtTheEnds()
    {
        await OpenAsync(Route);

        await Header("acl-noloop", 2).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("acl-noloop", 2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(Header("acl-noloop", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(Header("acl-noloop", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(Header("acl-noloop", 1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Keyboard_NavigableFalse_LeavesTheArrowsAlone()
    {
        await OpenAsync(Route);

        await Header("acl-nonav", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Keyboard.PressAsync("End");
        await Expect(Header("acl-nonav", 0)).ToBeFocusedAsync();

        // Tab still walks the headers, past a collapsed panel.
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Header("acl-nonav", 1)).ToBeFocusedAsync();
    }

    // ---------- cascading parameters ----------

    [TestMethod]
    public async Task Cascading_BitParams_SetTheDefaultsAndTheListsOwnWin()
    {
        await OpenAsync(Route);

        var list = Page.Locator("#acl-cas");
        await Expect(list).ToHaveClassAsync(HasClass("bit-acl-jnd"));
        await Expect(list).ToHaveClassAsync(HasClass("bit-acl-mlt"));
        await Expect(Item("acl-cas", 0).Locator("[role=heading]")).ToHaveAttributeAsync("aria-level", "4");
        await Expect(Item("acl-cas", 0).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Header("acl-cas", 0).ClickAsync();
        await Header("acl-cas", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-cas", 0, 1);
        await Expect(Item("acl-cas", 0).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Remove"));

        var own = Page.Locator("#acl-cas-own");
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-acl-jnd"));
        await Expect(own).Not.ToHaveClassAsync(HasClass("bit-acl-mlt"));
        await Header("acl-cas-own", 0).ClickAsync();
        await Header("acl-cas-own", 1).ClickAsync();
        await ExpectExpandedSetAsync("acl-cas-own", 1);
        await Expect(Item("acl-cas-own", 0).Locator("i.bit-acd-eic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
    }

    // ---------- color & size ----------

    [DataTestMethod]
    [DataRow("primary", "pbg", "var(--bit-clr-bg-pri)")]
    [DataRow("secondary", "sbg", "var(--bit-clr-bg-sec)")]
    [DataRow("tertiary", "tbg", "var(--bit-clr-bg-ter)")]
    [DataRow("transparent", "rbg", "transparent")]
    public async Task Background_EachKind_FillsEveryItem(string kind, string suffix, string color)
    {
        await OpenAsync(Route);

        var expected = await ResolveColorAsync(color);
        for (var i = 0; i < 3; i++)
        {
            await Expect(Item($"acl-bg-{kind}", i)).ToHaveClassAsync(HasClass($"bit-acd-{suffix}"));
            await Expect(Item($"acl-bg-{kind}", i)).ToHaveCSSAsync("background-color", expected);
        }
    }

    [DataTestMethod]
    [DataRow("primary", "pbr", "var(--bit-clr-brd-pri)")]
    [DataRow("secondary", "sbr", "var(--bit-clr-brd-sec)")]
    [DataRow("tertiary", "tbr", "var(--bit-clr-brd-ter)")]
    [DataRow("transparent", "rbr", "transparent")]
    public async Task Border_EachKind_OutlinesEveryItem(string kind, string suffix, string color)
    {
        await OpenAsync(Route);

        var expected = await ResolveColorAsync(color);
        for (var i = 0; i < 3; i++)
        {
            await Expect(Item($"acl-brd-{kind}", i)).ToHaveClassAsync(HasClass($"bit-acd-{suffix}"));
            await Expect(Item($"acl-brd-{kind}", i)).ToHaveCSSAsync("border-top-color", expected);
        }
    }

    [TestMethod]
    public async Task Border_InAJoinedList_WinsOverTheDividerVariable()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-joined-border", 1)).ToHaveCSSAsync("border-top-color", await ResolveColorAsync("var(--bit-clr-brd-ter)"));

        // A flush list leaves only the lines between the items.
        await Expect(Item("acl-flush", 0)).ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
        Assert.AreNotEqual("0px", await Item("acl-flush", 1).EvaluateAsync<string>("e => getComputedStyle(e).borderTopWidth"));
    }

    [TestMethod]
    public async Task Size_EachSize_ScalesThePaddingAndTheType()
    {
        await OpenAsync(Route);

        await Expect(Item("acl-size-small", 0)).ToHaveClassAsync(HasClass("bit-acd-sm"));
        await Expect(Item("acl-size-medium", 0)).ToHaveClassAsync(HasClass("bit-acd-md"));
        await Expect(Item("acl-size-large", 0)).ToHaveClassAsync(HasClass("bit-acd-lg"));

        var small = (await Header("acl-size-small", 0).BoundingBoxAsync())!.Height;
        var medium = (await Header("acl-size-medium", 0).BoundingBoxAsync())!.Height;
        var large = (await Header("acl-size-large", 0).BoundingBoxAsync())!.Height;
        Assert.IsTrue(small < medium && medium < large, $"{small} < {medium} < {large}");

        var smallFont = ParsePx(await ComputedStyleAsync("#acl-size-small .bit-acd-ttl", "font-size"));
        var largeFont = ParsePx(await ComputedStyleAsync("#acl-size-large .bit-acd-ttl", "font-size"));
        Assert.IsTrue(smallFont < largeFont, $"{smallFont} < {largeFont}");
    }

    // ---------- style, class, variables ----------

    [TestMethod]
    public async Task StyleAndClass_TheRoot_TakesThem()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-style")).ToHaveCSSAsync("border-top-width", "2px");
        await Expect(Page.Locator("#acl-style")).ToHaveCSSAsync("padding-top", "6px");
        await Expect(Page.Locator("#acl-style")).ToHaveCSSAsync("row-gap", "8px");
        await Expect(Page.Locator("#acl-style")).ToHaveClassAsync(HasClass("acl-root-class"));
    }

    [TestMethod]
    public async Task Styles_EveryPartOfEveryItem_TakesItsStyleAndTheExpandedOnesOnlyWhileOpen()
    {
        await OpenAsync(Route);

        var open = Item("acl-styles", 0);
        var closed = Item("acl-styles", 1);
        await Expect(Page.Locator("#acl-styles")).ToHaveCSSAsync("padding-top", "3px");
        await Expect(closed).ToHaveCSSAsync("margin-top", "4px");
        await Expect(open).ToHaveCSSAsync("outline-color", "rgb(0, 0, 255)");
        await Expect(closed).Not.ToHaveCSSAsync("outline-color", "rgb(0, 0, 255)");
        await Expect(open.Locator(".bit-acd-hwr")).ToHaveCSSAsync("padding-right", "5px");
        await Expect(open.Locator(".bit-acd-hed")).ToHaveCSSAsync("padding-left", "6px");
        await Expect(open.Locator(".bit-acd-hdr")).ToHaveCSSAsync("background-color", "rgb(200, 200, 200)");
        await Expect(open.Locator(".bit-acd-ico")).ToHaveCSSAsync("color", "rgb(10, 20, 30)");
        await Expect(open.Locator(".bit-acd-wrp")).ToHaveCSSAsync("padding-left", "7px");
        await Expect(open.Locator(".bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(255, 99, 71)");
        await Expect(open.Locator(".bit-acd-des")).ToHaveCSSAsync("color", "rgb(40, 50, 60)");
        await Expect(open.Locator(".bit-acd-eiw")).ToHaveCSSAsync("padding-left", "8px");
        await Expect(open.Locator(".bit-acd-eic")).ToHaveCSSAsync("color", "rgb(70, 80, 90)");
        await Expect(open.Locator(".bit-acd-eic")).ToHaveCSSAsync("font-size", "25px");
        await Expect(closed.Locator(".bit-acd-eic")).Not.ToHaveCSSAsync("font-size", "25px");
        await Expect(open.Locator(".bit-acd-act")).ToHaveCSSAsync("padding-left", "9px");
        await Expect(open.Locator(".bit-acd-cnt")).ToHaveCSSAsync("border-top-color", "rgb(1, 1, 1)");
        await Expect(open.Locator(".bit-acd-cwr")).ToHaveCSSAsync("padding-left", "10px");
        await Expect(open.Locator(".bit-acd-con")).ToHaveCSSAsync("color", "rgb(11, 12, 13)");

        // An item's own Class and Style.
        await Expect(open).ToHaveClassAsync(HasClass("acl-item-class"));
        await Expect(open).ToHaveCSSAsync("margin-left", "12px");
    }

    [TestMethod]
    public async Task Classes_EveryPartOfEveryItem_TakesItsClassAndTheExpandedOnesOnlyWhileOpen()
    {
        await OpenAsync(Route);

        var open = Item("acl-classes", 0);
        var closed = Item("acl-classes", 1);
        await Expect(Page.Locator("#acl-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(open).ToHaveClassAsync(HasClass("c-item"));
        await Expect(open).ToHaveClassAsync(HasClass("c-expanded"));
        await Expect(closed).ToHaveClassAsync(HasClass("c-item"));
        await Expect(closed).Not.ToHaveClassAsync(HasClass("c-expanded"));
        foreach (var (part, cls) in new[]
                 {
                     (".bit-acd-hwr", "c-hwr"), (".bit-acd-hed", "c-hed"), (".bit-acd-hdr", "c-hdr"), (".bit-acd-ico", "c-ico"),
                     (".bit-acd-wrp", "c-hcn"), (".bit-acd-ttl", "c-ttl"), (".bit-acd-des", "c-des"), (".bit-acd-eiw", "c-eiw"),
                     (".bit-acd-eic", "c-eic"), (".bit-acd-act", "c-act"), (".bit-acd-cnt", "c-cnt"), (".bit-acd-cwr", "c-cwr"), (".bit-acd-con", "c-con")
                 })
        {
            await Expect(open.Locator(part)).ToHaveClassAsync(HasClass(cls));
        }

        await Expect(open.Locator(".bit-acd-eic")).ToHaveClassAsync(HasClass("c-exi"));
        await Expect(closed.Locator(".bit-acd-eic")).Not.ToHaveClassAsync(HasClass("c-exi"));

        await Header("acl-classes", 1).ClickAsync();
        await Expect(closed).ToHaveClassAsync(HasClass("c-expanded"));
        await Expect(open).Not.ToHaveClassAsync(HasClass("c-expanded"));
    }

    [TestMethod]
    public async Task Styles_TheSpinnerOfABusyHeader_TakesItsClassAndStyle()
    {
        await OpenAsync(Route);

        await Header("acl-spinner", 0).ClickAsync();
        var spinner = Item("acl-spinner", 0).Locator(".bit-acd-spn");
        await Expect(spinner).ToHaveClassAsync(HasClass("c-spn"));
        await Expect(spinner).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 255)");

        await Page.Locator("#btn-release-hold").ClickAsync();
        await ExpectExpandedAsync("acl-spinner", 0, true);
        await Expect(spinner).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task CssVariables_OnAnAncestor_RestyleTheListAndItsItems()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-vars")).ToHaveCSSAsync("row-gap", "22px");
        await Expect(Page.Locator("#acl-vars-gap")).ToHaveCSSAsync("row-gap", "3px");
        await Expect(Item("acl-vars", 0)).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(Item("acl-vars", 0)).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 200)");
        await Expect(Item("acl-vars", 0).Locator(".bit-acd-ttl")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");

        await Expect(Item("acl-divider", 1)).ToHaveCSSAsync("border-top-color", "rgb(250, 0, 0)");
        await Expect(Item("acl-divider", 2)).ToHaveCSSAsync("border-top-color", "rgb(250, 0, 0)");
        await Expect(Item("acl-divider", 0)).Not.ToHaveCSSAsync("border-top-color", "rgb(250, 0, 0)");
        await Expect(Item("acl-divider-noborder", 1)).ToHaveCSSAsync("border-top-color", "rgb(0, 250, 0)");
    }

    // ---------- RTL ----------

    [DataTestMethod]
    [DataRow("acl-rtl")]
    [DataRow("acl-rtl-inherited")]
    public async Task Rtl_TheItems_AreMirrored(string list)
    {
        await OpenAsync(Route);

        await Expect(Item(list, 0)).ToHaveCSSAsync("direction", "rtl");
        var icon = await Item(list, 0).Locator(".bit-acd-ico").BoundingBoxAsync();
        var title = await Item(list, 0).Locator(".bit-acd-ttl").BoundingBoxAsync();
        var expander = await Item(list, 0).Locator(".bit-acd-eiw").BoundingBoxAsync();
        Assert.IsTrue(icon!.X > title!.X, "The leading icon is on the right.");
        Assert.IsTrue(expander!.X < title.X, "The expander is on the left.");

        await Header(list, 0).ClickAsync();
        await ExpectExpandedAsync(list, 0, true);
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_ReachesTheItems()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#acl-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Page.Locator("#acl-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Item("acl-rtl", 0)).ToHaveAttributeAsync("dir", "rtl");
    }

    // ---------- lifecycle ----------

    [TestMethod]
    public async Task Prerender_ThePagesHtml_ContainsTheItemsBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "<div id=\"acl-basic\" class=\"bit-acl\">");
        StringAssert.Matches(html, new Regex(@"aria-expanded=""false"" aria-controls=""[^""]+-cnt"""));
        StringAssert.Matches(html, new Regex(@"<div id=""acl-default-key"".*?aria-expanded=""true""", RegexOptions.Singleline));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Items_ChangedAtRuntime_KeepTheStateOfTheItemsThatStay()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-dynamic", 0, 1);
        await Page.Locator("#btn-add-item").ClickAsync();
        await Expect(Items("acl-dynamic")).ToHaveCountAsync(4);
        await Expect(Item("acl-dynamic", 3).Locator(".bit-acd-ttl")).ToHaveTextAsync("New 1");
        await ExpectExpandedSetAsync("acl-dynamic", 0, 1);

        await Page.Locator("#btn-remove-item").ClickAsync();
        await Expect(Items("acl-dynamic")).ToHaveCountAsync(3);
        await Expect(Item("acl-dynamic", 0).Locator(".bit-acd-ttl")).ToHaveTextAsync("Dynamic two");
        await ExpectExpandedSetAsync("acl-dynamic", 0);
    }

    [TestMethod]
    public async Task Multiple_TurnedOffWithSeveralOpen_KeepsOnlyTheFirst()
    {
        await OpenAsync(Route);

        await ExpectExpandedSetAsync("acl-dynamic", 0, 1);
        await Page.Locator("#btn-toggle-multiple").ClickAsync();
        await ExpectExpandedSetAsync("acl-dynamic", 0);
        await Header("acl-dynamic", 2).ClickAsync();
        await ExpectExpandedSetAsync("acl-dynamic", 2);
    }

    [TestMethod]
    public async Task Unmount_TheList_LeavesNoErrorBehind()
    {
        await OpenAsync(Route);

        await ExpectExpandedAsync("acl-mounted", 0, true);
        await Header("acl-mounted", 1).ClickAsync();
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Page.Locator("#acl-mounted")).ToHaveCountAsync(0);
        await Page.Locator("#btn-unmount").ClickAsync();
        await ExpectExpandedAsync("acl-mounted", 0, true);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- helpers ----------

    private Task<string> ResolveColorAsync(string cssColor)
    {
        return Page.EvaluateAsync<string>(
            """
            value => {
                const probe = document.createElement('div');
                probe.style.color = value;
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).color;
                probe.remove();
                return resolved;
            }
            """, cssColor);
    }

    private static double ParsePx(string value) => double.Parse(value.Replace("px", "").Trim(), CultureInfo.InvariantCulture);

    private static double ParseSeconds(string value)
    {
        var first = value.Split(',')[0].Trim();
        return first.EndsWith("ms")
            ? double.Parse(first[..^2], CultureInfo.InvariantCulture) / 1000
            : double.Parse(first.TrimEnd('s'), CultureInfo.InvariantCulture);
    }
}
