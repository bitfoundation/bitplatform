using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Buttons;

/// <summary>
/// The end-to-end tests of BitButtonGroup through its three APIs - Items (/e2e/button-group), custom items mapped by
/// NameSelectors (/e2e/button-group/custom) and BitButtonGroupOption children (/e2e/button-group/options) - whose pages
/// render the item-level sections with the same ids and data, so every item-level test runs once per API: the item
/// properties (text, icons by name and from external libraries, IconOnly, ReversedIcon, OnText/OffText,
/// OnIcon/OffIcon, OnTitle/OffTitle, Title, AriaLabel, Badge, IsLoading, IsDisabled, Href/Target/Rel, Template,
/// Class, Style, Key, OnClick), every Variant with its disabled item and disabled group, the Single selection mode
/// (Toggle, the two-way bound ToggleKey, DefaultToggleKey, FixedToggle, OnToggleChange), the Multiple mode
/// (ToggleKeys, DefaultToggleKeys, MaxToggles and its runtime tightening, FixedToggle), the selection indicator,
/// ItemTemplate, OnItemClick, the roving tabindex and the radiogroup/toolbar keyboard (arrows, Home, End, wrapping,
/// SelectOnFocus, DisabledInteractive, Navigable, Vertical), adding and removing items at runtime and unmounting.
/// The group-level parameters - FullWidth, Justified, Detached, Gap, Rounded, Overflow, Disabled and the disabled
/// contract, BitParams, every Color and Size, Style/Class/Styles/Classes, the CSS variables and their variant twins,
/// RTL, Visibility, reduced motion, roles, TabIndex, FocusAsync, AutoFocus, prerendering - are the same for every API
/// and run against the Items page.
/// </summary>
[TestClass]
public class BitButtonGroupE2ETests : E2ETestBase
{
    private const string ItemsApi = "/e2e/button-group";
    private const string CustomApi = "/e2e/button-group/custom";
    private const string OptionsApi = "/e2e/button-group/options";
    private const string Transparent = "rgba(0, 0, 0, 0)";

    private ILocator Group(string id) => Page.Locator($"#{id}");

    private ILocator Items(string groupId) => Page.Locator($"#{groupId} .bit-btg-itm");

    private ILocator ItemAt(string groupId, int index) => Items(groupId).Nth(index);

    private ILocator Text(string id) => Page.Locator($"#{id}");

    private Task<string> Token(string token) => Page.ResolveColorAsync($"var(--bit-clr-{token})");

    // ---------- content (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Basic_ClickingAButton_RaisesOnItemClickWithThatItem(string route)
    {
        await OpenAsync(route);

        var group = Group("grp-basic");
        await Expect(group).ToHaveAttributeAsync("role", "toolbar");
        await Expect(group).ToHaveAttributeAsync("aria-orientation", "horizontal");
        await Expect(group).ToHaveClassAsync(HasClass("bit-btg-fil"));
        await Expect(Items("grp-basic")).ToHaveCountAsync(3);
        await Expect(Items("grp-basic").Locator(".bit-btg-btx")).ToHaveTextAsync(["Add", "Edit", "Delete"]);
        await Expect(ItemAt("grp-basic", 0)).ToHaveClassAsync(HasClass("bit-btg-fst"));
        await Expect(ItemAt("grp-basic", 2)).ToHaveClassAsync(HasClass("bit-btg-lst"));
        await Expect(ItemAt("grp-basic", 0)).ToHaveAttributeAsync("type", "button");

        await ItemAt("grp-basic", 1).ClickAsync();
        await Expect(Text("basic-clicked")).ToHaveTextAsync("Edit");

        await ItemAt("grp-basic", 2).ClickAsync();
        await Expect(Text("basic-clicked")).ToHaveTextAsync("Delete");
    }

    [TestMethod]
    [DataRow(ItemsApi, "Fill")]
    [DataRow(ItemsApi, "Outline")]
    [DataRow(ItemsApi, "Text")]
    [DataRow(CustomApi, "Fill")]
    [DataRow(CustomApi, "Outline")]
    [DataRow(CustomApi, "Text")]
    [DataRow(OptionsApi, "Fill")]
    [DataRow(OptionsApi, "Outline")]
    [DataRow(OptionsApi, "Text")]
    public async Task Variant_EachValue_PaintsTheGroupItsItemsAndADisabledItem(string route, string variant)
    {
        await OpenAsync(route);

        var groupClass = variant switch { "Fill" => "bit-btg-fil", "Outline" => "bit-btg-otl", _ => "bit-btg-txt" };
        var itemClass = variant switch { "Fill" => "bit-btg-ifl", "Outline" => "bit-btg-iol", _ => "bit-btg-itx" };
        var primary = await Token("pri");

        var group = Group($"grp-variant-{variant}");
        var item = ItemAt($"grp-variant-{variant}", 0);
        await Expect(group).ToHaveClassAsync(HasClass(groupClass));
        await Expect(item).ToHaveClassAsync(HasClass(itemClass));
        await Expect(item).ToHaveCSSAsync("background-color", variant == "Fill" ? primary : Transparent);
        await Expect(item).ToHaveCSSAsync("color", variant == "Fill" ? await Token("pri-text") : primary);
        await Expect(group).ToHaveCSSAsync("border-top-color", variant == "Text" ? Transparent : primary);

        var disabledItem = ItemAt($"grp-variant-disitem-{variant}", 1);
        await Expect(disabledItem).ToBeDisabledAsync();
        await Expect(disabledItem).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(disabledItem).ToHaveCSSAsync("color", await Token("pri-dis-text"));
        await Expect(disabledItem).ToHaveCSSAsync("background-color", variant == "Fill" ? await Token("pri-dis") : Transparent);

        var disabledGroup = Group($"grp-variant-dis-{variant}");
        await Expect(disabledGroup).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(disabledGroup).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Items($"grp-variant-dis-{variant}").First).ToBeDisabledAsync();
        await Expect(disabledGroup).ToHaveCSSAsync("pointer-events", "none");

        await item.HoverAsync();
        await Expect(item).ToHaveCSSAsync("background-color", await Token("pri-hover"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Icons_ByNameIconOnlyAndReversed_RenderAndNameTheButtons(string route)
    {
        await OpenAsync(route);

        var icon = ItemAt("grp-icon", 0).Locator("i.bit-btg-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Add"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsTrue((await icon.BoxAsync()).X < (await ItemAt("grp-icon", 0).Locator(".bit-btg-btx").BoxAsync()).X);

        await Expect(Group("grp-icononly")).ToHaveClassAsync(HasClass("bit-btg-ion"));
        await Expect(Group("grp-icononly").Locator(".bit-btg-btx")).ToHaveCountAsync(0);
        await Expect(ItemAt("grp-icononly", 1)).ToHaveAttributeAsync("aria-label", "Edit");
        await Expect(ItemAt("grp-icononly", 1)).ToHaveAccessibleNameAsync("Edit");

        await Expect(ItemAt("grp-onlyicon", 2)).ToHaveAccessibleNameAsync("Delete");
        await Expect(Group("grp-onlyicon").Locator(".bit-btg-btx")).ToHaveCountAsync(0);

        var reversed = ItemAt("grp-reversed", 0);
        await Expect(reversed).ToHaveClassAsync(HasClass("bit-btg-rvi"));
        await Expect(reversed).ToHaveCSSAsync("flex-direction", "row-reverse");
        Assert.IsTrue((await reversed.Locator(".bit-btg-ico").BoxAsync()).X > (await reversed.Locator(".bit-btg-btx").BoxAsync()).X);
        await Expect(ItemAt("grp-reversed", 2)).Not.ToHaveClassAsync(HasClass("bit-btg-rvi"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Icons_FromExternalLibrariesPerState_FollowTheToggle(string route)
    {
        await OpenAsync(route);

        var home = ItemAt("grp-ext-icons", 0).Locator(".bit-btg-ico");
        await Expect(home).ToHaveClassAsync(HasClass("bi-house"));
        await ItemAt("grp-ext-icons", 0).ClickAsync();
        await Expect(home).ToHaveClassAsync(HasClass("bi-house-fill"));

        var gear = ItemAt("grp-ext-icons", 1).Locator(".bit-btg-ico");
        await Expect(gear).ToHaveClassAsync(HasClass("fa-solid"));
        await Expect(gear).ToHaveClassAsync(HasClass("fa-gear"));
    }

    // ---------- single selection (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Toggle_ClickingAnItem_TogglesItSwapsItsTextIconAndTitleAndRaisesOnToggleChange(string route)
    {
        await OpenAsync(route);

        var group = Group("grp-toggle");
        var play = ItemAt("grp-toggle", 1);

        await Expect(group).ToHaveAttributeAsync("role", "radiogroup");
        await Expect(play).ToHaveAttributeAsync("role", "radio");
        await Expect(play).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(play.Locator(".bit-btg-btx")).ToHaveTextAsync("Play");
        await Expect(play.Locator(".bit-btg-ico")).ToHaveClassAsync(HasClass("bit-icon--Play"));

        await play.ClickAsync();

        await Expect(play).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(play).ToHaveClassAsync(HasClass("bit-btg-chk"));
        await Expect(play.Locator(".bit-btg-btx")).ToHaveTextAsync("Pause");
        await Expect(play.Locator(".bit-btg-ico")).ToHaveClassAsync(HasClass("bit-icon--Pause"));
        await Expect(Text("toggle-changes")).ToHaveTextAsync("play");
        await Page.Mouse.MoveAsync(0, 0);
        await Expect(play).ToHaveCSSAsync("background-color", await Token("pri-dark"));

        var back = ItemAt("grp-toggle", 0);
        await Expect(back).ToHaveAttributeAsync("title", "Rewind");
        await back.ClickAsync();
        await Expect(back).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(back).ToHaveAttributeAsync("title", "Slow down");
        await Expect(play).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Text("toggle-changes")).ToHaveTextAsync("play,back");

        await back.ClickAsync();
        await Expect(back).ToHaveAttributeAsync("aria-checked", "false");
        await Expect(Text("toggle-changes")).ToHaveTextAsync("play,back,back");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task ToggleKey_BoundTwoWay_FollowsTheClicksAndThePage(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-toggle-bind", 0).ClickAsync();
        await Expect(Text("toggle-key")).ToHaveTextAsync("back");

        await Page.Locator("#btn-toggle-key-forward").ClickAsync();
        await Expect(ItemAt("grp-toggle-bind", 2)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(ItemAt("grp-toggle-bind", 0)).ToHaveAttributeAsync("aria-checked", "false");

        await Page.Locator("#btn-toggle-key-clear").ClickAsync();
        await Expect(Text("toggle-key")).ToHaveTextAsync("null");
        await Expect(Group("grp-toggle-bind").Locator(".bit-btg-chk")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task DefaultToggleKeyAndFixedToggle_SeedTheSelectionAndKeepOneToggled(string route)
    {
        await OpenAsync(route);

        await Expect(ItemAt("grp-toggle-default", 2)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(ItemAt("grp-toggle-default", 2).Locator(".bit-btg-btx")).ToHaveTextAsync("Forward (2X)");

        var play = ItemAt("grp-toggle-fixed", 1);
        await Expect(play).ToHaveAttributeAsync("aria-checked", "true");
        await play.ClickAsync();
        await Expect(play).ToHaveAttributeAsync("aria-checked", "true");

        await ItemAt("grp-toggle-fixed", 0).ClickAsync();
        await Expect(ItemAt("grp-toggle-fixed", 0)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(play).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    public async Task Toggle_OnCustomItems_WritesTheStateIntoTheIsToggledProperty()
    {
        await OpenAsync(CustomApi);

        await ItemAt("grp-toggle", 2).ClickAsync();
        await Expect(Text("toggle-selected")).ToHaveTextAsync("forward");
        await ItemAt("grp-toggle", 0).ClickAsync();
        await Expect(Text("toggle-selected")).ToHaveTextAsync("back");
    }

    // ---------- multiple selection (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Multiple_ClickingItems_TogglesEachOnItsOwnAndBindsTheKeys(string route)
    {
        await OpenAsync(route);

        await Expect(Group("grp-multi")).ToHaveAttributeAsync("role", "toolbar");
        var bold = ItemAt("grp-multi", 0);
        var italic = ItemAt("grp-multi", 1);
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");

        await bold.ClickAsync();
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Text("multi-keys")).ToHaveTextAsync("bold");

        await italic.ClickAsync();
        await Expect(Text("multi-keys")).ToHaveTextAsync("bold,italic");

        await bold.ClickAsync();
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(Text("multi-keys")).ToHaveTextAsync("italic");
        await Expect(Text("multi-changes")).ToHaveTextAsync("3");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task ToggleKeys_SetByThePage_KeepOnlyTheKeysTheGroupHoldsAndWriteThemBack(string route)
    {
        await OpenAsync(route);

        await Page.Locator("#btn-multi-set").ClickAsync();

        await Expect(Text("multi-keys")).ToHaveTextAsync("italic");
        await Expect(ItemAt("grp-multi", 1)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(ItemAt("grp-multi", 0)).ToHaveAttributeAsync("aria-pressed", "false");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task MaxToggles_Reached_TakesTheRestOutOfReachUntilOneIsUntoggled(string route)
    {
        await OpenAsync(route);

        var bold = ItemAt("grp-multi-max", 0);
        var italic = ItemAt("grp-multi-max", 1);
        var underline = ItemAt("grp-multi-max", 2);

        await bold.ClickAsync();
        await italic.ClickAsync();
        await Expect(Text("multi-max-keys")).ToHaveTextAsync("bold,italic");
        await Expect(underline).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(underline).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);

        await underline.ClickAsync(new() { Force = true });
        await Expect(underline).ToHaveAttributeAsync("aria-pressed", "false");

        await bold.ClickAsync();
        await Expect(underline).Not.ToHaveAttributeAsync("aria-disabled", ButtonsE2EHelpers.AnyValue);
        await underline.ClickAsync();
        await Expect(Text("multi-max-keys")).ToHaveTextAsync("italic,underline");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task MaxToggles_TightenedAtRuntime_CutsTheSelectionToTheFirstItems(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-multi-max", 1).ClickAsync();
        await ItemAt("grp-multi-max", 0).ClickAsync();
        await Expect(Text("multi-max-keys")).ToHaveTextAsync(new Regex("^(bold,italic|italic,bold)$"));

        await Page.Locator("#btn-max-1").ClickAsync();

        await Expect(Text("multi-max-keys")).ToHaveTextAsync("bold");
        await Expect(ItemAt("grp-multi-max", 1)).ToHaveAttributeAsync("aria-pressed", "false");
        await Expect(ItemAt("grp-multi-max", 1)).ToHaveAttributeAsync("aria-disabled", "true");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task DefaultToggleKeys_SeedTheSelectionWithoutRaisingOnToggleChangeInEitherMode(string route)
    {
        await OpenAsync(route);

        // what the group starts with is a state, not a change the user made: neither mode reports it
        await Expect(ItemAt("grp-toggle-default", 2)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(ItemAt("grp-multi-fixed", 0)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Text("toggle-default-changes")).ToHaveTextAsync("0");
        await Expect(Text("multi-fixed-changes")).ToHaveTextAsync("0");

        // the first click is the first change, in both modes
        await ItemAt("grp-toggle-default", 0).ClickAsync();
        await Expect(ItemAt("grp-toggle-default", 0)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(Text("toggle-default-changes")).ToHaveTextAsync("1");

        await ItemAt("grp-multi-fixed", 1).ClickAsync();
        await Expect(ItemAt("grp-multi-fixed", 1)).ToHaveAttributeAsync("aria-pressed", "true");
        await Expect(Text("multi-fixed-changes")).ToHaveTextAsync("1");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task MultipleFixedToggle_NeverUntogglesTheLastItem(string route)
    {
        await OpenAsync(route);

        var bold = ItemAt("grp-multi-fixed", 0);
        var italic = ItemAt("grp-multi-fixed", 1);

        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");
        await bold.ClickAsync();
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "true");

        await italic.ClickAsync();
        await Expect(italic).ToHaveAttributeAsync("aria-pressed", "true");
        await bold.ClickAsync();
        await Expect(bold).ToHaveAttributeAsync("aria-pressed", "false");
        await italic.ClickAsync();
        await Expect(italic).ToHaveAttributeAsync("aria-pressed", "true");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task SelectionIndicator_ShowsTheCheckMarkOnTheToggledItemAndReservesItsSpace(string route)
    {
        await OpenAsync(route);

        async Task<string> MarkOpacity(int index) => await ItemAt("grp-indicator", index).Locator(".bit-btg-sin")
            .EvaluateAsync<string>("el => getComputedStyle(el, '::after').opacity");

        await Expect(Group("grp-indicator").Locator(".bit-btg-sin")).ToHaveCountAsync(3);
        await Expect(ItemAt("grp-indicator", 0).Locator(".bit-btg-sin")).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.AreEqual("1", await MarkOpacity(1));
        Assert.AreEqual("0", await MarkOpacity(0));

        var width = (await ItemAt("grp-indicator", 0).BoxAsync()).Width;
        await ItemAt("grp-indicator", 0).ClickAsync();
        await Expect(ItemAt("grp-indicator", 0)).ToHaveClassAsync(HasClass("bit-btg-chk"));
        Assert.AreEqual("1", await MarkOpacity(0));
        ButtonsE2EHelpers.AreClose(width, (await ItemAt("grp-indicator", 0).BoxAsync()).Width, 0.5, "the indicator's space is reserved");

        await Expect(Group("grp-indicator-none").Locator(".bit-btg-sin")).ToHaveCountAsync(0);
    }

    // ---------- events, loading, badges, links, templates, titles (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Events_ClickingItems_RaisesOnItemClickAndTheItemsOwnOnClick(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-events", 0).ClickAsync();
        await Expect(Text("events-own-clicks")).ToHaveTextAsync("1");
        await ItemAt("grp-events", 0).ClickAsync();
        await Expect(Text("events-own-clicks")).ToHaveTextAsync("2");
        await ItemAt("grp-events", 2).ClickAsync();
        await Expect(Text("events-own-clicks")).ToHaveTextAsync("1");
        await ItemAt("grp-events", 1).ClickAsync();
        await Expect(Text("events-own-clicks")).ToHaveTextAsync("0");
        await Expect(Text("events-item-clicks")).ToHaveTextAsync("increase,increase,decrease,reset");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Loading_AnItem_ShowsASpinnerInPlaceOfTheIconAndRefusesTheClick(string route)
    {
        await OpenAsync(route);

        var save = ItemAt("grp-loading", 0);
        await Expect(save).ToHaveClassAsync(HasClass("bit-btg-ldg"));
        await Expect(save).ToHaveAttributeAsync("aria-busy", "true");
        await Expect(save).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(save.Locator(".bit-btg-spn")).ToBeVisibleAsync();
        await Expect(save.Locator(".bit-btg-ico")).ToHaveCountAsync(0);
        await Expect(save.Locator(".bit-btg-btx")).ToHaveTextAsync("Save");
        await Expect(save).ToHaveCSSAsync("pointer-events", "none");

        await save.DispatchEventAsync("click");
        await ItemAt("grp-loading", 1).ClickAsync();
        await Expect(Text("loading-clicks")).ToHaveTextAsync("sync");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Badge_OnItems_IsRenderedBesideTheTextAndJoinsTheNameOfAnIconOnlyButton(string route)
    {
        await OpenAsync(route);

        await Expect(Group("grp-badge").Locator(".bit-btg-bdg")).ToHaveTextAsync(["3", "12"]);
        await Expect(ItemAt("grp-badge", 0)).ToHaveAccessibleNameAsync("Inbox 3");

        await Expect(Group("grp-badge-icononly").Locator(".bit-btg-btx")).ToHaveCountAsync(0);
        await Expect(ItemAt("grp-badge-icononly", 0)).ToHaveAttributeAsync("aria-label", "Inbox 3");
        await Expect(ItemAt("grp-badge-icononly", 2)).ToHaveAttributeAsync("aria-label", "Sent");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Links_RenderAnchorsWithTargetAndRelAndNavigate(string route)
    {
        await OpenAsync(route);

        var home = ItemAt("grp-links", 0);
        Assert.AreEqual("A", await home.EvaluateAsync<string>("el => el.tagName"));
        await Expect(home).ToHaveAttributeAsync("href", "/");
        await Expect(home).ToHaveAttributeAsync("target", "_blank");
        await Expect(home).ToHaveAttributeAsync("rel", "noopener");
        await Expect(ItemAt("grp-links", 1)).ToHaveAttributeAsync("rel", "nofollow noopener");
        await Expect(ItemAt("grp-links", 2)).Not.ToHaveAttributeAsync("rel", ButtonsE2EHelpers.AnyValue);

        await ItemAt("grp-links", 2).ClickAsync();
        await Expect(Page).ToHaveURLAsync(new Regex("#links$"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Links_InTheSingleMode_AreRadiosThatSpaceSelectsWithoutNavigating(string route)
    {
        await OpenAsync(route);

        var url = Page.Url;
        var home = ItemAt("grp-links-toggle", 0);
        await Expect(home).ToHaveAttributeAsync("role", "radio");
        await Expect(home).ToHaveAttributeAsync("aria-checked", "false");

        await home.FocusAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("links-key")).ToHaveTextAsync("home");
        await Expect(home).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(Text("links-key")).ToHaveTextAsync("docs");
        await Expect(ItemAt("grp-links-toggle", 1)).ToBeFocusedAsync();
        Assert.AreEqual(url, Page.Url, "Selecting a radio link does not navigate.");
        Assert.AreEqual(1, Page.Context.Pages.Count, "Selecting a radio link opens no tab.");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Templates_ItemTemplateOrAnItemsOwn_ReplaceTheContent(string route)
    {
        await OpenAsync(route);

        await Expect(ItemAt("grp-template", 0).Locator(".tpl-own")).ToHaveTextAsync("Custom template");
        await Expect(ItemAt("grp-template", 0).Locator(".bit-btg-btx")).ToHaveCountAsync(0);
        await Expect(ItemAt("grp-template", 1).Locator(".bit-btg-btx")).ToHaveTextAsync("Plain");

        await Expect(Group("grp-itemtemplate").Locator(".tpl-item")).ToHaveTextAsync(["Add", "Edit", "Delete"]);
        await Expect(Group("grp-itemtemplate").Locator(".bit-btg-btx")).ToHaveCountAsync(0);
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Titles_PerItemAndPerState_FillTheTooltips(string route)
    {
        await OpenAsync(route);

        await Expect(ItemAt("grp-titles", 0)).ToHaveAttributeAsync("title", "Bold (Ctrl+B)");
        await Expect(ItemAt("grp-titles", 1)).ToHaveAttributeAsync("title", "Italic (Ctrl+I)");
        await Expect(ItemAt("grp-titles", 0)).ToHaveAccessibleNameAsync("Bold");

        var mute = ItemAt("grp-toggle-titles", 0);
        await Expect(mute).ToHaveAttributeAsync("title", "Mute");
        await mute.ClickAsync();
        await Expect(mute).ToHaveAttributeAsync("title", "Unmute");
        await Expect(ItemAt("grp-toggle-titles", 1)).ToHaveAttributeAsync("title", "Camera");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task ItemClassAndStyle_ReachTheirButton(string route)
    {
        await OpenAsync(route);

        await Expect(ItemAt("grp-item-styles", 0)).ToHaveClassAsync(HasClass("item-own-class"));
        await Expect(ItemAt("grp-item-styles", 1)).ToHaveCSSAsync("color", "rgb(255, 0, 255)");
    }

    // ---------- keyboard (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Radiogroup_ArrowKeysHomeAndEnd_MoveTheFocusSelectAndWrap(string route)
    {
        await OpenAsync(route);

        await Expect(Group("grp-radio")).ToHaveAttributeAsync("aria-label", "Text alignment");
        await Expect(ItemAt("grp-radio", 0)).ToHaveAttributeAsync("tabindex", "0");
        await Expect(ItemAt("grp-radio", 1)).ToHaveAttributeAsync("tabindex", "-1");

        await Page.Locator("#a11y-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(ItemAt("grp-radio", 0)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-radio", 1)).ToBeFocusedAsync();
        await Expect(Text("radio-key")).ToHaveTextAsync("center");
        await Expect(ItemAt("grp-radio", 1)).ToHaveAttributeAsync("tabindex", "0");

        await Page.Keyboard.PressAsync("End");
        await Expect(ItemAt("grp-radio", 2)).ToBeFocusedAsync();
        await Expect(Text("radio-key")).ToHaveTextAsync("right");

        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-radio", 0)).ToBeFocusedAsync();
        await Expect(Text("radio-key")).ToHaveTextAsync("left");

        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(ItemAt("grp-radio", 2)).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Home");
        await Expect(ItemAt("grp-radio", 0)).ToBeFocusedAsync();
        await Expect(Text("radio-key")).ToHaveTextAsync("left");
        await Expect(ItemAt("grp-radio", 0)).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#a11y-after")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Shift+Tab");
        await Expect(ItemAt("grp-radio", 0)).ToBeFocusedAsync();

        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollX"), "The arrow keys do not scroll the page.");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task SelectOnFocusOff_ArrowsMoveTheFocusAndSpaceCommits(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-no-select-on-focus", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-no-select-on-focus", 1)).ToBeFocusedAsync();
        await Expect(Text("commit-key")).ToHaveTextAsync("null");

        await Page.Keyboard.PressAsync("Space");
        await Expect(Text("commit-key")).ToHaveTextAsync("center");

        // the focus is moved by a round trip through the circuit, so the next key waits for it to land
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-no-select-on-focus", 2)).ToBeFocusedAsync();
        await Expect(Text("commit-key")).ToHaveTextAsync("center");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Text("commit-key")).ToHaveTextAsync("right");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Toolbar_ArrowKeys_SkipADisabledItemUnlessDisabledInteractiveKeepsIt(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-toolbar", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-toolbar", 2)).ToBeFocusedAsync();

        var disabled = ItemAt("grp-interactive", 1);
        await Expect(disabled).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(disabled).ToHaveAttributeAsync("aria-disabled", "true");

        await ItemAt("grp-interactive", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(disabled).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await disabled.ClickAsync(new() { Force = true });
        await ItemAt("grp-interactive", 2).ClickAsync();
        await Expect(Text("interactive-clicks")).ToHaveTextAsync("delete");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task NotNavigable_EveryButtonIsATabStopAndTheArrowsDoNothing(string route)
    {
        await OpenAsync(route);

        var group = Group("grp-not-navigable");
        await Expect(group).ToHaveAttributeAsync("role", "group");
        await Expect(group).Not.ToHaveAttributeAsync("aria-orientation", ButtonsE2EHelpers.AnyValue);
        await Expect(group).Not.ToHaveClassAsync(HasClass("bit-btg-nav"));
        await Expect(Items("grp-not-navigable").Nth(1)).ToHaveAttributeAsync("tabindex", "0");

        await ItemAt("grp-not-navigable", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-not-navigable", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(ItemAt("grp-not-navigable", 1)).ToBeFocusedAsync();
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Vertical_StacksTheButtonsAndNavigatesWithUpAndDown(string route)
    {
        await OpenAsync(route);

        var group = Group("grp-vertical");
        await Expect(group).ToHaveClassAsync(HasClass("bit-btg-vrt"));
        await Expect(group).ToHaveCSSAsync("flex-direction", "column");
        await Expect(group).ToHaveAttributeAsync("aria-orientation", "vertical");
        var first = await ItemAt("grp-vertical", 0).BoxAsync();
        var second = await ItemAt("grp-vertical", 1).BoxAsync();
        Assert.IsTrue(second.Y >= first.Y + first.Height - 1, "The buttons are stacked.");
        ButtonsE2EHelpers.AreClose(first.X, second.X, 0.5, "The buttons share a column");

        await ItemAt("grp-vertical", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt("grp-vertical", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("grp-vertical", 1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(ItemAt("grp-vertical", 0)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowUp");
        await Expect(ItemAt("grp-vertical", 2)).ToBeFocusedAsync();

        await ItemAt("grp-vertical-toggle", 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(ItemAt("grp-vertical-toggle", 1)).ToHaveAttributeAsync("aria-checked", "true");
    }

    // ---------- runtime and lifecycle (every API) ----------

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Items_AddedAndRemovedAtRuntime_ReRenderTheGroupAndKeepTheSelection(string route)
    {
        await OpenAsync(route);

        await Expect(Items("grp-dynamic").Locator(".bit-btg-btx")).ToHaveTextAsync(["A", "B"]);
        await Expect(ItemAt("grp-dynamic", 1)).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Locator("#btn-add-item").ClickAsync();
        await Expect(Items("grp-dynamic").Locator(".bit-btg-btx")).ToHaveTextAsync(["A", "B", "New 1"]);
        await Expect(ItemAt("grp-dynamic", 2)).ToHaveClassAsync(HasClass("bit-btg-lst"));
        await Expect(ItemAt("grp-dynamic", 1)).Not.ToHaveClassAsync(HasClass("bit-btg-lst"));
        await Expect(ItemAt("grp-dynamic", 1)).ToHaveAttributeAsync("aria-checked", "true");

        await Page.Locator("#btn-remove-item").ClickAsync();
        await Expect(Items("grp-dynamic").Locator(".bit-btg-btx")).ToHaveTextAsync(["B", "New 1"]);
        await Expect(ItemAt("grp-dynamic", 0)).ToHaveClassAsync(HasClass("bit-btg-fst"));
        await Expect(ItemAt("grp-dynamic", 0)).ToHaveAttributeAsync("aria-checked", "true");

        await ItemAt("grp-dynamic", 1).ClickAsync();
        await Expect(ItemAt("grp-dynamic", 1)).ToHaveAttributeAsync("aria-checked", "true");
        await Expect(ItemAt("grp-dynamic", 0)).ToHaveAttributeAsync("aria-checked", "false");
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Unmount_TheGroup_LeavesNoErrorBehindAndRemountsWithItsDefaults(string route)
    {
        await OpenAsync(route);

        await ItemAt("grp-mounted", 2).ClickAsync();
        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(Group("grp-mounted")).ToHaveCountAsync(0);

        await Page.Locator("#btn-unmount").ClickAsync();
        await Expect(ItemAt("grp-mounted", 0)).ToHaveAttributeAsync("aria-checked", "true");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    // ---------- group level (Items page) ----------

    [TestMethod]
    public async Task FullWidthAndJustified_StretchTheGroupAndEvenTheButtons()
    {
        await OpenAsync(ItemsApi);

        await Expect(Group("grp-fullwidth")).ToHaveClassAsync(HasClass("bit-btg-flw"));
        ButtonsE2EHelpers.AreClose(500, (await Group("grp-fullwidth").BoxAsync()).Width, 0.5, "full width");
        Assert.IsTrue((await Group("grp-not-fullwidth").BoxAsync()).Width < 400);

        await Expect(Group("grp-justified")).ToHaveClassAsync(HasClass("bit-btg-jst"));
        var widths = new List<double>();
        for (var i = 0; i < 3; i++) widths.Add((await ItemAt("grp-justified", i).BoxAsync()).Width);
        Assert.IsTrue(widths.Max() - widths.Min() <= 1.5, $"Justified buttons share the width evenly ({string.Join(", ", widths)}).");

        var uneven = new List<double>();
        for (var i = 0; i < 3; i++) uneven.Add((await ItemAt("grp-unjustified", i).BoxAsync()).Width);
        Assert.IsTrue(uneven.Max() - uneven.Min() > 20, "Without Justified the buttons follow their content.");
    }

    [TestMethod]
    public async Task DetachedGapAndRounded_SeparateAndRoundTheButtons()
    {
        await OpenAsync(ItemsApi);

        var full = await Page.ResolvePxAsync("var(--bit-shp-radius-full)");
        var buttonRadius = await Page.ResolvePxAsync("var(--bit-shp-radius-button)");

        var detached = Group("grp-detached");
        await Expect(detached).ToHaveClassAsync(HasClass("bit-btg-dtc"));
        await Expect(detached).ToHaveCSSAsync("border-top-width", "0px");
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.Spacing(1)), await detached.PxAsync("column-gap"), 0.01);
        Assert.AreEqual(buttonRadius, await ItemAt("grp-detached", 1).PxAsync("border-top-left-radius"), "each detached button has its own corners");
        var first = await ItemAt("grp-detached", 0).BoxAsync();
        var second = await ItemAt("grp-detached", 1).BoxAsync();
        Assert.IsTrue(second.X - (first.X + first.Width) > 2, "Detached buttons are apart.");

        await Expect(Group("grp-detached-gap")).ToHaveAttributeAsync("style", new Regex("--bit-ButtonGroup-gap:\\s*24px"));
        await Expect(Group("grp-detached-gap")).ToHaveCSSAsync("column-gap", "24px");

        await Expect(Group("grp-rounded")).ToHaveClassAsync(HasClass("bit-btg-rnd"));
        Assert.AreEqual(full, await Group("grp-rounded").PxAsync("border-top-left-radius"));
        Assert.AreEqual(full, await ItemAt("grp-rounded-detached", 0).PxAsync("border-top-left-radius"));
        Assert.AreEqual(0, await Group("grp-rounded-detached").PxAsync("border-top-left-radius"));
        Assert.AreEqual(buttonRadius, await Group("grp-rounded-vertical").PxAsync("border-top-left-radius"), "a vertical group is not rounded as a whole");
    }

    [TestMethod]
    [DataRow("grp-clip", "", "hidden")]
    [DataRow("grp-wrap", "bit-btg-wrp", "hidden")]
    [DataRow("grp-scroll", "bit-btg-scr", "auto")]
    [DataRow("grp-scrollbar", "bit-btg-scb", "auto")]
    public async Task Overflow_EachValue_ClipsWrapsOrScrollsTheButtons(string id, string cssClass, string overflowX)
    {
        await OpenAsync(ItemsApi);

        var group = Group(id);
        if (cssClass.Length > 0) await Expect(group).ToHaveClassAsync(HasClass(cssClass));
        await Expect(group).ToHaveCSSAsync("overflow-x", overflowX);
        ButtonsE2EHelpers.AreClose(220, (await group.BoxAsync()).Width, 0.5, "the group stays in its container");

        switch (id)
        {
            case "grp-wrap":
                await Expect(group).ToHaveCSSAsync("flex-wrap", "wrap");
                Assert.IsTrue((await group.BoxAsync()).Height > (await Group("grp-clip").BoxAsync()).Height * 2, "Wrap breaks the buttons onto more rows.");
                break;
            case "grp-scroll":
            case "grp-scrollbar":
                // Playwright's Firefox computes every scrollbar-width as none (its headless profile hides the
                // scrollbars), so the thin one is only read in the other engines; the scrolling itself is read in all.
                if (id == "grp-scroll" || IsFirefox is false)
                {
                    await Expect(group).ToHaveCSSAsync("scrollbar-width", id == "grp-scroll" ? "none" : "thin");
                }
                await group.EvaluateAsync("el => el.scrollLeft = 100");
                Assert.IsTrue(await group.EvaluateAsync<double>("el => el.scrollLeft") > 0, "The row scrolls.");
                await ItemAt(id, 7).FocusAsync();
                await ItemAt(id, 7).ScrollIntoViewIfNeededAsync();
                await Expect(ItemAt(id, 7)).ToBeInViewportAsync();
                break;
            default:
                Assert.IsTrue(await group.EvaluateAsync<bool>("el => el.scrollWidth > el.clientWidth"), "Clip lets the buttons run past the edge.");
                break;
        }
    }

    [TestMethod]
    public async Task Disabled_TheWholeGroup_RunsNoHandlerUntilItIsEnabledAgain()
    {
        await OpenAsync(ItemsApi);

        var group = Group("grp-disabled");
        var item = ItemAt("grp-disabled", 1);

        await Expect(group).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(group).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(item).ToBeDisabledAsync();
        await Expect(item).ToHaveAttributeAsync("aria-disabled", "true");

        await item.DispatchEventAsync("click");
        await item.DispatchEventAsync("mousedown");
        await group.DispatchEventAsync("click");

        await Page.Locator("#btn-toggle-disabled").ClickAsync();
        await Expect(group).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(item).ToBeEnabledAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Text("disabled-page-events")).ToHaveTextAsync("0");
        await Expect(item).ToHaveAttributeAsync("aria-checked", "false");

        await item.ClickAsync();
        await Expect(Text("disabled-clicks")).ToHaveTextAsync("1");
        await Expect(Text("disabled-page-events")).ToHaveTextAsync("2");
        await Expect(item).ToHaveAttributeAsync("aria-checked", "true");

        var interactive = ItemAt("grp-disabled-interactive", 0);
        await Expect(interactive).Not.ToHaveAttributeAsync("disabled", ButtonsE2EHelpers.AnyValue);
        await Expect(interactive).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(interactive).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task GroupAccessibility_FocusAsyncRolesAndTabIndex()
    {
        await OpenAsync(ItemsApi);

        await Page.Locator("#btn-focus-group").ClickAsync();
        await Expect(ItemAt("grp-focus", 1)).ToBeFocusedAsync();

        await Expect(Group("grp-role-group")).ToHaveAttributeAsync("role", "group");
        await Expect(Group("grp-role-group")).Not.ToHaveAttributeAsync("aria-orientation", ButtonsE2EHelpers.AnyValue);
        await Expect(Group("grp-role-menubar")).ToHaveAttributeAsync("role", "toolbar");

        await Expect(ItemAt("grp-tabindex", 0)).ToHaveAttributeAsync("tabindex", "-1");
        await Expect(ItemAt("grp-tabindex", 1)).ToHaveAttributeAsync("tabindex", "-1");
    }

    [TestMethod]
    public async Task AutoFocus_OnTheFirstRender_FocusesTheToggledButton()
    {
        await OpenAsync("/e2e/button-group/autofocus");

        await Expect(ItemAt("grp-autofocus", 1)).ToHaveAttributeAsync("autofocus", "");
        await Expect(ItemAt("grp-autofocus", 1)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BitParams_AroundGroups_FillsInWhatEachLeftUnset()
    {
        await OpenAsync(ItemsApi);

        foreach (var cssClass in new[] { "bit-btg-otl", "bit-btg-sm", "bit-btg-sec", "bit-btg-rnd" })
        {
            await Expect(Group("params-inherit")).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(Group("params-inherit")).ToHaveCSSAsync("border-top-color", await Token("sec"));
        await Expect(Group("params-own")).ToHaveClassAsync(HasClass("bit-btg-txt"));
        await Expect(Group("params-own")).ToHaveClassAsync(HasClass("bit-btg-sm"));
        await Expect(Group("params-outside")).Not.ToHaveClassAsync(HasClass("bit-btg-sm"));
        await Expect(Group("params-outside")).ToHaveClassAsync(HasClass("bit-btg-fil"));
    }

    // The group's own role table: the semantic roles paint from the shared role tokens, the surface roles from the
    // pairs BitButtonGroup.scss names for them (a surface role's dark is its main color).
    public static IEnumerable<object[]> GroupColorRows =>
    [
        ["Primary", "pri", "pri", "pri-text", "pri-dark", "pri-dis"],
        ["Secondary", "sec", "sec", "sec-text", "sec-dark", "sec-dis"],
        ["Tertiary", "ter", "ter", "ter-text", "ter-dark", "ter-dis"],
        ["Info", "inf", "inf", "inf-text", "inf-dark", "inf-dis"],
        ["Success", "suc", "suc", "suc-text", "suc-dark", "suc-dis"],
        ["Warning", "wrn", "wrn", "wrn-text", "wrn-dark", "wrn-dis"],
        ["SevereWarning", "swr", "swr", "swr-text", "swr-dark", "swr-dis"],
        ["Error", "err", "err", "err-text", "err-dark", "err-dis"],
        ["PrimaryBackground", "pbg", "bg-pri", "fg-pri", "bg-pri", "bg-pri-dis"],
        ["SecondaryBackground", "sbg", "bg-sec", "fg-pri", "bg-sec", "bg-sec-dis"],
        ["TertiaryBackground", "tbg", "bg-ter", "fg-pri", "bg-ter", "bg-ter-dis"],
        ["PrimaryForeground", "pfg", "fg-pri", "bg-pri", "fg-pri", "fg-pri-dis"],
        ["SecondaryForeground", "sfg", "fg-sec", "bg-pri", "fg-sec", "fg-sec-dis"],
        ["TertiaryForeground", "tfg", "fg-ter", "bg-pri", "fg-ter", "fg-ter-dis"],
        ["PrimaryBorder", "pbr", "brd-pri", "fg-pri", "brd-pri", "brd-pri-dis"],
        ["SecondaryBorder", "sbr", "brd-sec", "bg-pri", "brd-sec", "brd-sec-dis"],
        ["TertiaryBorder", "tbr", "brd-ter", "bg-pri", "brd-ter", "brd-ter-dis"],
    ];

    [TestMethod]
    [DynamicData(nameof(GroupColorRows))]
    public async Task Color_EachValue_PaintsTheButtonsTheToggledOneAndADisabledOne(string name, string suffix, string main, string text, string dark, string dis)
    {
        await OpenAsync(ItemsApi);

        var group = Group($"color-{name}");
        await Expect(group).ToHaveClassAsync(HasClass($"bit-btg-{suffix}"));

        var item = ItemAt($"color-{name}", 1);
        await Expect(item).ToHaveCSSAsync("background-color", await Token(main));
        await Expect(item).ToHaveCSSAsync("color", await Token(text));
        await Expect(ItemAt($"color-{name}", 0)).ToHaveCSSAsync("background-color", await Token(dark));

        await Expect(ItemAt($"color-outline-{name}", 0)).ToHaveCSSAsync("color", await Token(main));
        await Expect(Group($"color-outline-{name}")).ToHaveCSSAsync("border-top-color", await Token(main));

        await Expect(ItemAt($"color-dis-{name}", 1)).ToHaveCSSAsync("background-color", await Token(dis));

        await item.HoverAsync();
        await Expect(item).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync($"var(--bit-clr-{main}-hover)"));
    }

    [TestMethod]
    [DataRow("Small", "sm", "xs")]
    [DataRow("Medium", "md", "sm")]
    [DataRow("Large", "lg", "md")]
    public async Task Size_EachValue_SetsTheButtonHeightTypeAndIcon(string size, string suffix, string font)
    {
        await OpenAsync(ItemsApi);

        var height = await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight(suffix));
        var item = ItemAt($"size-{size}", 0);

        await Expect(Group($"size-{size}")).ToHaveClassAsync(HasClass($"bit-btg-{suffix}"));
        Assert.AreEqual(height, await item.PxAsync("min-height"), "min-height");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-tpg-fs-{font})"), await item.PxAsync("font-size"), "font-size");
        Assert.AreEqual(await Page.ResolvePxAsync($"var(--bit-siz-icon-{suffix})"), await item.Locator(".bit-btg-ico").PxAsync("font-size"), "icon size");
        Assert.AreEqual(height, await ItemAt($"size-icon-{size}", 0).PxAsync("min-width"), "icon-only buttons are at least square");
    }

    [TestMethod]
    public async Task Size_Unset_IsTheMediumGroupWithoutAClassOfItsOwn()
    {
        await OpenAsync(ItemsApi);

        await Expect(Group("size-default")).Not.ToHaveClassAsync(HasClass("bit-btg-md"));
        Assert.AreEqual(await ItemAt("size-Medium", 0).PxAsync("min-height"), await ItemAt("size-default", 0).PxAsync("min-height"));
    }

    [TestMethod]
    public async Task StyleClassStylesAndClasses_ReachTheRootAndEveryPart()
    {
        await OpenAsync(ItemsApi);

        await Expect(Group("sc-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Group("sc-style")).ToHaveClassAsync(HasClass("e2e-custom-class"));
        await Expect(Group("sc-attrs")).ToHaveAttributeAsync("data-e2e", "custom-attribute");

        await Expect(Group("sc-styles")).ToHaveCSSAsync("outline-style", "dotted");
        await Expect(ItemAt("sc-styles", 2)).ToHaveCSSAsync("letter-spacing", "2px");
        await Expect(ItemAt("sc-styles", 0).Locator(".bit-btg-bdg")).ToHaveCSSAsync("color", "rgb(255, 0, 0)");
        await Expect(ItemAt("sc-styles", 1).Locator(".bit-btg-ico")).ToHaveCSSAsync("color", "rgb(0, 128, 0)");
        await Expect(ItemAt("sc-styles", 1).Locator(".bit-btg-sin")).ToHaveCSSAsync("width", "21px");
        await Expect(ItemAt("sc-styles", 1).Locator(".bit-btg-btx")).ToHaveCSSAsync("text-decoration-line", "underline");
        await Expect(ItemAt("sc-styles", 0)).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(ItemAt("sc-styles", 1)).Not.ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");

        await Expect(ItemAt("sc-styles-spinner", 0).Locator(".bit-btg-spn")).ToHaveCSSAsync("border-top-color", "rgb(218, 165, 32)");
        await Expect(ItemAt("sc-styles-spinner", 0).Locator(".bit-btg-spn")).ToHaveClassAsync(HasClass("c-spinner"));

        await Expect(Group("sc-classes")).ToHaveClassAsync(HasClass("c-root"));
        await Expect(ItemAt("sc-classes", 1)).ToHaveClassAsync(HasClass("c-button"));
        await Expect(ItemAt("sc-classes", 0)).ToHaveClassAsync(HasClass("c-toggled"));
        await Expect(ItemAt("sc-classes", 1)).Not.ToHaveClassAsync(HasClass("c-toggled"));
        await Expect(ItemAt("sc-classes", 0).Locator(".bit-btg-bdg")).ToHaveClassAsync(HasClass("c-badge"));
        await Expect(ItemAt("sc-classes", 0).Locator(".bit-btg-ico")).ToHaveClassAsync(HasClass("c-icon"));
        await Expect(ItemAt("sc-classes", 0).Locator(".bit-btg-sin")).ToHaveClassAsync(HasClass("c-indicator"));
        await Expect(ItemAt("sc-classes", 0).Locator(".bit-btg-btx")).ToHaveClassAsync(HasClass("c-text"));

        await ItemAt("sc-classes", 1).ClickAsync();
        await Expect(ItemAt("sc-classes", 1)).ToHaveClassAsync(HasClass("c-toggled"));
        await Expect(ItemAt("sc-classes", 0)).Not.ToHaveClassAsync(HasClass("c-toggled"));
    }

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheDefaultsAndLoseToExplicitParameters()
    {
        await OpenAsync(ItemsApi);

        var group = Group("vars-default");
        var plain = ItemAt("vars-default", 1);
        var toggled = ItemAt("vars-default", 0);

        await Expect(plain).ToHaveCSSAsync("background-color", "rgb(10, 20, 30)");
        await Expect(plain).ToHaveCSSAsync("color", "rgb(250, 240, 230)");
        await Expect(group).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 0)");
        await Expect(plain).ToHaveCSSAsync("border-right-color", "rgb(0, 200, 0)");
        await Expect(toggled).ToHaveCSSAsync("background-color", "rgb(0, 0, 150)");
        await Expect(toggled).ToHaveCSSAsync("color", "rgb(255, 255, 0)");
        await Expect(group).ToHaveCSSAsync("border-top-left-radius", "0px");
        await Expect(group).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(plain).ToHaveCSSAsync("min-height", "50px");
        await Expect(plain).ToHaveCSSAsync("padding-left", "21px");
        await Expect(plain).ToHaveCSSAsync("font-size", "19px");
        await Expect(plain).ToHaveCSSAsync("font-weight", "700");
        await Expect(plain).ToHaveCSSAsync("column-gap", "12px");
        await Expect(plain.Locator(".bit-btg-ico")).ToHaveCSSAsync("font-size", "25px");
        await Expect(plain.Locator(".bit-btg-bdg")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(plain.Locator(".bit-btg-bdg")).ToHaveCSSAsync("background-color", "rgb(255, 255, 255)");
        await Expect(plain.Locator(".bit-btg-bdg")).ToHaveCSSAsync("font-size", "9px");

        await Expect(ItemAt("vars-color", 0)).ToHaveCSSAsync("background-color", await Token("err"));
        Assert.AreEqual(await Page.ResolvePxAsync(ButtonsE2EHelpers.ControlHeight("sm")), await ItemAt("vars-size", 0).PxAsync("min-height"));
        await Expect(Group("vars-detached")).ToHaveCSSAsync("column-gap", "17px");
        await Expect(ItemAt("vars-disabled", 1)).ToHaveCSSAsync("background-color", "rgb(77, 77, 77)");

        await plain.HoverAsync();
        await Expect(plain).ToHaveCSSAsync("background-color", "rgb(255, 128, 0)");
    }

    [TestMethod]
    public async Task CssVariables_VariantTwins_RestyleTheirOwnLookOnly()
    {
        await OpenAsync(ItemsApi);

        await Expect(Group("vars-twin-outline")).ToHaveCSSAsync("border-top-color", "rgb(200, 0, 200)");
        await Expect(Group("vars-twin-fill")).Not.ToHaveCSSAsync("border-top-color", "rgb(200, 0, 200)");
        await Expect(ItemAt("vars-twin-text", 0)).ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(ItemAt("vars-twin-fill", 0)).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
        await Expect(ItemAt("vars-twin-outline", 0)).Not.ToHaveCSSAsync("color", "rgb(0, 0, 200)");
    }

    [TestMethod]
    [DataRow("grp-rtl", true)]
    [DataRow("grp-rtl-inherited", false)]
    public async Task Rtl_ExplicitOrInherited_LaysTheButtonsOutRightToLeftAndTurnsTheArrows(string id, bool explicitDir)
    {
        await OpenAsync(ItemsApi);

        if (explicitDir)
        {
            await Expect(Group(id)).ToHaveAttributeAsync("dir", "rtl");
            await Expect(Group(id)).ToHaveClassAsync(HasClass("bit-rtl"));
        }

        Assert.IsTrue((await ItemAt(id, 0).BoxAsync()).X > (await ItemAt(id, 1).BoxAsync()).X, "The first button is on the right.");

        await ItemAt(id, 0).FocusAsync();
        await Page.Keyboard.PressAsync("ArrowLeft");
        await Expect(ItemAt(id, 1)).ToBeFocusedAsync();
        await Expect(ItemAt(id, 1)).ToHaveAttributeAsync("aria-checked", "true");
        await Page.Keyboard.PressAsync("ArrowRight");
        await Expect(ItemAt(id, 0)).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Visibility_HiddenAndCollapsed_HideTheGroup()
    {
        await OpenAsync(ItemsApi);

        await Page.Locator("#btn-vis-hidden").ClickAsync();
        await Expect(Group("grp-visibility")).ToHaveCSSAsync("visibility", "hidden");
        await Page.Locator("#btn-vis-collapsed").ClickAsync();
        await Expect(Group("grp-visibility")).ToHaveCSSAsync("display", "none");
        await Page.Locator("#btn-vis-visible").ClickAsync();
        await Expect(Group("grp-visibility")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task GroupParameters_ChangedAtRuntime_ReRenderTheGroupAndItsButtons()
    {
        await OpenAsync(ItemsApi);

        await Page.Locator("#btn-rerender").ClickAsync();

        var group = Group("grp-rerender");
        foreach (var cssClass in new[] { "bit-btg-otl", "bit-btg-err", "bit-btg-lg", "bit-btg-vrt", "bit-btg-ion" })
        {
            await Expect(group).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(ItemAt("grp-rerender", 0)).ToHaveClassAsync(HasClass("bit-btg-iol"));
        await Expect(group.Locator(".bit-btg-btx")).ToHaveCountAsync(0);
        await Expect(ItemAt("grp-rerender", 0)).ToHaveAttributeAsync("aria-label", "Add");
        await Expect(ItemAt("grp-rerender", 0)).ToHaveCSSAsync("color", await Token("err"));
        await Expect(group).ToHaveAttributeAsync("aria-orientation", "vertical");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_CollapsesTheTransitionsUnlessForceAnimationIsSet()
    {
        await OpenAsync(ItemsApi);

        var full = ButtonsE2EHelpers.ParseSeconds(await ItemAt("motion-default", 0).StyleAsync("transition-duration"));
        Assert.IsTrue(full >= 0.05);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        Assert.IsTrue(ButtonsE2EHelpers.ParseSeconds(await ItemAt("motion-default", 0).StyleAsync("transition-duration")) < 0.001);
        Assert.AreEqual(full, ButtonsE2EHelpers.ParseSeconds(await ItemAt("motion-forced", 0).StyleAsync("transition-duration")), 0.0001);
    }

    [TestMethod]
    public async Task ForcedColors_Active_MarksTheToggledButtonWithHighlight()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit does not emulate forced colors.");

        await OpenAsync(ItemsApi);
        await Page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });

        await Expect(ItemAt("grp-toggle-default", 2)).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("Highlight"));
        await Expect(ItemAt("grp-variant-disitem-Fill", 1)).ToHaveCSSAsync("color", await Page.ResolveColorAsync("GrayText"));
    }

    [TestMethod]
    [DataRow(ItemsApi)]
    [DataRow(CustomApi)]
    [DataRow(OptionsApi)]
    public async Task Prerender_ThePagesHtml_ContainsTheGroups(string route)
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Matches(html, new Regex(@"<div id=""grp-basic"" role=""toolbar"" class=""[^""]*\bbit-btg\b"));
        StringAssert.Contains(html, "<span id=\"status\">Loading</span>");
    }
}
