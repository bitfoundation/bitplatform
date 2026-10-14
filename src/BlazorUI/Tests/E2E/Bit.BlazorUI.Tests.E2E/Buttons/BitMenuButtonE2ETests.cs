using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of BitMenuButton through its three APIs - Items (/e2e/menu-button), custom items mapped by
/// NameSelectors (/e2e/menu-button/custom) and BitMenuButtonOption children (/e2e/menu-button/options) - whose pages
/// render the item-level sections with the same ids and data, so every item-level test runs once per API: opening
/// and closing the callout (trigger, chevron, overlay, item click, bound IsOpen) and where it is placed, Split,
/// Sticky (SelectedItem two-way, DefaultSelectedItem, IsSelected, OnChange, the split header running the selected
/// item), the header and item icons, IconOnly, NoIcon, the split Toggle (IsToggled two-way, DefaultIsToggled,
/// OnToggleChange), checkable and radio items, HeaderTemplate, ItemTemplate and an item's own Template, headers and
/// separators, SecondaryText, Title, AriaLabel, disabled items and DisabledInteractive, links with Rel and the
/// new-tab hint (NewTabHint, NoNewTabHint), the loading states (AutoLoading, IsLoading two-way, LoadingLabel and its
/// live region, LoadingTemplate, Reclickable), the menu keyboard (arrows, Home, End, typeahead, Enter, Escape, Tab)
/// and the submenus by keyboard and by pointer, item Class and Style, items changed at runtime, and unmounting while
/// open. The group-level parameters - every Variant, Color and Size, FullWidth, the chevron, the mark icons,
/// Background, MaxHeight, CloseOnItemClick, ButtonType and FormId, the accessibility attributes, FocusAsync and
/// AutoFocus, the disabled contract, BitParams, Style/Class/Styles/Classes, the CSS variables and their variant
/// twins, RTL, Visibility, re-rendering, Escape inside a modal, DropDirection, reduced motion, forced colors and
/// prerendering - are the same for every API and run against the Items page.
/// </summary>
[TestClass]
public class BitMenuButtonE2ETests : E2ETestBase
{
    private const string ItemsApi = "/e2e/menu-button";
    private const string CustomApi = "/e2e/menu-button/custom";
    private const string OptionsApi = "/e2e/menu-button/options";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Opb(string id) => Page.Locator($"#{id}-opb");

    private ILocator Chb(string id) => Page.Locator($"#{id}-chb");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task<string> Token(string token) => Page.ResolveColorAsync($"var(--bit-clr-{token})");

    // The menu the trigger of a menu button controls, wherever it is rendered.
    private async Task<ILocator> MenuOf(string id)
    {
        var opener = await Chb(id).CountAsync() > 0 ? Chb(id) : Opb(id);
        var menuId = await opener.GetAttributeAsync("aria-controls") ?? throw new AssertFailedException($"{id} controls no menu.");
        return Page.Locator($"[id='{menuId}']");
    }

    private static ILocator CalloutOf(ILocator menu) => menu.Locator("xpath=..");

    private static ILocator ItemsOf(ILocator menu) => menu.Locator(":scope > li > .bit-mnb-itm");

    // The submenu a row opens, once the row names it (aria-controls is only written while it is open).
    private async Task<ILocator> SubmenuOfAsync(ILocator row)
    {
        await Expect(row).ToHaveAttributeAsync("aria-controls", new Regex(".+"));
        return Page.Locator($"[id='{await row.GetAttributeAsync("aria-controls")}']");
    }

    // A callout slides in from the side it is placed on; its box is measured once that transition is over.
    private static async Task SettledAsync(ILocator callout)
    {
        await Expect(callout).ToHaveCSSAsync("transform", new Regex(@"^(none|matrix\(1, 0, 0, 1, 0, 0\))$"));
    }

    private async Task<ILocator> OpenMenuAsync(string id)
    {
        var opener = await Chb(id).CountAsync() > 0 ? Chb(id) : Opb(id);
        await opener.ClickAsync();
        var menu = await MenuOf(id);
        await Expect(menu).ToBeVisibleAsync();
        return menu;
    }

    // ---------- opening and closing (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Basic_OpeningTheMenuAndClickingAnItem_RaisesOnClickClosesTheMenuAndReturnsTheFocus(string route)
    {
        await OpenAsync(route);

        var opb = Opb("mnb-basic");
        await Expect(Root("mnb-basic")).ToHaveClassAsync(HasClass("bit-mnb"));
        await Expect(Root("mnb-basic")).ToHaveClassAsync(HasClass("bit-mnb-nsp"));
        await Expect(opb).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(opb).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(opb.Locator(".bit-mnb-btx")).ToHaveTextAsync("Actions");
        await Expect(opb.Locator(".bit-mnb-chv")).ToHaveCountAsync(1);

        var menu = await MenuOf("mnb-basic");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(menu).ToHaveAttributeAsync("role", "menu");

        await opb.ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Expect(opb).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Root("mnb-basic")).ToHaveClassAsync(HasClass("bit-mnb-omn"));
        await Expect(ItemsOf(menu)).ToHaveCountAsync(3);
        await Expect(ItemsOf(menu).Locator(".bit-mnb-btx")).ToHaveTextAsync(["Add", "Edit", "Delete"]);
        await Expect(ItemsOf(menu).First).ToHaveAttributeAsync("role", "menuitem");
        await Expect(ItemsOf(menu).First).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(CalloutOf(menu)).ToHaveCSSAsync("opacity", "1");

        await ItemsOf(menu).Nth(1).ClickAsync();

        await Expect(Text("basic-clicked")).ToHaveTextAsync("Edit");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(opb).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Root("mnb-basic")).Not.ToHaveClassAsync(HasClass("bit-mnb-omn"));
        await Expect(opb).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Callout_WhenOpen_IsMovedToTheBodyAndPlacedUnderTheButton(string route)
    {
        await OpenAsync(route);

        var menu = await MenuOf("mnb-basic");
        var callout = CalloutOf(menu);
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#basic') !== null"), "Closed, the callout is rendered beside the button.");

        await Opb("mnb-basic").ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Expect(callout).ToHaveCSSAsync("position", "fixed");
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#basic') === null"), "Open, the callout is moved out to the body.");

        await SettledAsync(callout);
        var button = await Root("mnb-basic").BoxAsync();
        var box = await callout.BoxAsync();
        Assert.IsTrue(box.Y >= button.Y + button.Height - 1.5, $"The callout ({box.Y}) opens under the button ({button.Y + button.Height}).");
        ButtonsE2EHelpers.AreClose(button.X, box.X, 2, "The callout is aligned with the button");

        await Page.Mouse.ClickAsync(button.X + button.Width + 300, button.Y + 200);
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Opb("mnb-basic")).ToBeFocusedAsync();
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.closest('#basic') !== null"), "Closed again, the callout goes back beside the button.");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Split_TheMainHalfRunsOnClickAndTheChevronOpensTheMenu(string route)
    {
        await OpenAsync(route);

        await Expect(Root("mnb-split")).ToHaveClassAsync(HasClass("bit-mnb-spl"));
        await Expect(Opb("mnb-split")).Not.ToHaveAttributeAsync("aria-haspopup", ButtonsE2EHelpers.AnyValue);
        await Expect(Root("mnb-split").Locator(".bit-mnb-spb")).ToHaveAttributeAsync("aria-hidden", "true");

        var chevron = Chb("mnb-split");
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "More options");
        await Expect(chevron).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "false");

        await Opb("mnb-split").ClickAsync();
        await Expect(Text("split-clicked")).ToHaveTextAsync("header");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "false");

        var menu = await OpenMenuAsync("mnb-split");
        await Expect(chevron).ToHaveAttributeAsync("aria-expanded", "true");
        await ItemsOf(menu).Nth(2).ClickAsync();
        await Expect(Text("split-clicked")).ToHaveTextAsync("Delete");
        await Expect(chevron).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task IsOpen_BoundTwoWay_OpensFromThePageAndReportsTheClose(string route)
    {
        await OpenAsync(route);

        await Page.Locator("#btn-open").ClickAsync();
        var menu = await MenuOf("mnb-open-bind");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(Text("open-value")).ToHaveTextAsync("True");
        await Expect(Opb("mnb-open-bind")).ToHaveAttributeAsync("aria-expanded", "true");

        await ItemsOf(menu).First.ClickAsync();
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Text("open-value")).ToHaveTextAsync("False");

        await Opb("mnb-open-bind").ClickAsync();
        await Expect(Text("open-value")).ToHaveTextAsync("True");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("open-value")).ToHaveTextAsync("False");
    }

    // ---------- sticky (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Sticky_SelectingAnItem_PromotesItToTheHeaderAndBindsIt(string route)
    {
        await OpenAsync(route);

        await Expect(Opb("mnb-sticky").Locator(".bit-mnb-btx")).ToHaveTextAsync("Add");
        await Expect(Text("sticky-selected")).ToHaveTextAsync("add");

        var menu = await OpenMenuAsync("mnb-sticky");
        await Expect(ItemsOf(menu).Locator(".bit-mnb-btx")).ToHaveTextAsync(["Edit", "Delete"]);

        await ItemsOf(menu).First.ClickAsync();
        await Expect(Opb("mnb-sticky").Locator(".bit-mnb-btx")).ToHaveTextAsync("Edit");
        await Expect(Text("sticky-selected")).ToHaveTextAsync("edit");
        await Expect(Text("sticky-changes")).ToHaveTextAsync("edit");

        menu = await OpenMenuAsync("mnb-sticky");
        await Expect(ItemsOf(menu).Locator(".bit-mnb-btx")).ToHaveTextAsync(["Add", "Delete"]);
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Sticky_DefaultSelectedItemAndIsSelected_SeedTheHeader(string route)
    {
        await OpenAsync(route);

        await Expect(Opb("mnb-sticky-default").Locator(".bit-mnb-btx")).ToHaveTextAsync("Edit");
        await Expect(Opb("mnb-sticky-isselected").Locator(".bit-mnb-btx")).ToHaveTextAsync("Delete");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task SplitSticky_TheMainHalf_RunsTheSelectedItemsOwnAction(string route)
    {
        await OpenAsync(route);

        await Opb("mnb-split-sticky").ClickAsync();
        await Expect(Text("split-sticky-header")).ToHaveTextAsync("add");
        await Expect(Text("split-sticky-actions")).ToHaveTextAsync("1");

        var menu = await OpenMenuAsync("mnb-split-sticky");
        await ItemsOf(menu).First.ClickAsync();
        await Expect(Opb("mnb-split-sticky").Locator(".bit-mnb-btx")).ToHaveTextAsync("Edit");
        await Expect(Text("split-sticky-actions")).ToHaveTextAsync("1");

        await Opb("mnb-split-sticky").ClickAsync();
        await Expect(Text("split-sticky-header")).ToHaveTextAsync("edit");
        await Expect(Text("split-sticky-actions")).ToHaveTextAsync("11");
    }

    // ---------- content (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Icons_OnTheHeaderAndTheItems_IconOnlyAndNoIcon(string route)
    {
        await OpenAsync(route);

        await Expect(Opb("mnb-icons").Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(Opb("mnb-icons").Locator(".bit-mnb-ico")).ToHaveAttributeAsync("aria-hidden", "true");
        var menu = await OpenMenuAsync("mnb-icons");
        await Expect(ItemsOf(menu).Nth(0).Locator(".bit-mnb-iic")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-iic")).ToHaveClassAsync(HasClass("bit-icon--Edit"));
        await Expect(ItemsOf(menu).Nth(2).Locator(".bit-mnb-iic")).ToHaveClassAsync(HasClass("bi-trash"));
        await Page.Keyboard.PressAsync("Escape");

        var iconOnly = Opb("mnb-icononly");
        await Expect(Root("mnb-icononly")).ToHaveClassAsync(HasClass("bit-mnb-ion"));
        await Expect(iconOnly).ToHaveAccessibleNameAsync("More actions");
        await Expect(iconOnly.Locator(".bit-mnb-btx")).ToHaveCountAsync(0);
        await Expect(iconOnly.Locator(".bit-mnb-chv")).ToHaveCountAsync(0);
        await Expect(iconOnly.Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("bit-icon--More"));

        await Expect(Opb("mnb-noicon").Locator(".bit-mnb-ico")).ToHaveCountAsync(0);
        await Expect(Opb("mnb-noicon").Locator(".bit-mnb-btx")).ToHaveTextAsync("No icon");

        await Expect(Opb("mnb-ext-icon").Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("fa-house"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Toggle_ClickingTheMainHalf_TogglesItAndBindsTheState(string route)
    {
        await OpenAsync(route);

        var opb = Opb("mnb-toggle");
        await Expect(opb).ToHaveAttributeAsync("aria-pressed", "false");

        await opb.ClickAsync();
        await Expect(opb).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Root("mnb-toggle")).ToHaveClassAsync(HasClass("bit-mnb-tgl"));
        await Expect(Text("toggle-value")).ToHaveTextAsync("True");
        await Expect(Text("toggle-changes")).ToHaveTextAsync("True");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(opb).ToHaveCSSAsync("background-color", await Token("pri-dark"));

        await opb.ClickAsync();
        await Expect(opb).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Text("toggle-changes")).ToHaveTextAsync("True,False");

        await Expect(Opb("mnb-toggle-default")).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Checkable_CheckboxAndRadioItems_ToggleAndPickWithTheirOwnRoles(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-check");
        var items = ItemsOf(menu);
        var bold = items.Nth(0);
        var italic = items.Nth(1);
        var left = items.Nth(2);
        var center = items.Nth(3);

        await Expect(bold).ToHaveAttributeAsync("role", "menuitemcheckbox");
        await Expect(bold).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(italic).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(italic).ToHaveClassAsync(HasClass("bit-mnb-chk"));
        await Expect(italic.Locator(".bit-mnb-ick")).ToHaveCSSAsync("visibility", "visible");
        await Expect(bold.Locator(".bit-mnb-ick")).ToHaveCSSAsync("visibility", "hidden");
        await Expect(left).ToHaveAttributeAsync("role", "menuitemradio");
        await Expect(left).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(menu.Locator("[role=separator]")).ToHaveCountAsync(1);
        await Expect(italic).ToHaveCSSAsync("background-color", await Token("bg-sec"));

        await bold.ClickAsync();
        await Expect(bold).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(Text("check-state")).ToHaveTextAsync("bold,italic,left");

        await center.ClickAsync();
        await Expect(center).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(left).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Text("check-state")).ToHaveTextAsync("bold,italic,center");

        await center.ClickAsync();
        await Expect(center).ToHaveAttributeAsync("aria-checked", "true");

        await italic.ClickAsync();
        await Expect(italic).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Text("check-state")).ToHaveTextAsync("bold,center");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Templates_HeaderItemTemplateAndAnItemsOwn_ReplaceTheContent(string route)
    {
        await OpenAsync(route);

        await Expect(Opb("mnb-header-template").Locator(".tpl-header")).ToHaveTextAsync("Custom header");

        var menu = await OpenMenuAsync("mnb-item-template");
        await Expect(ItemsOf(menu).Locator(".tpl-item")).ToHaveTextAsync(["Add", "Edit", "Delete"]);
        await Page.Keyboard.PressAsync("Escape");

        menu = await OpenMenuAsync("mnb-own-template");
        await Expect(ItemsOf(menu).Nth(0).Locator(".tpl-own")).ToHaveTextAsync("Custom template");
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-btx")).ToHaveTextAsync("Plain");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Grouping_HeadersAndSeparators_AreNotItemsAndAreSkippedByTheKeyboard(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-grouping");
        var headers = menu.Locator(":scope > li.bit-mnb-ihd");
        await Expect(headers).ToHaveCountAsync(2);
        await Expect(headers.First).ToHaveAttributeAsync("role", "presentation");
        await Expect(headers.First).ToHaveTextAsync("Files");
        await Expect(headers.First).ToHaveAttributeAsync("title", "File commands");
        await Expect(menu.Locator(":scope > li[role=separator]")).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(ItemsOf(menu)).ToHaveCountAsync(3);

        await Page.Keyboard.PressAsync("Escape");
        await Opb("mnb-grouping").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemsOf(menu).Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(ItemsOf(menu).Nth(2)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(ItemsOf(menu).Nth(1)).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Details_SecondaryTextTitleAriaLabelAndADisabledItem(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-details");
        var save = ItemsOf(menu).Nth(0);
        var locked = ItemsOf(menu).Nth(1);
        var named = ItemsOf(menu).Nth(2);

        await Expect(save.Locator(".bit-mnb-isc")).ToHaveTextAsync("Ctrl+S");
        await Expect(save).ToHaveAttributeAsync("title", "Save the document");
        await Expect(save).ToHaveAccessibleNameAsync(new Regex("^Save\\s+Ctrl\\+S$"));
        await Expect(locked).ToBeDisabledAsync();
        await Expect(locked).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(locked).ToHaveCSSAsync("color", await Token("fg-dis"));
        await Expect(named).ToHaveAccessibleNameAsync("A named item");

        await locked.ClickAsync(new() { Force = true });
        await Expect(menu).ToBeVisibleAsync();

        await save.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(named).ToBeFocusedAsync();

        await named.ClickAsync();
        await Expect(Text("details-clicked")).ToHaveTextAsync("named");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task DisabledInteractive_KeepsADisabledItemReachableAndInert(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-disabled-interactive");
        var disabled = ItemsOf(menu).Nth(1);

        await Expect(disabled).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");

        await ItemsOf(menu).Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(disabled).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(menu).ToBeVisibleAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Links_RenderAnchorsWithRelAndANewTabHintAndNavigate(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-links");
        var home = ItemsOf(menu).Nth(0);
        Assert.AreEqual("A", await home.EvaluateAsync<string>("el => el.tagName"));
        await Expect(home).ToHaveAttributeAsync("role", "menuitem");
        await Expect(home).ToHaveAttributeAsync("href", "/");
        await Expect(home).ToHaveAttributeAsync("target", "_blank");
        await Expect(home).ToHaveAttributeAsync("rel", "noopener");
        await Expect(home.Locator(".bit-mnb-nth")).ToHaveTextAsync("(opens in a new tab)");
        await Expect(home).ToHaveAccessibleNameAsync(new Regex("^Home\\s*\\(opens in a new tab\\)$"));
        await Expect(ItemsOf(menu).Nth(1)).ToHaveAttributeAsync("rel", "nofollow");
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-nth")).ToHaveCountAsync(0);

        await ItemsOf(menu).Nth(2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#links$"));
        await Expect(menu).ToBeHiddenAsync();

        var hint = await OpenMenuAsync("mnb-links-hint");
        await Expect(ItemsOf(hint).Nth(0).Locator(".bit-mnb-nth")).ToHaveTextAsync("(new window)");
        await Page.Keyboard.PressAsync("Escape");

        var noHint = await OpenMenuAsync("mnb-links-nohint");
        await Expect(ItemsOf(noHint).Nth(0).Locator(".bit-mnb-nth")).ToHaveCountAsync(0);
    }

    // ---------- loading (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task AutoLoading_OnASplitButton_ShowsTheSpinnerAndTheLabelAndKeepsTheMenuReachable(string route)
    {
        await OpenAsync(route);

        var opb = Opb("mnb-loading");
        var status = Page.Locator("#mnb-loading ~ .bit-mnb-sts").First;
        await Expect(status).ToHaveTextAsync("");

        await opb.ClickAsync();
        await Expect(Text("loading-clicks")).ToHaveTextAsync("1");
        await Expect(Text("loading-value")).ToHaveTextAsync("True");
        await Expect(Root("mnb-loading")).ToHaveClassAsync(HasClass("bit-mnb-lod"));
        await Expect(opb).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(opb.Locator(".bit-mnb-spn")).ToBeVisibleAsync();
        await Expect(opb.Locator(".bit-mnb-ico")).ToHaveCountAsync(0);
        await Expect(opb.Locator(".bit-mnb-btx")).ToHaveTextAsync("Saving...");
        await Expect(status).ToHaveTextAsync("Saving...");
        await Expect(opb).ToHaveCSSAsync("cursor", "default");

        await opb.ClickAsync();
        var menu = await OpenMenuAsync("mnb-loading");
        await Expect(ItemsOf(menu)).ToHaveCountAsync(3);
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-release-loading").ClickAsync();
        await Expect(Text("loading-value")).ToHaveTextAsync("False");
        await Expect(opb).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
        await Expect(opb.Locator(".bit-mnb-btx")).ToHaveTextAsync("Save");
        await Expect(Text("loading-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task IsLoading_OnAPlainButton_RefusesToOpenAndRendersTheTemplateOrRepeats(string route)
    {
        await OpenAsync(route);

        var plain = Opb("mnb-loading-plain");
        await Expect(plain).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(plain.Locator(".bit-mnb-btx")).ToHaveTextAsync("Working...");
        await plain.ClickAsync();
        await plain.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Page.Locator("#btn-release-loading").ClickAsync();
        await Expect(plain).ToHaveAttributeAsync("aria-expanded", "false");

        await Expect(Opb("mnb-loading-template").Locator(".tpl-loading")).ToHaveTextAsync("Please wait");
        await Expect(Opb("mnb-loading-template").Locator(".bit-mnb-spn")).ToHaveCountAsync(0);

        var again = Opb("mnb-loading-reclickable");
        await Expect(Root("mnb-loading-reclickable")).ToHaveClassAsync(HasClass("bit-mnb-rcl"));
        await Expect(again).ToHaveCSSAsync("cursor", "pointer");
        await again.ClickAsync();
        await again.ClickAsync();
        await Expect(Text("reclicks")).ToHaveTextAsync("2");
    }

    // ---------- keyboard (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Keyboard_ArrowsHomeEndAndTypeahead_MoveTheFocusThroughTheMenu(string route)
    {
        await OpenAsync(route);

        await Page.Locator("#kbd-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Opb("mnb-keys")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        var menu = await MenuOf("mnb-keys");
        var items = ItemsOf(menu);
        await Expect(menu).ToBeVisibleAsync();
        await Expect(items.Nth(0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(items.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("End");
        await Expect(items.Nth(3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(items.Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(items.Nth(3)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Home");
        await Expect(items.Nth(0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("b");
        await Expect(items.Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("b");
        await Expect(items.Nth(2)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Opb("mnb-keys")).ToBeFocusedAsync();

        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollX"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Keyboard_ArrowUpEnterAndTab_OpenOnTheLastItemActivateAndLeave(string route)
    {
        await OpenAsync(route);

        var opb = Opb("mnb-keys");
        await opb.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        var menu = await MenuOf("mnb-keys");
        var items = ItemsOf(menu);
        await Expect(items.Nth(3)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("keys-clicked")).ToHaveTextAsync("cherry");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(opb).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(items.Nth(0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(opb).ToHaveAttributeAsync("aria-expanded", "false");
        Assert.IsTrue(await CalloutOf(menu).EvaluateAsync<bool>("el => !el.contains(document.activeElement)"), "Tab leaves the menu.");

        await opb.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(menu).ToBeVisibleAsync();
        await Expect(items.Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("keys-clicked")).ToHaveTextAsync("apple");
    }

    // ---------- submenus (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Submenu_FromTheKeyboard_OpensWithArrowRightAndClosesWithArrowLeftOrEscape(string route)
    {
        await OpenAsync(route);

        await Opb("mnb-submenu").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var menu = await MenuOf("mnb-submenu");
        var share = ItemsOf(menu).Nth(1);

        // the focus is moved into the menu by a round trip, so the next key waits for it to land there
        await Expect(ItemsOf(menu).Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(share).ToBeFocusedAsync();
        await Expect(share).ToHaveAttributeAsync("aria-haspopup", "menu");
        await Expect(share).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(share.Locator(".bit-mnb-sch")).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(share).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(share).ToHaveClassAsync(HasClass("bit-mnb-osb"));
        var submenu = await SubmenuOfAsync(share);
        await Expect(submenu).ToBeVisibleAsync();
        await Expect(submenu).ToHaveAttributeAsync("aria-labelledby", (await share.GetAttributeAsync("id"))!);
        var email = ItemsOf(submenu).Nth(0);
        await Expect(email).ToBeFocusedAsync();

        await SettledAsync(CalloutOf(submenu));
        var menuBox = await CalloutOf(menu).BoxAsync();
        var subBox = await CalloutOf(submenu).BoxAsync();
        Assert.IsTrue(subBox.X >= menuBox.X + menuBox.Width - 2, "The submenu opens beside the menu, on the right.");

        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemsOf(submenu).Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(submenu).ToBeHiddenAsync();
        await Expect(share).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(email).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(submenu).ToBeHiddenAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Expect(share).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Opb("mnb-submenu")).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Submenu_ByThePointer_OpensOnHoverAndAChildsClickClosesEveryMenu(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-submenu");
        var more = ItemsOf(menu).Nth(2);

        await more.HoverAsync();
        await Expect(more).ToHaveAttributeAsync("aria-expanded", "true");
        var submenu = await SubmenuOfAsync(more);
        await Expect(submenu).ToBeVisibleAsync();

        var print = ItemsOf(submenu).Nth(0);
        await print.HoverAsync();
        await Expect(print).ToHaveAttributeAsync("aria-expanded", "true");
        var nested = await SubmenuOfAsync(print);
        await Expect(nested).ToBeVisibleAsync();

        await ItemsOf(nested).Nth(0).ClickAsync();
        await Expect(Text("submenu-clicked")).ToHaveTextAsync("pdf");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(submenu).ToBeHiddenAsync();
        await Expect(nested).ToBeHiddenAsync();

        menu = await OpenMenuAsync("mnb-submenu");
        var share = ItemsOf(menu).Nth(1);
        await share.ClickAsync();
        submenu = await SubmenuOfAsync(share);
        await ItemsOf(submenu).Nth(0).ClickAsync();
        await Expect(Text("submenu-clicked")).ToHaveTextAsync("email-own");

        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    // ---------- styles, runtime and lifecycle (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task ItemClassAndStyle_ReachTheirItem(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-item-styles");
        await Expect(ItemsOf(menu).Nth(0)).ToHaveClassAsync(HasClass("item-own-class"));
        await Expect(ItemsOf(menu).Nth(1)).ToHaveCSSAsync("color", "rgb(255, 0, 255)");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Items_AddedAtRuntime_AppearInTheMenu(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-dynamic");
        await Expect(ItemsOf(menu)).ToHaveCountAsync(1);
        await Page.Keyboard.PressAsync("Escape");

        await Page.Locator("#btn-add-item").ClickAsync();
        await Page.Locator("#btn-add-item").ClickAsync();

        menu = await OpenMenuAsync("mnb-dynamic");
        await Expect(ItemsOf(menu).Locator(".bit-mnb-btx")).ToHaveTextAsync(["A", "New 1", "New 2"]);
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Unmount_WhileTheMenuIsOpen_TakesTheCalloutAwayWithoutAnError(string route)
    {
        await OpenAsync(route);

        var menu = await OpenMenuAsync("mnb-mounted");
        var menuId = await menu.GetAttributeAsync("id");

        await Page.Locator("#btn-unmount").DispatchEventAsync("click");

        await Expect(Root("mnb-mounted")).ToHaveCountAsync(0);
        await Expect(Page.Locator($"[id='{menuId}']")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".bit-mnb-ovl:visible")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Root("mnb-mounted")).ToBeVisibleAsync();
        await OpenMenuAsync("mnb-mounted");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- group level (Items page) ----------

    [TestMethod]
    [DataRow("Fill")]
    [DataRow("Outline")]
    [DataRow("Text")]
    public async Task Variant_EachValue_PaintsTheButtonTheSeparatorAndTheDisabledOne(string variant)
    {
        await OpenAsync(ItemsApi);

        var cssClass = variant switch { "Fill" => "bit-mnb-fil", "Outline" => "bit-mnb-otl", _ => "bit-mnb-txt" };
        var primary = await Token("pri");
        var onPrimary = await Token("pri-text");

        var root = Root($"mnb-variant-{variant}");
        await Expect(root).ToHaveClassAsync(HasClass(cssClass));
        await Expect(root).ToHaveCSSAsync("background-color", variant == "Fill" ? primary : Transparent);
        await Expect(root).ToHaveCSSAsync("color", variant == "Fill" ? onPrimary : primary);
        await Expect(root).ToHaveCSSAsync("border-top-color", variant == "Text" ? Transparent : primary);

        await Expect(Root($"mnb-variant-split-{variant}").Locator(".bit-mnb-spb")).ToHaveCSSAsync("background-color", variant == "Fill" ? onPrimary : primary);

        var disabled = Root($"mnb-variant-dis-{variant}");
        await Expect(disabled).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Opb($"mnb-variant-dis-{variant}")).ToBeDisabledAsync();
        await Expect(Opb($"mnb-variant-dis-{variant}")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabled).ToHaveCSSAsync("color", await Token("pri-dis-text"));
        await Expect(disabled).ToHaveCSSAsync("background-color", variant == "Fill" ? await Token("pri-dis") : Transparent);

        var opb = Opb($"mnb-variant-{variant}");
        await opb.HoverAsync();
        await Expect(opb).ToHaveCSSAsync("background-color", await Token("pri-hover"));

        await opb.ClickAsync();
        await Expect(opb.Locator(".bit-mnb-chv")).ToHaveCSSAsync("rotate", "180deg");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(opb).ToHaveCSSAsync("background-color", await Token("pri-active"));
    }

    [TestMethod]
    [DynamicData(nameof(ColorRows))]
    public async Task Color_EachValue_PaintsTheButtonItsToggledHalfAndTheDisabledOne(string name, string suffix, string token, string onToken)
    {
        await OpenAsync(ItemsApi);

        var root = Root($"mnb-color-{name}");
        await Expect(root).ToHaveClassAsync(HasClass($"bit-mnb-{suffix}"));
        await Expect(root).ToHaveCSSAsync("background-color", await Token(token));
        await Expect(root).ToHaveCSSAsync("color", await Token(onToken));

        await Expect(Root($"mnb-color-outline-{name}")).ToHaveCSSAsync("color", await Token(token));
        await Expect(Root($"mnb-color-outline-{name}")).ToHaveCSSAsync("border-top-color", await Token(token));
        await Expect(Opb($"mnb-color-toggle-{name}")).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync($"var(--bit-clr-{token}-dark)"));
        await Expect(Root($"mnb-color-dis-{name}")).ToHaveCSSAsync("background-color", await Token($"{token}-dis"));

        var menu = await OpenMenuAsync($"mnb-color-{name}");
        await Expect(CalloutOf(menu)).ToHaveClassAsync(HasClass($"bit-mnb-{suffix}"));
    }

    public static IEnumerable<object[]> ColorRows => ButtonsE2EHelpers.ColorRows;

    [TestMethod]
    [DataRow("Small", "sm", "xs")]
    [DataRow("Medium", "md", "sm")]
    [DataRow("Large", "lg", "md")]
    public async Task Size_EachValue_SetsTheButtonAndTheItemsOfItsClass(string size, string suffix, string font)
    {
        await OpenAsync(ItemsApi);

        var root = Root($"mnb-size-{size}");
        await Expect(root).ToHaveClassAsync(HasClass($"bit-mnb-{suffix}"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight(suffix)), await root.PxAsync("min-height"), "min-height");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-tpg-fs-{font})"), await root.PxAsync("font-size"), "font-size");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-siz-icon-{suffix})"), await Opb($"mnb-size-{size}").Locator(".bit-mnb-ico").PxAsync("font-size"), "icon size");

        var itemHeight = await Page.ResolvePxAsync($"var(--bit-siz-item-{suffix}, calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * var(--bit-siz-item-{suffix}-steps)))");
        var menu = await OpenMenuAsync($"mnb-size-{size}");
        await Expect(CalloutOf(menu)).ToHaveClassAsync(HasClass($"bit-mnb-{suffix}"));
        Assert.AreEqual(itemHeight, await ItemsOf(menu).First.PxAsync("min-height"), "item height");
        await Page.Keyboard.PressAsync("Escape");

        var iconOnly = await Root($"mnb-size-icon-{size}").BoxAsync();
        Assert.IsTrue(iconOnly.Width >= iconOnly.Height - 1, "An icon-only menu button is at least square.");
    }

    [TestMethod]
    public async Task Layout_FullWidthTheChevronAndTheMarkIcons()
    {
        await OpenAsync(ItemsApi);

        await Expect(Root("mnb-fullwidth")).ToHaveClassAsync(HasClass("bit-mnb-flw"));
        ButtonsE2EHelpers.AreClose(420, (await Root("mnb-fullwidth").BoxAsync()).Width, 0.5, "full width");

        var chevron = Chb("mnb-chevron");
        await Expect(chevron.Locator(".bit-mnb-chv")).ToHaveClassAsync(HasClass("bit-icon--ChevronDownMed"));
        await Expect(chevron).ToHaveAttributeAsync("aria-label", "Show the save options");
        await Expect(chevron).ToHaveAttributeAsync("title", "More save options");

        var menu = await OpenMenuAsync("mnb-icons-custom");
        await Expect(ItemsOf(menu).Nth(0).Locator(".bit-mnb-ick")).ToHaveClassAsync(HasClass("bit-icon--CheckboxComposite"));
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-ick")).ToHaveClassAsync(HasClass("bit-icon--RadioBtnOn"));
        await Expect(ItemsOf(menu).Nth(2).Locator(".bit-mnb-sch")).ToHaveClassAsync(HasClass("bit-icon--ChevronRightMed"));
    }

    [TestMethod]
    [DataRow("Primary", "bit-mnb-bpg", "var(--bit-clr-bg-pri)")]
    [DataRow("Secondary", "bit-mnb-bsg", "var(--bit-clr-bg-sec)")]
    [DataRow("Tertiary", "bit-mnb-btg", "var(--bit-clr-bg-ter)")]
    [DataRow("Transparent", "bit-mnb-brg", "transparent")]
    public async Task Background_EachValue_PaintsTheCallout(string kind, string cssClass, string color)
    {
        await OpenAsync(ItemsApi);

        var menu = await OpenMenuAsync($"mnb-background-{kind}");
        await Expect(CalloutOf(menu)).ToHaveClassAsync(HasClass(cssClass));
        await Expect(CalloutOf(menu)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync(color));
    }

    [TestMethod]
    public async Task MaxHeightAndCloseOnItemClick_CapTheMenuAndKeepItOpen()
    {
        await OpenAsync(ItemsApi);

        var menu = await OpenMenuAsync("mnb-maxheight");
        var callout = CalloutOf(menu);
        await Expect(callout).ToHaveClassAsync(HasClass("bit-mnb-mxh"));
        await Expect(callout).ToHaveAttributeAsync("style", new Regex("--bit-MenuButton-callout-max-height:\\s*10rem"));
        ButtonsE2EHelpers.AreClose(await Page.ResolvePxAsync("10rem"), (await callout.BoxAsync()).Height, 1, "the menu is capped");
        Assert.IsTrue(await callout.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The capped menu scrolls.");
        await ItemsOf(menu).Last.ScrollIntoViewIfNeededAsync();
        await Expect(ItemsOf(menu).Last).ToBeInViewportAsync();
        await Page.Keyboard.PressAsync("Escape");

        menu = await OpenMenuAsync("mnb-stays-open");
        await ItemsOf(menu).First.ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task ButtonTypeAndFormId_SubmitTheFormFromTheMainHalfOrAnItem()
    {
        await OpenAsync(ItemsApi);

        await Expect(Opb("mnb-form-split")).ToHaveAttributeAsync("type", "submit");
        await Expect(Opb("mnb-form-plain")).ToHaveAttributeAsync("type", "button");

        await Opb("mnb-form-split").ClickAsync();
        await Expect(Text("form-submits")).ToHaveTextAsync("1");

        var menu = await OpenMenuAsync("mnb-form-plain");
        await Expect(ItemsOf(menu).First).ToHaveAttributeAsync("type", "submit");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Opb("mnb-form-external")).ToHaveAttributeAsync("form", "mnb-ext-form");
        await Opb("mnb-form-external").ClickAsync();
        await Expect(Text("form-ext-submits")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task StopPropagation_SetOrNot_KeepsTheClickFromTheParentOrLetsItBubble()
    {
        await OpenAsync(ItemsApi);

        await Opb("mnb-stop").ClickAsync();
        await Expect(Text("header-clicks")).ToHaveTextAsync("1");

        await Opb("mnb-bubble").ClickAsync();
        await Expect(Text("header-clicks")).ToHaveTextAsync("2");
        await Expect(Text("parent-clicks")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task LoadingDelay_ALongRun_ShowsTheSpinnerOnlyOnceTheDelayIsUp()
    {
        await OpenAsync(ItemsApi);

        var opb = Opb("mnb-delay");
        await opb.EvaluateAsync(
            """
            el => {
                window.__delay = {};
                const start = performance.now();
                new MutationObserver(() => {
                    if (el.getAttribute('aria-busy') === 'true' && window.__delay.busy === undefined) window.__delay.busy = performance.now() - start;
                    if (el.querySelector('.bit-mnb-spn') && window.__delay.spinner === undefined) window.__delay.spinner = performance.now() - start;
                }).observe(el, { attributes: true, childList: true, subtree: true });
            }
            """);

        await opb.ClickAsync();
        await Expect(opb.Locator(".bit-mnb-spn")).ToBeVisibleAsync();

        var busy = await Page.EvaluateAsync<double>("() => window.__delay.busy");
        var spinner = await Page.EvaluateAsync<double>("() => window.__delay.spinner");
        Assert.IsTrue(spinner - busy >= 450, $"The spinner came {spinner - busy}ms after the busy state; the delay is 500ms.");

        await Page.Locator("#btn-release-delay").ClickAsync();
        await Expect(opb.Locator(".bit-mnb-spn")).ToHaveCountAsync(0);
        await Expect(opb).Not.ToHaveAttributeAsync("aria-busy", ButtonsE2EHelpers.AnyValue);
    }

    [TestMethod]
    [Ignore("KNOWN BUG: inside an EditForm the items default to type=submit, but the open menu is moved to the body, out of the <form>, and they render no form attribute without FormId - so clicking one submits nothing.")]
    public async Task Items_InsideAnEditForm_SubmitTheFormTheyDefaultToSubmitting()
    {
        await OpenAsync(ItemsApi);

        var menu = await OpenMenuAsync("mnb-form-plain");
        await Expect(ItemsOf(menu).First).ToHaveAttributeAsync("type", "submit");
        await ItemsOf(menu).First.ClickAsync();

        await Expect(Text("form-submits")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task Accessibility_TitleDescriptionHiddenAndFocusAsync()
    {
        await OpenAsync(ItemsApi);

        var opb = Opb("mnb-a11y");
        await Expect(opb).ToHaveAttributeAsync("title", "The menu button tooltip");
        await Expect(opb).ToHaveAttributeAsync("aria-describedby", "a11y-extra mnb-a11y-dsc");
        await Expect(Text("mnb-a11y-dsc")).ToHaveTextAsync("Opens the actions menu");
        await Expect(opb).ToHaveAccessibleDescriptionAsync("Author description Opens the actions menu");
        var menu = await MenuOf("mnb-a11y");
        await Expect(menu).ToHaveAttributeAsync("aria-labelledby", "mnb-a11y-opb");

        await Expect(Opb("mnb-a11y-hidden")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Opb("mnb-a11y-hidden")).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(Chb("mnb-a11y-hidden")).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Chb("mnb-a11y-hidden")).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Locator("#btn-focus").ClickAsync();
        await Expect(Opb("mnb-focus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task AutoFocus_OnTheFirstRender_FocusesTheMainHalfButNeverAHiddenOrDisabledOne()
    {
        await OpenAsync("/e2e/menu-button/autofocus");

        await Expect(Opb("mnb-autofocus-hidden")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Opb("mnb-autofocus-disabled")).Not.ToHaveAttributeAsync("autofocus", ButtonsE2EHelpers.AnyValue);
        await Expect(Opb("mnb-autofocus")).ToHaveAttributeAsync("autofocus", "");
        await Expect(Opb("mnb-autofocus")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Disabled_TheMenuButton_RunsNoHandlerAndOpensNothingUntilItIsEnabled()
    {
        await OpenAsync(ItemsApi);

        var root = Root("mnb-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Opb("mnb-disabled")).ToBeDisabledAsync();
        await Expect(Chb("mnb-disabled")).ToBeDisabledAsync();
        await Expect(Chb("mnb-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        await root.DispatchEventAsync("click");
        await root.DispatchEventAsync("mousedown");
        await Opb("mnb-disabled").DispatchEventAsync("click");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-page-events")).ToHaveTextAsync("0");

        await Opb("mnb-disabled").ClickAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-page-events")).ToHaveTextAsync("2");

        var focusable = Opb("mnb-disabled-focusable");
        await Expect(focusable).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(focusable).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(focusable).ToHaveAttributeAsync("tabindex", "0");
        await focusable.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(focusable).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task BitParams_AroundMenuButtons_FillsInWhatEachLeftUnset()
    {
        await OpenAsync(ItemsApi);

        foreach (var cssClass in new[] { "bit-mnb-otl", "bit-mnb-sm", "bit-mnb-sec", "bit-mnb-spl" })
        {
            await Expect(Root("params-inherit")).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(Chb("params-inherit")).ToHaveCountAsync(1);
        await Expect(Root("params-inherit")).ToHaveCSSAsync("border-top-color", await Token("sec"));
        await Expect(Root("params-own")).ToHaveClassAsync(HasClass("bit-mnb-fil"));
        await Expect(Root("params-own")).ToHaveClassAsync(HasClass("bit-mnb-sm"));
        await Expect(Root("params-outside")).ToHaveClassAsync(HasClass("bit-mnb-nsp"));
        await Expect(Root("params-outside")).Not.ToHaveClassAsync(HasClass("bit-mnb-sm"));
    }

    [TestMethod]
    public async Task Styles_PerPart_ReachEveryPartOfTheButtonAndTheMenus()
    {
        await OpenAsync(ItemsApi);

        await Expect(Root("sc-style")).ToHaveCSSAsync("margin-left", "11px");
        await Expect(Root("sc-style")).ToHaveClassAsync(HasClass("e2e-custom-class"));
        await Expect(Root("sc-style")).ToHaveAttributeAsync("data-e2e", "custom-attribute");

        var root = Root("sc-styles");
        await Expect(root).ToHaveCSSAsync("outline-style", "dotted");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(0, 0, 255)");
        await Expect(Opb("sc-styles")).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(Opb("sc-styles").Locator(".bit-mnb-ico")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Opb("sc-styles").Locator(".bit-mnb-btx")).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(Chb("sc-styles")).ToHaveCSSAsync("padding-left", "9px");
        await Expect(Chb("sc-styles").Locator(".bit-mnb-chv")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(root.Locator(".bit-mnb-spb")).ToHaveCSSAsync("width", "5px");

        var menu = await OpenMenuAsync("sc-styles");
        await Expect(root).ToHaveCSSAsync("margin-top", "3px");
        await Expect(CalloutOf(menu)).ToHaveCSSAsync("border-top-color", "rgb(255, 0, 255)");
        await Expect(menu).ToHaveCSSAsync("padding-top", "6px");
        await Expect(menu.Locator(":scope > li").Nth(1)).ToHaveCSSAsync("margin-left", "4px");
        var add = ItemsOf(menu).Nth(0);
        await Expect(add).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(add.Locator(".bit-mnb-iic")).ToHaveCSSAsync("color", "rgb(128, 0, 128)");
        await Expect(add.Locator(".bit-mnb-btx")).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(add.Locator(".bit-mnb-isc")).ToHaveCSSAsync("color", "rgb(0, 0, 255)");
        await Expect(add.Locator(".bit-mnb-ick")).ToHaveCSSAsync("color", "rgb(255, 165, 0)");
        await Expect(menu.Locator(".bit-mnb-ihd")).ToHaveCSSAsync("color", "rgb(0, 100, 0)");
        await Expect(menu.Locator(".bit-mnb-isp")).ToHaveCSSAsync("height", "4px");
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-sch")).ToHaveCSSAsync("color", "rgb(10, 10, 10)");
        await ItemsOf(menu).Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        var submenu = await SubmenuOfAsync(ItemsOf(menu).Nth(1));
        await Expect(CalloutOf(submenu)).ToHaveCSSAsync("padding-top", "7px");
        await Page.Keyboard.PressAsync("Escape");
        await Page.Keyboard.PressAsync("Escape");

        await Expect(Opb("sc-sticky").Locator(".bit-mnb-ico")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(Opb("sc-sticky").Locator(".bit-mnb-btx")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(Opb("sc-sticky").Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("c-obi"));
        await Expect(Opb("sc-sticky").Locator(".bit-mnb-btx")).ToHaveClassAsync(HasClass("c-obt"));
        await Expect(Opb("sc-spinner").Locator(".bit-mnb-spn")).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");
        await Expect(Opb("sc-spinner").Locator(".bit-mnb-spn")).ToHaveClassAsync(HasClass("c-spinner"));
    }

    [TestMethod]
    public async Task Classes_PerPart_ReachEveryPartOfTheButtonAndTheMenus()
    {
        await OpenAsync(ItemsApi);

        var root = Root("sc-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        await Expect(root).ToHaveClassAsync(HasClass("c-toggled"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("c-opened"));
        await Expect(Opb("sc-classes")).ToHaveClassAsync(HasClass("c-opb"));
        await Expect(Opb("sc-classes").Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(Opb("sc-classes").Locator(".bit-mnb-btx")).ToHaveClassAsync(HasClass("c-text"));
        await Expect(Chb("sc-classes")).ToHaveClassAsync(HasClass("c-chb"));
        await Expect(Chb("sc-classes").Locator(".bit-mnb-chv")).ToHaveClassAsync(HasClass("c-chv"));
        await Expect(root.Locator(".bit-mnb-spb")).ToHaveClassAsync(HasClass("c-spb"));

        var menu = await OpenMenuAsync("sc-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-opened"));
        await Expect(CalloutOf(menu)).ToHaveClassAsync(HasClass("c-callout"));
        await Expect(menu).ToHaveClassAsync(HasClass("c-cul"));
        await Expect(menu.Locator(":scope > li").Nth(1)).ToHaveClassAsync(HasClass("c-iwr"));
        var add = ItemsOf(menu).Nth(0);
        await Expect(add).ToHaveClassAsync(HasClass("c-itm"));
        await Expect(add.Locator(".bit-mnb-iic")).ToHaveClassAsync(HasClass("c-iic"));
        await Expect(add.Locator(".bit-mnb-btx")).ToHaveClassAsync(HasClass("c-itx"));
        await Expect(add.Locator(".bit-mnb-isc")).ToHaveClassAsync(HasClass("c-isc"));
        await Expect(add.Locator(".bit-mnb-ick")).ToHaveClassAsync(HasClass("c-ick"));
        await Expect(menu.Locator(".bit-mnb-ihd")).ToHaveClassAsync(HasClass("c-ihd"));
        await Expect(menu.Locator(".bit-mnb-isp")).ToHaveClassAsync(HasClass("c-isp"));
        await Expect(ItemsOf(menu).Nth(1).Locator(".bit-mnb-sch")).ToHaveClassAsync(HasClass("c-sch"));
        await Expect(Page.Locator(".bit-mnb-ovl.c-ovl")).ToBeVisibleAsync();
        await ItemsOf(menu).Nth(1).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        var submenu = await SubmenuOfAsync(ItemsOf(menu).Nth(1));
        await Expect(CalloutOf(submenu)).ToHaveClassAsync(HasClass("c-sub"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheButtonAndTheMenuAndLoseToExplicitParameters()
    {
        await OpenAsync(ItemsApi);

        var root = Root("vars-default");
        await Expect(root).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(root).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(root).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(root).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(root).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(root).ToHaveCSSAsync("min-height", "50px");
        await Expect(root).ToHaveCSSAsync("font-size", "19px");
        await Expect(Opb("vars-default")).ToHaveCSSAsync("padding-left", "21px");
        await Expect(Opb("vars-default")).ToHaveCSSAsync("column-gap", "12px");
        await Expect(Opb("vars-default").Locator(".bit-mnb-ico")).ToHaveCSSAsync("font-size", "25px");

        await Expect(Root("vars-color")).ToHaveCSSAsync("background-color", await Token("err"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight("sm")), await Root("vars-size").PxAsync("min-height"));
        await Expect(Opb("vars-toggled")).ToHaveCSSAsync("background-color", "rgb(0, 0, 150)");
        await Expect(Root("vars-disabled")).ToHaveCSSAsync("background-color", "rgb(77, 77, 77)");

        await Opb("vars-default").HoverAsync();
        await Expect(Opb("vars-default")).ToHaveCSSAsync("background-color", "rgb(255, 128, 0)");

        var menu = await OpenMenuAsync("vars-default");
        var callout = CalloutOf(menu);
        await Expect(callout).ToHaveCSSAsync("background-color", "rgb(240, 240, 200)");
        await Expect(callout).ToHaveCSSAsync("border-bottom-left-radius", "9px");
        await Expect(ItemsOf(menu).First).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(ItemsOf(menu).First).ToHaveCSSAsync("min-height", "44px");
        await ItemsOf(menu).First.HoverAsync();
        await Expect(ItemsOf(menu).First).ToHaveCSSAsync("background-color", "rgb(0, 255, 255)");
        await Page.Keyboard.PressAsync("Escape");

        menu = await OpenMenuAsync("vars-background");
        await Expect(CalloutOf(menu)).ToHaveCSSAsync("background-color", await Token("bg-ter"));
    }

    [TestMethod]
    public async Task CssVariables_VariantTwins_RestyleTheirOwnLookOnly()
    {
        await OpenAsync(ItemsApi);

        await Expect(Root("vars-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 200)");
        await Expect(Root("vars-twin-fill")).Not.ToHaveCSSAsync("border-top-color", "rgb(200, 0, 200)");
        await Expect(Root("vars-twin-text")).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(Root("vars-twin-fill")).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(Root("vars-twin-outline")).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    [TestMethod]
    [DataRow("mnb-rtl", true)]
    [DataRow("mnb-rtl-inherited", false)]
    public async Task Rtl_ExplicitOrInherited_AlignsTheMenuRightAndOpensSubmenusToTheLeft(string id, bool explicitDir)
    {
        await OpenAsync(ItemsApi);

        if (explicitDir)
        {
            await Expect(Root(id)).ToHaveAttributeAsync("dir", "rtl");
            await Expect(Root(id)).ToHaveClassAsync(HasClass("bit-rtl"));
        }

        var opb = Opb(id);
        Assert.IsTrue((await opb.Locator(".bit-mnb-ico").BoxAsync()).X > (await opb.Locator(".bit-mnb-btx").BoxAsync()).X, "The icon is on the right.");

        await opb.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var menu = await MenuOf(id);
        await Expect(menu).ToBeVisibleAsync();
        await SettledAsync(CalloutOf(menu));
        var button = await Root(id).BoxAsync();
        var callout = await CalloutOf(menu).BoxAsync();
        ButtonsE2EHelpers.AreClose(button.X + button.Width, callout.X + callout.Width, 2, "The menu is aligned with the right edge of the button");

        await Expect(ItemsOf(menu).Nth(0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var share = ItemsOf(menu).Nth(1);
        await Expect(share).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(share).ToHaveAttributeAsync("aria-expanded", "false");
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(share).ToHaveAttributeAsync("aria-expanded", "true");

        var submenu = await SubmenuOfAsync(share);
        await Expect(ItemsOf(submenu).First).ToBeFocusedAsync();
        await SettledAsync(CalloutOf(submenu));
        var sub = await CalloutOf(submenu).BoxAsync();
        Assert.IsTrue(sub.X + sub.Width <= callout.X + 2, "In RTL the submenu opens on the left.");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(submenu).ToBeHiddenAsync();
        await Expect(share).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheMenuButton()
    {
        await OpenAsync(ItemsApi);

        await Page.Locator("#btn-vis-hidden").ClickAsync();
        await Expect(Root("mnb-visibility")).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-vis-collapsed").ClickAsync();
        await Expect(Root("mnb-visibility")).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-vis-visible").ClickAsync();
        await Expect(Root("mnb-visibility")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Parameters_ChangedAtRuntime_ReRenderTheMenuButton()
    {
        await OpenAsync(ItemsApi);

        await Expect(Chb("mnb-rerender")).ToHaveCountAsync(0);
        await Page.Locator("#btn-rerender").ClickAsync();

        var root = Root("mnb-rerender");
        foreach (var cssClass in new[] { "bit-mnb-otl", "bit-mnb-err", "bit-mnb-lg", "bit-mnb-spl" })
        {
            await Expect(root).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(Chb("mnb-rerender")).ToHaveCountAsync(1);
        await Expect(Opb("mnb-rerender").Locator(".bit-mnb-btx")).ToHaveTextAsync("After");
        await Expect(Opb("mnb-rerender").Locator(".bit-mnb-ico")).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(root).ToHaveCSSAsync("color", await Token("err"));
    }

    [TestMethod]
    [DataRow("mnb-in-modal")]
    [DataRow("mnb-in-modal-split")]
    public async Task Escape_InsideAModal_ClosesTheMenuFirstAndTheModalOnTheNextPress(string id)
    {
        await OpenAsync(ItemsApi);

        await Page.Locator("#btn-open-modal").ClickAsync();
        await Expect(Text("modal-open")).ToHaveTextAsync("True");
        var opener = id.EndsWith("split") ? Chb(id) : Opb(id);
        await Expect(opener).ToBeVisibleAsync();

        // the modal moves the focus to its first control once it has opened; the test takes it from there
        await Expect(Opb("mnb-in-modal")).ToBeFocusedAsync();

        await opener.FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        var menu = await MenuOf(id);
        await Expect(menu).ToBeVisibleAsync();
        await Expect(ItemsOf(menu).First).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(opener).ToBeFocusedAsync();
        await Expect(Text("modal-open")).ToHaveTextAsync("True");
        await Expect(opener).ToBeVisibleAsync();

        await opener.ClickAsync();
        await Expect(menu).ToBeVisibleAsync();
        await Page.Keyboard.PressAsync("Escape");
        await Expect(menu).ToBeHiddenAsync();
        await Expect(Text("modal-open")).ToHaveTextAsync("True");

        await Page.Keyboard.PressAsync("Escape");
        await Expect(Text("modal-open")).ToHaveTextAsync("False");
    }

    [TestMethod]
    public async Task DropDirection_TopAndBottomStaysAboveOrBelowAndAllMayOpenBeside()
    {
        await Page.SetViewportSizeAsync(1000, 320);
        await OpenAsync("/e2e/menu-button/drop");

        var menu = await OpenMenuAsync("mnb-drop-topbottom");
        var position = await CalloutOf(menu).GetAttributeAsync("data-bit-cal-pos");
        Assert.IsTrue(position is "below" or "above", $"TopAndBottom opens above or below, not {position}.");
        Assert.IsTrue(await CalloutOf(menu).EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The menu taller than the room scrolls.");
        await Page.Keyboard.PressAsync("Escape");

        menu = await OpenMenuAsync("mnb-drop-all");
        await Expect(CalloutOf(menu)).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        await SettledAsync(CalloutOf(menu));
        var button = await Root("mnb-drop-all").BoxAsync();
        var callout = await CalloutOf(menu).BoxAsync();
        Assert.IsTrue(callout.X >= button.X + button.Width - 1, "All opens the menu beside the button when neither side has the room.");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionsUnlessForceAnimationIsSet()
    {
        await OpenAsync(ItemsApi);

        var full = ButtonsE2EHelpers.ParseSeconds(await Root("motion-default").StyleAsync("transition-duration"));
        Assert.IsTrue(full >= 0.05);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.IsTrue(ButtonsE2EHelpers.ParseSeconds(await Root("motion-default").StyleAsync("transition-duration")) < 0.001);
        Assert.IsTrue(ButtonsE2EHelpers.ParseSeconds(await Opb("motion-default").Locator(".bit-mnb-chv").StyleAsync("transition-duration")) < 0.001);
        Assert.AreEqual(full, ButtonsE2EHelpers.ParseSeconds(await Root("motion-forced").StyleAsync("transition-duration")), 0.0001);

        var menu = await OpenMenuAsync("motion-forced");
        await Expect(CalloutOf(menu)).ToHaveClassAsync(HasClass("bit-fam"));
    }

    [TestMethod]
    public async Task ForcedColors_Active_MarksTheToggledHalfAndTheOpenChevronWithHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(ItemsApi);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        var highlight = await Page.ResolveColorAsync("Highlight");
        await Expect(Opb("mnb-toggle-default")).ToHaveCSSAsync("background-color", highlight);
        await Expect(Root("mnb-variant-dis-Fill")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("GrayText"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Prerender_ThePagesHtml_ContainsTheMenuButtons(string route)
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<div id=""mnb-basic"" [^>]*class=""[^""]*\bbit-mnb\b"));
        StringAssert.Contains(html, "id=\"mnb-basic-opb\"");
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }
}
