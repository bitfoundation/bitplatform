namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// The callout of BitDropdown (<c>/e2e/dropdown/callout</c>): its bounding box against the field - below it and as wide
/// as it, as wide as its longest item when the field is narrower, its own width with PreserveCalloutWidth - above a field
/// near the bottom of the viewport, beside one with DropDirection.All when neither side fits, capped within the viewport
/// with TopAndBottom, MaxHeight, the header and footer templates around the scrolling list, dismissal by a page scroll,
/// a resize and another dropdown opening, and the entry animation collapsing under reduced motion unless ForceAnimation.
/// </summary>
[TestClass]
public class BitDropdownCalloutE2ETests : BitDropdownE2ETestBase
{
    private const string Url = "/e2e/dropdown/callout";

    private async Task<ILocator> OpenSettledAsync(string id)
    {
        var callout = await OpenDropdownAsync(id);

        await WaitForAnimationsAsync(callout);

        return callout;
    }

    [TestMethod]
    public async Task Callout_SitsBelowTheField_AsWideAsIt()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-below");
        var field = await BoxAsync(Trigger("dd-cal-below"));
        var box = await BoxAsync(callout);

        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.AreEqual(field.Y + field.Height + 1, box.Y, 2);
        Assert.AreEqual(field.X, box.X, 1);
        Assert.AreEqual(400, box.Width, 1);
        Assert.AreEqual(field.Width, box.Width, 1);
    }

    [TestMethod]
    public async Task Callout_IsAsWideAsItsLongestItem_WhenTheFieldIsNarrower()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-narrow");
        var field = await BoxAsync(Trigger("dd-cal-narrow"));
        var box = await BoxAsync(callout);

        Assert.AreEqual(120, field.Width, 1);
        Assert.IsTrue(box.Width > field.Width + 50, $"The callout ({box.Width}px) takes the width its items need.");
        Assert.AreEqual(field.X, box.X, 1);
    }

    [TestMethod]
    public async Task PreserveCalloutWidth_KeepsTheCalloutAtItsOwnWidth()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-preserve");
        var field = await BoxAsync(Trigger("dd-cal-preserve"));
        var box = await BoxAsync(callout);

        Assert.AreEqual(400, field.Width, 1);
        Assert.IsTrue(box.Width < field.Width - 100, $"The callout ({box.Width}px) is not stretched to the field ({field.Width}px).");
        Assert.AreEqual(field.X, box.X, 1);
        Assert.IsFalse((await callout.GetAttributeAsync("style") ?? string.Empty).Contains("width:"), "No width is written onto the callout.");
    }

    [TestMethod]
    public async Task Callout_OpensAboveAFieldNearTheBottomOfTheViewport()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-bottom");
        var field = await BoxAsync(Trigger("dd-cal-bottom"));
        var box = await BoxAsync(callout);

        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "above");
        Assert.AreEqual(field.Y - 1, box.Y + box.Height, 2, "The callout ends right above the field.");
        Assert.AreEqual(field.X, box.X, 1);
    }

    [TestMethod]
    public async Task DropDirectionAll_PlacesTheCalloutBesideTheField_WhenNeitherSideHasTheRoom()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-all");
        var field = await BoxAsync(Trigger("dd-cal-all"));
        var box = await BoxAsync(callout);
        var viewport = Page.ViewportSize!;

        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "right");
        Assert.AreEqual(field.X + field.Width + 1, box.X, 2, "The callout starts right after the field.");
        Assert.IsTrue(box.Y >= 0 && box.Y + box.Height <= viewport.Height, "The callout stays within the viewport.");
    }

    [TestMethod]
    public async Task DropDirectionTopAndBottom_KeepsTheCalloutAboveOrBelow_CappedToTheRoomThere()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-tb");
        var field = await BoxAsync(Trigger("dd-cal-tb"));
        var box = await BoxAsync(callout);
        var viewport = Page.ViewportSize!;

        await Expect(callout).ToHaveAttributeAsync("data-bit-cal-pos", "below");
        Assert.AreEqual(field.Y + field.Height + 1, box.Y, 2);
        Assert.IsTrue(box.Y + box.Height <= viewport.Height, $"The callout ends ({box.Y + box.Height}px) within the viewport ({viewport.Height}px).");
        Assert.AreEqual(field.X, box.X, 1);

        // The list scrolls inside the room it was given.
        var scroller = ListBox(callout);
        Assert.IsTrue(await scroller.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 1"));
    }

    [TestMethod]
    public async Task MaxHeight_CapsTheScrollingList()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-maxh");
        var scroller = ListBox(callout);

        Assert.IsTrue(await PxAsync(scroller, "height") <= 150.5, "The list is no taller than MaxHeight.");
        Assert.IsTrue(await scroller.EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight + 1"), "The rest of the list scrolls.");

        // The wheel scrolls the list under the pointer (by an amount each engine decides), never the page.
        await scroller.HoverAsync();
        await Page.Mouse.WheelAsync(0, 600);
        await Page.WaitForFunctionAsync("el => el.scrollTop > 40", await scroller.ElementHandleAsync());
        await Expect(Option(callout, "Option 01")).Not.ToBeInViewportAsync();
        Assert.AreEqual(0, await Page.EvaluateAsync<double>("() => window.scrollY"));
    }

    [TestMethod]
    public async Task CalloutTemplates_FrameTheScrollingListAndStayInteractive()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-hf");

        var header = callout.Locator("#callout-header-content");
        var footer = callout.Locator("#callout-footer-button");
        var list = ListBox(callout);

        await Expect(header).ToHaveTextAsync("Best in the world");
        await Expect(footer).ToBeVisibleAsync();

        var headerBox = await BoxAsync(header);
        var listBox = await BoxAsync(list);
        var footerBox = await BoxAsync(footer);

        Assert.IsTrue(headerBox.Y + headerBox.Height <= listBox.Y + 1, "The header sits above the list.");
        Assert.IsTrue(footerBox.Y >= listBox.Y + listBox.Height - 1, "The footer sits below the list.");
        Assert.IsTrue(footerBox.Y + footerBox.Height <= Page.ViewportSize!.Height, "The footer stays on the screen under a long list.");
        await Expect(header).Not.ToHaveAttributeAsync("role", "option");

        await footer.ClickAsync();

        await Expect(Page.Locator("#footer-clicks")).ToHaveTextAsync("1");
        await Expect(callout).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Callout_IsDismissedByAScrollOfThePage()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-below");

        await Page.EvaluateAsync("() => window.scrollBy(0, 300)");

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-cal-below")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Callout_IsDismissedWhenAnotherDropdownOpens()
    {
        await OpenAsync(Url);

        var first = await OpenSettledAsync("dd-cal-below");

        await Trigger("dd-cal-other").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        var second = await CalloutAsync("dd-cal-other");
        await Expect(second).ToBeVisibleAsync();
        await Expect(first).ToBeHiddenAsync();
        await Expect(Trigger("dd-cal-below")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Callout_IsDismissedByAResizeOfTheWindow()
    {
        await OpenAsync(Url);

        var callout = await OpenSettledAsync("dd-cal-below");

        await Page.SetViewportSizeAsync(1100, 650);

        await Expect(callout).ToBeHiddenAsync();
        await Expect(Trigger("dd-cal-below")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task EntryAnimation_RunsByDefault_CollapsesUnderReducedMotion_AndComesBackWithForceAnimation()
    {
        await OpenAsync(Url);

        var callout = await OpenDropdownAsync("dd-cal-below");
        await Expect(callout).ToHaveCSSAsync("animation-duration", "0.3s");
        Assert.AreNotEqual("none", await StyleAsync(callout, "animation-name"));
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        callout = await OpenDropdownAsync("dd-cal-below");
        var reduced = double.Parse((await StyleAsync(callout, "animation-duration")).TrimEnd('s'), System.Globalization.CultureInfo.InvariantCulture);
        Assert.IsTrue(reduced < 0.001, $"The entry animation collapses under reduced motion, lasted {reduced}s.");
        await Page.Keyboard.PressAsync("Escape");
        await Expect(callout).ToBeHiddenAsync();

        await Expect(Root("dd-cal-fam")).ToHaveClassAsync(HasClass("bit-fam"));
        var forced = await OpenDropdownAsync("dd-cal-fam");
        await Expect(forced).ToHaveCSSAsync("animation-duration", "0.3s");
    }
}
