using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The Responsive mode of BitDropdown on a phone (<c>/e2e/dropdown/responsive</c>, a 390x844 touch viewport): a tap
/// opens a full-height side panel at the end of the screen carrying the label and a close button
/// (ResponsiveCloseButtonAriaLabel), the close button, a pick and a swipe dismiss it, an RTL panel slides in from the
/// left, the ComboBox panel brings its own combobox input and add button (ComboBoxAddButtonAriaLabel, the close and add
/// icons by name and by BitIconInfo), Enter in a multi select panel keeps it open, the search box takes the focus inside
/// the panel, the panel's parts take Styles and Classes, and a dropdown that is not Responsive stays a plain callout.
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
