namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiScrollToLatest at /e2e/aiscrolltolatest in a real browser: nothing rendered until it is Visible, the
/// button with its label and the Down glyph of the AI font, OnClick by pointer and keyboard, its place floating centered
/// above the positioned box it is in (and still centered in right-to-left text), its entrance and reduced motion,
/// Label, Disabled (and the page's own handlers on its root it drops), the theme's popup shadow, every
/// --bit-AiScrollToLatest-* variable, Styles/Classes, RTL and print.
/// </summary>
[TestClass]
public class BitAiScrollToLatestE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiscrolltolatest";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Button(string id) => Root(id).Locator("button.bit-btn");

    private async Task ShowAsync()
    {
        await Page.Locator("#stl-toggle").ClickAsync();
        await Expect(Root("stl-basic")).ToBeVisibleAsync();
        await LandedAsync("stl-basic");
    }

    // Waits for the entrance of a button to have run, so it is measured where it rests.
    private Task LandedAsync(string id) => Root(id).EvaluateAsync("e => Promise.all(e.getAnimations().map(a => a.finished))");

    // ---------- basic ----------

    [TestMethod]
    public async Task Visible_Toggled_RendersTheButtonOnlyWhileThereIsSomewhereToGo()
    {
        await OpenAsync(Route);

        await Expect(Root("stl-basic")).ToHaveCountAsync(0);

        await ShowAsync();
        await Expect(Root("stl-basic")).ToHaveClassAsync(HasClass("bit-aistl"));
        await Expect(Button("stl-basic")).ToHaveTextAsync("Scroll to latest");
        await Expect(Root("stl-basic").GetByRole(AriaRole.Button, new() { Name = "Scroll to latest" })).ToBeVisibleAsync();
        await Button("stl-basic").Locator("i.bit-btn-icn").AssertAiGlyphAsync("Down");
        await Expect(Button("stl-basic").Locator("i.bit-btn-icn")).ToHaveAttributeAsync("aria-hidden", "true");

        await Page.Locator("#stl-toggle").ClickAsync();
        await Expect(Root("stl-basic")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task OnClick_ByPointerAndKeyboard_IsRaised()
    {
        await OpenAsync(Route);
        await ShowAsync();

        await Button("stl-basic").ClickAsync();
        await Expect(Page.Locator("#stl-clicks")).ToHaveTextAsync("1");

        await Button("stl-basic").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#stl-clicks")).ToHaveTextAsync("2");
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#stl-clicks")).ToHaveTextAsync("3");
    }

    [TestMethod]
    public async Task Position_TheButton_FloatsCenteredAboveItsPositionedBox()
    {
        await OpenAsync(Route);
        await ShowAsync();

        var box = await Page.Locator("#stl-box").RectAsync();
        var root = await Root("stl-basic").RectAsync();
        await Expect(Root("stl-basic")).ToHaveCSSAsync("position", "absolute");

        var offset = AiE2EHelpers.ParsePx(await Root("stl-basic").ResolveLengthInAsync("calc(var(--bit-spa-scaling-factor) * var(--bit-layout-density-scale, 1) * 1)"));
        Assert.AreEqual(box.Top - offset, root.Bottom, 1, "The button floats one spacing unit above the box.");
        Assert.AreEqual((box.Left + box.Right) / 2, (root.Left + root.Right) / 2, 1, "The button is centered over the box.");
        await Expect(Root("stl-basic")).ToHaveCSSAsync("z-index", "3");
    }

    [TestMethod]
    public async Task Theme_TheButton_IsRoundOnThePrimaryBackgroundWithThePopupShadow()
    {
        await OpenAsync(Route);
        await ShowAsync();

        await Expect(Button("stl-basic")).ToHaveClassAsync(HasClass("bit-btn-dft-pbg"));
        var shadow = await Root("stl-basic").EvaluateAsync<string>(
            """
            e => {
                const probe = document.createElement('div');
                probe.style.boxShadow = 'var(--bit-shd-popup)';
                document.body.appendChild(probe);
                const resolved = getComputedStyle(probe).boxShadow;
                probe.remove();
                return resolved;
            }
            """);
        await Expect(Root("stl-basic")).ToHaveCSSAsync("box-shadow", shadow);
        Assert.AreNotEqual("none", shadow);

        var height = AiE2EHelpers.ParsePx(await ComputedStyleAsync("#stl-basic button", "height"));
        Assert.IsTrue(AiE2EHelpers.ParsePx(await ComputedStyleAsync("#stl-basic button", "border-top-left-radius")) >= height / 2 - 0.5, "The button is rounded.");
    }

    [TestMethod]
    public async Task Entrance_TheButton_RisesInAndReducedMotionCollapsesIt()
    {
        await OpenAsync(Route);
        await ShowAsync();

        await Expect(Root("stl-basic")).ToHaveCSSAsync("animation-name", "bit-aistl-ent");
        Assert.IsTrue(AiE2EHelpers.ParseSeconds(await ComputedStyleAsync("#stl-basic", "animation-duration")) > 0);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        Assert.IsTrue(AiE2EHelpers.ParseSeconds(await ComputedStyleAsync("#stl-basic", "animation-duration")) <= 0.001, "The entrance collapses.");

    }

    // ---------- label ----------

    [TestMethod]
    public async Task Label_Set_ReplacesTheTextOfTheStringTable()
    {
        await OpenAsync(Route);

        await Expect(Button("stl-label")).ToHaveTextAsync("Jump to the newest");
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Jump to the newest" })).ToBeVisibleAsync();
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheButton_RaisesNothingAndDropsThePagesHandlersOfItsRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("stl-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Button("stl-disabled")).ToBeDisabledAsync();
        await Expect(Button("stl-disabled")).ToHaveAttributeAsync("aria-disabled", "true");

        await Button("stl-disabled").ClickAsync(new() { Force = true });
        await Root("stl-disabled").DispatchEventAsync("click");
        await Button("stl-disabled").DispatchEventAsync("click");

        // The enabled one still runs the page's handler of its root.
        await Button("stl-enabled").ClickAsync();
        await Expect(Page.Locator("#stl-enabled-root-clicks")).ToHaveTextAsync("1");

        await Expect(Page.Locator("#stl-disabled-clicks")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#stl-root-clicks")).ToHaveTextAsync("0");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_MoveStackAndShadowTheButton()
    {
        await OpenAsync(Route);

        await LandedAsync("stl-var");
        var box = await Page.Locator("#stl-vars-box").RectAsync();
        var root = await Root("stl-var").RectAsync();
        Assert.AreEqual(box.Top - 20, root.Bottom, 1, "The offset puts 20px between the button and the box.");
        await Expect(Root("stl-var")).ToHaveCSSAsync("z-index", "7");
        await Expect(Root("stl-var")).ToHaveCSSAsync("box-shadow", "rgb(255, 0, 0) 0px 0px 0px 3px");
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheRootTheButtonAndItsIcon()
    {
        await OpenAsync(Route);

        await Expect(Root("stl-classes")).ToHaveClassAsync(HasClass("e2e-stl-root"));
        await Expect(Button("stl-classes")).ToHaveClassAsync(HasClass("e2e-stl-button"));
        await Expect(Button("stl-classes")).ToHaveCSSAsync("outline-color", "rgb(5, 4, 3)");

        var icon = Button("stl-classes").Locator("i.bit-btn-icn");
        await Expect(icon).ToHaveClassAsync(HasClass("e2e-stl-icon"));
        await icon.AssertAiGlyphAsync("Down");
    }

    // ---------- rtl, print ----------

    [TestMethod]
    public async Task Rtl_TheButton_StaysCenteredOverItsBox()
    {
        await OpenAsync(Route);

        await Expect(Root("stl-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await LandedAsync("stl-rtl");
        var box = await Page.Locator("#stl-rtl-box").RectAsync();
        var root = await Root("stl-rtl").RectAsync();
        Assert.AreEqual((box.Left + box.Right) / 2, (root.Left + root.Right) / 2, 1);

        // The glyph and the label read from the right.
        var icon = await Button("stl-rtl").Locator("i.bit-btn-icn").RectAsync();
        var label = await Button("stl-rtl").Locator(".bit-btn-tcn").RectAsync();
        Assert.IsTrue(icon.Left > label.Left, "The glyph comes first, at the right.");
    }

    [TestMethod]
    public async Task Print_TheButton_IsLeftOut()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { Media = Media.Print });

        await Expect(Root("stl-label")).ToHaveCSSAsync("display", "none");
    }
}
