using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The Responsive mode of BitDropdown on a phone (<c>/e2e/dropdown/responsive</c>, a 390x844 touch viewport): a tap
/// opens a full-height side panel at the end of the screen carrying the label and a close button
/// (ResponsiveCloseButtonAriaLabel), the close button, a pick and a swipe dismiss it, a closed panel is out of the
/// accessibility tree and the tab order (and out of the page once the screen widens), an open one hides with the part of
/// the page it was opened from, an RTL panel slides in from the left, the ComboBox panel brings its own combobox input and add button (ComboBoxAddButtonAriaLabel, the close and add
/// icons by name and by BitIconInfo), Enter in a multi select panel keeps it open, the search box takes the focus inside
/// the panel, the panel's parts take Styles and Classes, a long list ends short of the bottom of the screen and clear of
/// a phone's safe areas and scrolls to its last item, and a dropdown that is not Responsive stays a plain callout.
/// </summary>
[TestClass]
public class BitDropdownResponsiveE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/responsive";

    protected override BrowserNewContextOptions? ContextOptions => new()
    {
        ViewportSize = new() { Width = 390, Height = 844 },
        HasTouch = true,
    };

    private async Task<ILocator> TapOpenAsync(string id)
    {
        await Trigger(id).TapAsync();

        var callout = await CalloutAsync(id);
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await WaitForAnimationsAsync(callout);

        return callout;
    }

    [TestMethod]
    public async Task Responsive_ATapOpensAFullHeightSidePanel_WithTheLabelAndACloseButton()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp");

        await Expect(callout).ToHaveClassAsync(HasClass("bit-drp-res"));
        await Expect(Trigger("dd-resp")).ToHaveAttributeAsync("aria-expanded", "true");

        var box = await BoxAsync(callout);
        Assert.AreEqual(390, box.X + box.Width, 1.5, "The panel ends at the right edge of the screen.");
        Assert.AreEqual(0, box.Y, 1.5);
        Assert.AreEqual(844, box.Height, 1.5, "The panel takes the whole height of the screen.");
        Assert.IsTrue(box.Width < 390, "The panel leaves the page visible beside it.");

        var header = callout.Locator(".bit-drp-rlc");
        await Expect(header).ToBeVisibleAsync();
        await Expect(header.Locator("label")).ToHaveTextAsync("Responsive");

        var close = callout.GetByRole(AriaRole.Button, new() { Name = "Dismiss panel", Exact = true });
        await Expect(close).ToHaveAttributeAsync("title", "Dismiss panel");
        await Expect(close.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        foreach (var part in new[] { "ResponsiveLabelContainer", "ResponsiveLabel", "ResponsiveCloseButton", "ResponsiveCloseIcon" })
        {
            await Expect(callout.Locator($".c-{part}").First).ToHaveAttributeAsync("style", new Regex($"--part-{part}: 1"));
        }
    }

    [TestMethod]
    public async Task Responsive_TheCloseButton_SlidesThePanelOutAndReturnsTheFocus()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp");

        await callout.GetByRole(AriaRole.Button, new() { Name = "Dismiss panel" }).TapAsync();

        await Expect(Trigger("dd-resp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(callout).ToHaveCSSAsync("opacity", "0");
        await Expect(Trigger("dd-resp")).ToBeFocusedAsync();
        await Expect(Page.Locator("#resp-close-count")).ToHaveTextAsync("1");

        await WaitForAnimationsAsync(callout);
        Assert.IsTrue((await BoxAsync(callout)).X >= 389, "The panel slid out past the right edge.");
    }

    [TestMethod]
    public async Task Responsive_AClosedPanel_IsOutOfTheAccessibilityTreeAndTheTabOrder()
    {
        await OpenAsync(Url);

        // Four of the responsive dropdowns on the page keep the default name of the close button, and none of them is
        // open: a closed panel is only out of sight, laid out off the screen for its slide, and must not leave its close
        // button (or its options) for a screen reader - or a test - to find.
        var close = Page.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true });
        await Expect(close).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);

        var callout = await CalloutAsync("dd-resp-rtl");
        await Expect(callout).ToBeHiddenAsync();

        // The closed panel is rendered right after its field, and it is focusable while it is open.
        await Trigger("dd-resp-rtl").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Trigger("dd-resp-search")).ToBeFocusedAsync();

        await TapOpenAsync("dd-resp-rtl");

        await Expect(close).ToHaveCountAsync(1);
        await Expect(callout.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true })).ToBeVisibleAsync();
        await Expect(Options(callout)).ToHaveCountAsync(3);

        await close.TapAsync();

        await Expect(Trigger("dd-resp-rtl")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(callout).ToBeHiddenAsync();
        await Expect(close).ToHaveCountAsync(0);
        await Expect(Page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Responsive_APanelClosedBeforeTheScreenWidens_StaysClosedAndOpensAsAPlainCallout()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-rtl");
        var close = callout.GetByRole(AriaRole.Button, new() { Name = "Close", Exact = true });

        await close.TapAsync();

        await Expect(Trigger("dd-resp-rtl")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(callout).ToHaveCSSAsync("visibility", "hidden");

        // The phone turned to landscape: wide enough for a plain callout, which is display:none while it is closed - the
        // display the panel was opened with is not left behind to hold it in the page, invisible but there.
        await Page.SetViewportSizeAsync(844, 390);

        await Expect(callout).ToHaveCSSAsync("display", "none");
        await Expect(Page.GetByRole(AriaRole.Option)).ToHaveCountAsync(0);

        // Nor is the fade the panel closed with left on the plain callout it opens as there.
        await Trigger("dd-resp-rtl").TapAsync();

        await Expect(Trigger("dd-resp-rtl")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await Expect(Options(callout)).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Responsive_AnOpenPanel_HidesWithThePartOfThePageItWasOpenedFrom()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-rtl");

        // The open panel is moved to the body inside a copy of every element around it, and the copy of a section the
        // page hides hides the panel with it, as that section would where the panel was rendered.
        await Trigger("dd-resp-rtl").EvaluateAsync("el => el.closest('section').style.visibility = 'hidden'");
        await Expect(callout).ToBeHiddenAsync();

        await Trigger("dd-resp-rtl").EvaluateAsync("el => el.closest('section').style.visibility = ''");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Responsive_APick_ClosesThePanel()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp");

        await Option(callout, "Banana").TapAsync();

        await Expect(Page.Locator("#resp-value")).ToHaveTextAsync("f-ban");
        await Expect(Trigger("dd-resp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(TextContainer("dd-resp")).ToHaveTextAsync("Banana");
    }

    [TestMethod]
    public async Task Responsive_SwipingThePanelTowardsItsEdge_ClosesIt()
    {
        if (IsWebKit) Assert.Inconclusive("WebKit has no Touch constructor, so the swipe cannot be synthesized there (and Playwright's touchscreen only taps).");

        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp");
        var calloutId = await callout.GetAttributeAsync("id");

        await Page.EvaluateAsync(@"id => {
            const panel = document.getElementById(id);
            const touch = (x) => new Touch({ identifier: 1, target: panel, clientX: x, clientY: 400, screenX: x, screenY: 400, pageX: x, pageY: 400 });
            const fire = (type, x) => panel.dispatchEvent(new TouchEvent(type, {
                bubbles: true, cancelable: true, composed: true,
                touches: type === 'touchend' ? [] : [touch(x)],
                targetTouches: type === 'touchend' ? [] : [touch(x)],
                changedTouches: [touch(x)]
            }));
            fire('touchstart', 150);
            fire('touchmove', 200);
            fire('touchmove', 300);
            fire('touchmove', 380);
            fire('touchend', 380);
        }", calloutId);

        await Expect(Trigger("dd-resp")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(callout).ToHaveCSSAsync("opacity", "0");
        await Expect(Page.Locator("#resp-value")).ToHaveTextAsync("");
    }

    [TestMethod]
    public async Task Responsive_Rtl_SlidesInFromTheLeftEdge()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-rtl");
        var box = await BoxAsync(callout);

        Assert.AreEqual(0, box.X, 1.5, "The RTL panel starts at the left edge of the screen.");
        await Expect(callout).ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod]
    public async Task ResponsiveCombo_ThePanelHasItsOwnInput_AndTheAddButtonCreatesTheItem()
    {
        await OpenAsync(Url);

        var fieldInput = ComboInput("dd-resp-combo");
        await Expect(fieldInput).ToHaveAttributeAsync("role", "combobox");

        var callout = await TapOpenAsync("dd-resp-combo");
        var panelInput = callout.Locator("input.bit-drp-icb");

        // The combobox role moves to the input the user can see.
        await Expect(panelInput).ToHaveAttributeAsync("role", "combobox");
        await Expect(panelInput).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(fieldInput).Not.ToHaveAttributeAsync("role", "combobox");
        await Expect(fieldInput).ToHaveAttributeAsync("tabindex", "-1");

        foreach (var part in new[] { "ResponsiveComboInputContainer", "ResponsiveComboAddButton", "ComboBoxInput" })
        {
            await Expect(callout.Locator($".c-{part}").First).ToHaveAttributeAsync("style", new Regex($"--part-{part}: 1"));
        }

        await Expect(callout.Locator(".bit-drp-cls i")).ToHaveClassAsync(HasClass("bit-icon--ChromeClose"));

        var add = callout.GetByRole(AriaRole.Button, new() { Name = "Add it", Exact = true });
        await Expect(add.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--CircleAddition"));

        await panelInput.TapAsync();
        await panelInput.FillAsync("Kiwi");
        await Expect(callout.Locator(".bit-drp-dyn")).ToHaveTextAsync("Add \"Kiwi\"");

        await add.TapAsync();

        await Expect(Page.Locator("#resp-combo-value")).ToHaveTextAsync("dyn-kiwi");
        await Expect(fieldInput).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(Page.Locator("#dd-resp-combo .bit-drp-tdp")).ToHaveTextAsync("Kiwi");
    }

    [TestMethod]
    public async Task ResponsiveComboMulti_EnterAddsTheItem_AndThePanelStaysOpenForTheNext()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-combo-multi");
        var panelInput = callout.Locator("input.bit-drp-icb");

        await Expect(callout.Locator(".bit-drp-cls i")).ToHaveClassAsync(HasClass("ext-close"));
        await Expect(callout.Locator(".bit-drp-cai i")).ToHaveClassAsync(HasClass("ext-add"));
        await Expect(callout.Locator(".bit-drp-cai")).ToHaveAttributeAsync("aria-label", "Add");

        await panelInput.TapAsync();
        await panelInput.FillAsync("Kiwi");
        await panelInput.PressAsync("Enter");

        await Expect(Page.Locator("#resp-combo-multi-values")).ToHaveTextAsync("dyn-kiwi");
        await Expect(panelInput).ToHaveValueAsync("");
        await Expect(callout).ToHaveCSSAsync("opacity", "1");
        await Expect(panelInput).ToHaveAttributeAsync("aria-expanded", "true");

        await panelInput.FillAsync("apple");
        await panelInput.PressAsync("Enter");
        await Expect(Page.Locator("#resp-combo-multi-values")).ToHaveTextAsync("dyn-kiwi,f-app");
    }

    [TestMethod]
    public async Task ResponsiveSearch_TheSearchBoxTakesTheFocusInsideThePanel()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-search");
        var input = callout.Locator("input.bit-drp-sin");

        await Expect(input).ToBeFocusedAsync();
        await input.FillAsync("gr");
        await Expect(Options(callout)).ToHaveTextAsync(["Grape"]);
    }

    [TestMethod]
    public async Task Responsive_ALongList_EndsJustShortOfTheBottomOfTheScreen()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-resp-long");

        // The list is capped once the panel has slid in, which writes the cap onto it.
        var list = callout.Locator(".bit-drp-scn");
        await Expect(list).ToHaveAttributeAsync("style", new Regex("max-height"));

        var box = await BoxAsync(list);
        Assert.AreEqual(844 - 10, box.Y + box.Height, 1.5, "The list keeps its gap from the bottom of a screen with no safe area.");

        // The cap is only half of it: the user still reaches the last item by scrolling the list itself. Wheeled over
        // the list rather than scrolled into view from script, which would move a list that clips its overflow too - a
        // page of it at a time, each let land, until it reaches its end: Firefox moves a scroller by no more than a page
        // for one wheel event however large its delta is, where the other engines go the whole way.
        var last = callout.GetByRole(AriaRole.Option, new() { Name = "Item 60", Exact = true });

        await Page.Mouse.MoveAsync(box.X + box.Width / 2, box.Y + box.Height / 2);
        var handle = await list.ElementHandleAsync();
        for (var i = 0; i < 20 && await list.EvaluateAsync<bool>("e => e.scrollTop + e.clientHeight < e.scrollHeight - 1"); i++)
        {
            var before = await list.EvaluateAsync<double>("e => e.scrollTop");
            await Page.Mouse.WheelAsync(0, (float)(box.Height * 0.8));
            await Page.WaitForFunctionAsync("([e, before]) => e.scrollTop > before", new object[] { handle, before });
        }
        await Expect(last).ToBeInViewportAsync(new() { Ratio = 1 });

        var item = await BoxAsync(last);
        Assert.IsTrue(item.Y >= box.Y - 0.5 && item.Y + item.Height <= box.Y + box.Height + 0.5,
            $"The last item ({item.Y}..{item.Y + item.Height}px) is not within the visible part of the list ({box.Y}..{box.Y + box.Height}px).");
    }

    [TestMethod]
    public async Task Responsive_OnAPhoneWithANotch_KeepsTheHeaderAndTheListClearOfTheSafeAreas()
    {
        if (IsChromium is false) Assert.Inconclusive("The safe areas of a phone are emulated through the Chrome DevTools Protocol, which only Chromium speaks.");

        await OpenAsync(Url);
        await SetSafeAreaInsetsAsync(top: 47, bottom: 34);

        var callout = await TapOpenAsync("dd-resp-long");
        var list = callout.Locator(".bit-drp-scn");
        await Expect(list).ToHaveAttributeAsync("style", new Regex("max-height"));

        // The surface still runs from the top edge of the screen to the bottom one, behind the bars of the phone.
        var box = await BoxAsync(callout);
        Assert.AreEqual(0, box.Y, 1.5);
        Assert.AreEqual(844, box.Height, 1.5);

        // What is in it starts below the notch and ends above the home indicator, which an app shell's bars paint over.
        var header = await BoxAsync(callout.Locator(".bit-drp-rlc"));
        Assert.IsTrue(header.Y >= 47, $"The header of the panel starts under the notch ({header.Y}px).");

        var listBox = await BoxAsync(list);
        Assert.AreEqual(844 - 34, listBox.Y + listBox.Height, 1.5, "The list ends at the home indicator.");
    }

    [TestMethod]
    public async Task Responsive_OnAPhoneWithANotch_APaddingOfTheConsumersOwnStillLeavesTheSafeAreasClear()
    {
        if (IsChromium is false) Assert.Inconclusive("The safe areas of a phone are emulated through the Chrome DevTools Protocol, which only Chromium speaks.");

        await OpenAsync(Url);
        await SetSafeAreaInsetsAsync(top: 47, bottom: 34);

        var callout = await TapOpenAsync("dd-resp-long-padded");
        var list = callout.Locator(".bit-drp-scn");
        await Expect(list).ToHaveAttributeAsync("style", new Regex("max-height"));

        // The padding the consumer gave the panel replaced the one of the stylesheet, and neither safe area was part of it.
        await Expect(callout).ToHaveCSSAsync("padding-top", "0px");

        var header = await BoxAsync(callout.Locator(".bit-drp-rlc"));
        Assert.IsTrue(header.Y >= 47, $"The header of the panel starts under the notch ({header.Y}px).");

        var listBox = await BoxAsync(list);
        Assert.AreEqual(844 - 34, listBox.Y + listBox.Height, 1.5, "The list ends at the home indicator.");
    }

    [TestMethod]
    public async Task Responsive_OnAPhoneWithANotch_TheKeyboardTakesOverTheSafeAreaAtTheBottom()
    {
        if (IsChromium is false) Assert.Inconclusive("The safe areas of a phone are emulated through the Chrome DevTools Protocol, which only Chromium speaks.");

        await OpenAsync(Url);
        await SetSafeAreaInsetsAsync(top: 47, bottom: 34);

        var callout = await TapOpenAsync("dd-resp-long-search");
        await Expect(callout.Locator("input.bit-drp-sin")).ToBeFocusedAsync();

        // The keyboard the focused search box raises leaves the room above it, which the viewport is shortened to. The
        // bottom of what is left is the top of the keyboard, with nothing of the home indicator showing above it, so the
        // list keeps the gap it has always kept from there rather than a safe area the keyboard already covers.
        await Page.SetViewportSizeAsync(390, 544);

        var list = await callout.Locator(".bit-drp-scn").ElementHandleAsync();
        await Page.WaitForFunctionAsync("el => Math.abs(el.getBoundingClientRect().bottom - (544 - 10)) <= 1.5", list);
    }

    [TestMethod]
    public async Task NotResponsive_OnAPhone_StaysAPlainCalloutUnderTheField()
    {
        await OpenAsync(Url);

        var callout = await TapOpenAsync("dd-nonresp");

        await Expect(callout).Not.ToHaveClassAsync(HasClass("bit-drp-res"));
        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");

        var field = await BoxAsync(Trigger("dd-nonresp"));
        var box = await BoxAsync(callout);
        Assert.AreEqual(field.Y + field.Height + 1, box.Y, 2);
        Assert.AreEqual(field.Width, box.Width, 1);
    }
}
