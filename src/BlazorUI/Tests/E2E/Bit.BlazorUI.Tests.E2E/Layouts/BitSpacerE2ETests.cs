namespace Bit.BlazorUI.Tests.E2E.Layouts;

/// <summary>
/// BitSpacer in a real browser: the room a flexible spacer takes in a real flex row and column (alone, several, with a
/// container gap, with Grow, under a MinGap floor), the fixed space of Width / Height / Gap / Size and which of them
/// wins, Vertical, Element, the accessibility attributes, the BitParams cascade, the --bit-Spacer-*-size variables,
/// Style / Class / HtmlAttributes / Visibility / Disabled, RTL (inherited and explicit), a runtime parameter change,
/// prerendering and unmounting.
/// </summary>
[TestClass]
public class BitSpacerE2ETests : E2ETestBase
{
    private const double Tolerance = 1;

    [TestMethod]
    public async Task Prerender_TheSpacerIsInTheServerRenderedHtml()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/e2e/spacer");
        var html = await response.TextAsync();

        StringAssert.Contains(html, "id=\"basic-spacer\"");
        StringAssert.Contains(html, "bit-spc");
        StringAssert.Contains(html, "Status: <span id=\"status\">Loading</span>");
    }

    [TestMethod]
    public async Task Basic_AFlexibleSpacer_PushesItsNeighboursToTheTwoEnds()
    {
        await OpenAsync("/e2e/spacer");

        var row = await BoxAsync("#basic-row");
        var a = await BoxAsync("#basic-a");
        var b = await BoxAsync("#basic-b");
        var spacer = await BoxAsync("#basic-spacer");

        AssertClose(row.X, a.X, "the first box sits at the start");
        AssertClose(row.Right, b.Right, "the last box sits at the end");
        AssertClose(400, spacer.W, "the spacer takes all of the leftover room");
        await Expect(Page.Locator("#basic-spacer")).ToHaveClassAsync(HasClass("bit-spc"));
        await Expect(Page.Locator("#basic-spacer")).ToHaveCSSAsync("flex-grow", "1");
        await Expect(Page.Locator("#basic-spacer")).ToHaveCSSAsync("align-self", "stretch");
    }

    [TestMethod]
    public async Task Basic_SeveralFlexibleSpacers_SplitTheRoomEvenly()
    {
        await OpenAsync("/e2e/spacer");

        var s1 = await BoxAsync("#even-s1");
        var s2 = await BoxAsync("#even-s2");

        AssertClose(150, s1.W, "300px left over split in two");
        AssertClose(150, s2.W, "300px left over split in two");
        AssertClose((await BoxAsync("#even-row")).Right, (await BoxAsync("#even-c")).Right, "the last box at the end");
    }

    [TestMethod]
    public async Task Basic_AContainerGap_IsAddedOnBothSidesOfTheSpacer()
    {
        await OpenAsync("/e2e/spacer");

        var a = await BoxAsync("#cgap-a");
        var b = await BoxAsync("#cgap-b");

        AssertClose(10 + 20 + 10, b.X - a.Right, "gap + fixed space + gap");
    }

    [TestMethod]
    public async Task Width_AFixedSpacer_KeepsItsPixelsAndDoesNotGrow()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(32, (await BoxAsync("#width-b")).X - (await BoxAsync("#width-a")).Right, "Width=32");
        await Expect(Page.Locator("#width-spacer")).ToHaveCSSAsync("margin-inline-start", "32px");
        await Expect(Page.Locator("#width-spacer")).ToHaveCSSAsync("flex-grow", "0");

        // A parameter changed at runtime re-renders the spacer with the new space.
        await Page.ClickAsync("#btn-width");

        await Expect(Page.Locator("#width-spacer")).ToHaveCSSAsync("margin-inline-start", "96px");
        AssertClose(96, (await BoxAsync("#width-b")).X - (await BoxAsync("#width-a")).Right, "Width=96");
    }

    [TestMethod]
    public async Task Gap_AnyCssLength_IsTheFixedSpace()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(16, (await BoxAsync("#gap-b")).X - (await BoxAsync("#gap-a")).Right, "1rem");
        // A percentage margin resolves against the width of the containing block: 10% of 600px.
        AssertClose(60, (await BoxAsync("#gap-c")).X - (await BoxAsync("#gap-b")).Right, "10%");
        AssertClose(22, (await BoxAsync("#gap-d")).X - (await BoxAsync("#gap-c")).Right, "var(--spc-my-gap)");
        await Expect(Page.Locator("#gap-rem")).ToHaveCSSAsync("flex-grow", "0");
    }

    [TestMethod]
    public async Task Precedence_GapWinsOverWidthAndSize_WidthWinsOverSize()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(10, (await BoxAsync("#prec-b")).X - (await BoxAsync("#prec-a")).Right, "Gap wins");
        AssertClose(50, (await BoxAsync("#prec-c")).X - (await BoxAsync("#prec-b")).Right, "Width wins over Size");
    }

    [TestMethod]
    public async Task Vertical_HeightVerticalGapAndAFlexibleSpacer_SpaceAColumn()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(32, (await BoxAsync("#vert-b")).Y - (await BoxAsync("#vert-a")).Bottom, "Height=32");
        AssertClose(48, (await BoxAsync("#vert-c")).Y - (await BoxAsync("#vert-b")).Bottom, "Vertical Gap=3rem");
        await Expect(Page.Locator("#vert-gap")).ToHaveCSSAsync("margin-block-start", "48px");
        await Expect(Page.Locator("#vert-gap")).ToHaveCSSAsync("margin-inline-start", "0px");

        // The flexible spacer grows along the column, pinning the last box to the bottom.
        AssertClose((await BoxAsync("#vertical-col")).Bottom, (await BoxAsync("#vert-d")).Bottom, "the last box at the bottom");
    }

    [TestMethod]
    public async Task Grow_TwoSpacers_SplitTheLeftoverOneThirdTwoThirds()
    {
        await OpenAsync("/e2e/spacer");

        var s1 = await BoxAsync("#grow-1");
        var s2 = await BoxAsync("#grow-2");

        AssertClose(100, s1.W, "1/3 of 300px");
        AssertClose(200, s2.W, "2/3 of 300px");
        await Expect(Page.Locator("#grow-2")).ToHaveCSSAsync("flex-grow", "2");

        // A fixed spacer ignores Grow.
        await Expect(Page.Locator("#grow-fixed")).ToHaveCSSAsync("flex-grow", "0");
        AssertClose(10, (await BoxAsync("#growf-b")).X - (await BoxAsync("#growf-a")).Right, "fixed 10px despite Grow=5");
    }

    [TestMethod]
    public async Task MinGap_ASqueezedRow_KeepsTheFloorOfAFlexibleSpacerOnly()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(32, (await BoxAsync("#min-b")).X - (await BoxAsync("#min-a")).Right, "MinGap 2rem kept");
        await Expect(Page.Locator("#min-spacer")).ToHaveCSSAsync("min-inline-size", "32px");

        AssertClose(0, (await BoxAsync("#nomin-b")).X - (await BoxAsync("#nomin-a")).Right, "no floor, collapsed");

        // A fixed spacer ignores MinGap.
        AssertClose(8, (await BoxAsync("#minfix-b")).X - (await BoxAsync("#minfix-a")).Right, "fixed 8px, MinGap ignored");
        await Expect(Page.Locator("#minfix-spacer")).Not.ToHaveAttributeAsync("style", new System.Text.RegularExpressions.Regex("min-"));

        // Vertical moves the floor to the block axis.
        await Expect(Page.Locator("#minv-spacer")).ToHaveCSSAsync("min-block-size", "30px");
        AssertClose(30, (await BoxAsync("#minv-b")).Y - (await BoxAsync("#minv-a")).Bottom, "vertical MinGap kept");
    }

    [TestMethod]
    public async Task Element_RendersTheGivenTag()
    {
        await OpenAsync("/e2e/spacer");

        Assert.AreEqual("LI", await Page.Locator("#element-spacer").EvaluateAsync<string>("el => el.tagName"));
        Assert.AreEqual("UL", await Page.Locator("#element-spacer").EvaluateAsync<string>("el => el.parentElement.tagName"));
        await Expect(Page.Locator("#element-list > li")).ToHaveCountAsync(3);
        AssertClose((await BoxAsync("#element-list")).Right, (await BoxAsync("#element-last")).Right, "the li spacer still flexes");
        Assert.AreEqual("DIV", await Page.Locator("#basic-spacer").EvaluateAsync<string>("el => el.tagName"));
    }

    [TestMethod]
    public async Task Accessibility_AnUnlabelledSpacer_IsHiddenAndALabelledOne_IsANamedSeparator()
    {
        await OpenAsync("/e2e/spacer");

        var plain = Page.Locator("#a11y-plain");
        await Expect(plain).ToHaveAttributeAsync("aria-hidden", "true");
        Assert.IsNull(await plain.GetAttributeAsync("role"));
        Assert.IsNull(await plain.GetAttributeAsync("aria-label"));

        var labelled = Page.Locator("#a11y-labelled");
        await Expect(labelled).ToHaveAttributeAsync("role", "separator");
        await Expect(labelled).ToHaveAttributeAsync("aria-label", "Support");
        await Expect(labelled).ToHaveAttributeAsync("aria-orientation", "vertical");
        Assert.IsNull(await labelled.GetAttributeAsync("aria-hidden"));
        await Expect(Page.GetByRole(AriaRole.Separator, new() { Name = "Support" })).ToHaveCountAsync(1);

        // A separator dividing a column is horizontal, the ARIA default, so no orientation is written.
        foreach (var id in new[] { "#a11y-vertical", "#a11y-height" })
        {
            await Expect(Page.Locator(id)).ToHaveAttributeAsync("role", "separator");
            Assert.IsNull(await Page.Locator(id).GetAttributeAsync("aria-orientation"), id);
        }

        // The splatted attributes win over the generated ones.
        var overridden = Page.Locator("#a11y-override");
        await Expect(overridden).ToHaveAttributeAsync("role", "none");
        await Expect(overridden).ToHaveAttributeAsync("aria-hidden", "true");

        // An aria-label given through HtmlAttributes labels the spacer as AriaLabel does.
        var htmlLabel = Page.Locator("#a11y-html-label");
        await Expect(htmlLabel).ToHaveAttributeAsync("role", "separator");
        await Expect(htmlLabel).ToHaveAttributeAsync("aria-label", "From attributes");
    }

    [TestMethod]
    public async Task CascadingParameters_BitParams_AreDefaultsAndStopAtTheirScope()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(24, (await BoxAsync("#casc-b")).X - (await BoxAsync("#casc-a")).Right, "the cascaded 1.5rem");
        AssertClose(64, (await BoxAsync("#casc-c")).X - (await BoxAsync("#casc-b")).Right, "the spacer's own Gap wins");
        await Expect(Page.Locator("#casc-1")).ToHaveCSSAsync("flex-grow", "0");

        // Outside the BitParams the spacer is flexible again.
        await Expect(Page.Locator("#casc-out")).ToHaveCSSAsync("flex-grow", "1");
        AssertClose(400, (await BoxAsync("#casc-out")).W, "flexible outside the scope");
    }

    [TestMethod]
    public async Task Size_EveryStep_TakesTheSpacingScaleOfTheTheme()
    {
        await OpenAsync("/e2e/spacer");

        // (id, class, expected px, previous box, next box) with the default theme's 0.5rem spacing unit.
        var cases = new (string Id, string Class, double Px, string Before, string After)[]
        {
            ("#size-sm", "bit-spc-sm", 8, "#size-a", "#size-b"),
            ("#size-md", "bit-spc-md", 16, "#size-b", "#size-c"),
            ("#size-lg", "bit-spc-lg", 32, "#size-c", "#size-d"),
        };

        foreach (var (id, cls, px, before, after) in cases)
        {
            await Expect(Page.Locator(id)).ToHaveClassAsync(HasClass(cls));
            AssertClose(px, (await BoxAsync(after)).X - (await BoxAsync(before)).Right, id);
            await Expect(Page.Locator(id)).ToHaveCSSAsync("flex-grow", "0");
        }

        AssertClose(8, (await BoxAsync("#sizev-b")).Y - (await BoxAsync("#sizev-a")).Bottom, "vertical small");
        AssertClose(32, (await BoxAsync("#sizev-c")).Y - (await BoxAsync("#sizev-b")).Bottom, "vertical large");
    }

    [TestMethod]
    public async Task CssVariables_TheSizeStepsSetOnAnAncestor_ResizeTheSpacersUnderneath()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(96, (await BoxAsync("#sizevar-b")).X - (await BoxAsync("#sizevar-a")).Right, "--bit-Spacer-large-size: 6rem");
        AssertClose(3, (await BoxAsync("#sizevar-c")).X - (await BoxAsync("#sizevar-b")).Right, "--bit-Spacer-small-size: 3px");
        AssertClose(13, (await BoxAsync("#sizevar-d")).X - (await BoxAsync("#sizevar-c")).Right, "--bit-Spacer-medium-size: 13px");
    }

    [TestMethod]
    public async Task StyleClassAttributes_LandOnTheSpacerElement()
    {
        await OpenAsync("/e2e/spacer");

        var styled = Page.Locator("#style-spacer");
        await Expect(styled).ToHaveCSSAsync("background-color", "rgb(255, 99, 71)");
        Assert.IsTrue((await BoxAsync("#style-spacer")).W >= 64 - Tolerance, "min-width:4rem holds");

        await Expect(Page.Locator("#class-spacer")).ToHaveClassAsync(HasClass("spc-custom"));
        await Expect(Page.Locator("#class-spacer")).ToHaveCSSAsync("background-color", "rgb(0, 128, 0)");
        // A decorated spacer stretches along the cross axis of a centered row, so the decoration shows.
        AssertClose(40, (await BoxAsync("#class-spacer")).H, "stretched to the row's height");

        var attr = Page.Locator("#attr-spacer");
        await Expect(attr).ToHaveAttributeAsync("data-test", "spacer");
        await Expect(attr).ToHaveAttributeAsync("title", "A spacer");
        await Expect(attr).ToHaveAttributeAsync("data-own", "yes");
    }

    [TestMethod]
    public async Task Visibility_HiddenKeepsTheSpace_CollapsedRemovesIt()
    {
        await OpenAsync("/e2e/spacer");

        AssertClose(50, (await BoxAsync("#vis-b")).X - (await BoxAsync("#vis-a")).Right, "visible");

        await Page.ClickAsync("#btn-hidden");
        await Expect(Page.Locator("#vis-spacer")).ToHaveCSSAsync("visibility", "hidden");
        AssertClose(50, (await BoxAsync("#vis-b")).X - (await BoxAsync("#vis-a")).Right, "hidden keeps its room");

        await Page.ClickAsync("#btn-collapsed");
        await Expect(Page.Locator("#vis-spacer")).ToHaveCSSAsync("display", "none");
        AssertClose(0, (await BoxAsync("#vis-b")).X - (await BoxAsync("#vis-a")).Right, "collapsed takes no room");

        await Page.ClickAsync("#btn-visible");
        await Expect(Page.Locator("#vis-spacer")).ToHaveCSSAsync("display", "block");
    }

    [TestMethod]
    public async Task Disabled_TheRootCarriesBitDis_AndLosesItWhenEnabledAgain()
    {
        await OpenAsync("/e2e/spacer");

        await Expect(Page.Locator("#disabled-spacer")).ToHaveClassAsync(HasClass("bit-dis"));

        await Page.ClickAsync("#btn-toggle-disabled");

        await Expect(Page.Locator("#disabled-spacer")).Not.ToHaveClassAsync(HasClass("bit-dis"));
    }

    [TestMethod]
    public async Task Rtl_AnInheritedDirection_MirrorsTheFixedSpace()
    {
        await OpenAsync("/e2e/spacer");

        var a = await BoxAsync("#rtl-a");
        var b = await BoxAsync("#rtl-b");
        var row = await BoxAsync("#rtl-row");

        AssertClose(row.Right, a.Right, "the first box starts at the right edge");
        AssertClose(64, a.X - b.Right, "the space sits to the left of the first box");
        AssertClose(row.X, (await BoxAsync("#rtl-c")).X, "the flexible spacer pushes the last box to the left edge");
        await Expect(Page.Locator("#rtl-inherited")).ToHaveCSSAsync("margin-right", "64px");
    }

    [TestMethod]
    public async Task Rtl_AnExplicitDir_MovesTheSpaceToTheOtherSideOfTheSpacer()
    {
        await OpenAsync("/e2e/spacer");

        var spacer = Page.Locator("#dir-spacer");
        await Expect(spacer).ToHaveAttributeAsync("dir", "rtl");
        await Expect(spacer).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(spacer).ToHaveCSSAsync("margin-right", "64px");
        await Expect(spacer).ToHaveCSSAsync("margin-left", "0px");

        // The 64px stays between the two boxes, but on the far side of the zero-width spacer.
        var a = await BoxAsync("#dir-a");
        var s = await BoxAsync("#dir-spacer");
        AssertClose(a.Right, s.X, "the spacer touches the first box");
        AssertClose(64, (await BoxAsync("#dir-b")).X - a.Right, "the gap is still 64px");
    }

    [TestMethod]
    public async Task Lifecycle_UnmountingAndMountingAgain_RaisesNoError()
    {
        await OpenAsync("/e2e/spacer");

        await Expect(Page.Locator("#mount-spacer")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-unmount");
        await Expect(Page.Locator("#mounted")).ToHaveTextAsync("False");
        await Expect(Page.Locator("#mount-spacer")).ToHaveCountAsync(0);

        await Page.ClickAsync("#btn-mount");
        await Expect(Page.Locator("#mount-spacer")).ToHaveCSSAsync("margin-inline-start", "40px");

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    private async Task<Box> BoxAsync(string selector)
    {
        var box = await Page.Locator(selector).BoundingBoxAsync();
        Assert.IsNotNull(box, $"{selector} has no box");
        return new(box.X, box.Y, box.Width, box.Height);
    }

    private static void AssertClose(double expected, double actual, string what)
    {
        Assert.IsTrue(Math.Abs(expected - actual) <= Tolerance, $"{what}: expected {expected}, got {actual}");
    }

    private sealed record Box(double X, double Y, double W, double H)
    {
        public double Right => X + W;
        public double Bottom => Y + H;
    }
}
