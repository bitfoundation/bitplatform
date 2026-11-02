namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiRendererBoundary at /e2e/airendererboundary in a real browser: content that renders gets no element of
/// the boundary's own, a child that throws is replaced by a notice (a BitTag with its label, the exception's message as
/// its title, the warning glyph, the Id/Class/Style/Styles/Classes/Dir of the boundary) while the rest of the page lives
/// on, a render of the boundary by its parent tries the content again and recovers it, a fragment throwing inline, and
/// the boundaries around custom renderers: a tool renderer failing on partial arguments and a part renderer failing on a
/// partial value, each recovering once its card or part view renders again with the value complete, and failing again.
/// </summary>
/// <remarks>
/// What the boundary catches is a handled exception: on the server the framework logs it as a warning of the circuit,
/// and in the browser the WebAssembly logger writes it to the console as an error, which the tests of a failure allow.
/// </remarks>
[TestClass]
public class BitAiRendererBoundaryE2ETests : E2ETestBase
{
    private const string Route = "/e2e/airendererboundary";

    [TestMethod]
    public async Task Basic_ContentThatRenders_IsRenderedWithNoElementOfTheBoundarysOwn()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#rbd-ok-content")).ToHaveTextAsync("Rendered fine");
        await Expect(Page.Locator("#rbd-ok")).ToHaveCountAsync(0);
        await Expect(Page.Locator(".e2e-rbd-ok")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#basic .bit-airbd")).ToHaveCountAsync(0);
        Assert.AreEqual("rbd-ok-box", await Page.Locator("#rbd-ok-content").EvaluateAsync<string>("e => e.parentElement.id"));
        Assert.IsFalse((await Page.ContentAsync()).Contains("Never shown", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task Throwing_AFailingChild_IsReplacedByANotice_AndTheRestOfThePageLivesOn()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Page.Locator("#rbd-throw").ClickAsync();

        var notice = Page.Locator("#rbd-notice");
        await Expect(notice).ToBeVisibleAsync();
        await Expect(notice).ToHaveClassAsync(HasClass("bit-airbd"));
        await Expect(notice).ToHaveClassAsync(HasClass("e2e-rbd-notice"));
        await Expect(notice).ToHaveCSSAsync("margin-top", "3px");
        await Expect(notice).ToHaveCSSAsync("display", "block");
        await Expect(Page.Locator("#rbd-notice-content")).ToHaveCountAsync(0);

        // The notice is a tag of the error role saying which piece failed; the message, written for a developer, is the
        // title of the tag rather than its text.
        var tag = notice.Locator(".bit-tag");
        await Expect(tag).ToHaveTextAsync("The chart could not be drawn");
        await Expect(tag).ToHaveAttributeAsync("title", "The chart's series are missing.");
        await Expect(tag.Locator(".bit-tag-icn")).ToHaveClassAsync(HasClass("bit-icon--Warning"));
        await Expect(tag).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-err)"));

        // The tag is as wide as what it says, not stretched across the line its block takes.
        var (tagLeft, _, tagRight, _) = await tag.RectAsync();
        var (boxLeft, _, boxRight, _) = await Page.Locator("#rbd-notice-box").RectAsync();
        Assert.IsTrue(tagRight - tagLeft < (boxRight - boxLeft) / 2, "The tag is as wide as its text.");

        // Nothing else of the page went down with it; and a render of the page, which tries the content again, keeps
        // the notice for content that still fails.
        await Page.Locator("#rbd-sibling").ClickAsync();
        await Expect(Page.Locator("#rbd-sibling")).ToHaveTextAsync("Sibling clicked 1");
        await Expect(notice.Locator(".bit-tag")).ToHaveTextAsync("The chart could not be drawn");
        await Expect(Page.Locator("#rbd-notice-content")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());

        var errors = await GetServerErrorsAsync();
        Assert.IsFalse(errors.Any(e => e.Contains("series are missing", StringComparison.Ordinal)), string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public async Task Throwing_ARenderOfTheBoundaryByItsParent_TriesTheContentAgain_AndCatchesTheNextFailure()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Page.Locator("#rbd-throw").ClickAsync();
        await Expect(Page.Locator("#rbd-notice")).ToBeVisibleAsync();

        await Page.Locator("#rbd-throw").ClickAsync();
        await Expect(Page.Locator("#rbd-notice-content")).ToHaveTextAsync("Content rendered");
        await Expect(Page.Locator("#rbd-notice")).ToHaveCountAsync(0);

        await Page.Locator("#rbd-throw").ClickAsync();
        await Expect(Page.Locator("#rbd-notice")).ToBeVisibleAsync();
        await Expect(Page.Locator("#rbd-notice-content")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task DefaultLabel_WithoutALabel_TheNoticeSaysTheContentCouldNotBeRendered()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Expect(Page.Locator("#rbd-default-content")).ToBeVisibleAsync();
        await Page.Locator("#rbd-throw-default").ClickAsync();

        var tag = Page.Locator("#rbd-default .bit-tag");
        await Expect(tag).ToHaveTextAsync("This content could not be rendered");
        await Expect(tag).ToHaveAttributeAsync("title", "Boom without a label.");
    }

    [TestMethod]
    public async Task Inline_AFragmentThatThrowsAsItIsBuilt_IsCaughtToo_AndRecovers()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Expect(Page.Locator("#rbd-inline-content")).ToHaveTextAsync("Inline content");
        await Page.Locator("#rbd-throw-inline").ClickAsync();

        await Expect(Page.Locator("#rbd-inline .bit-tag")).ToHaveTextAsync("The inline fragment failed");
        await Expect(Page.Locator("#rbd-inline .bit-tag")).ToHaveAttributeAsync("title", "The inline fragment threw.");
        await Expect(Page.Locator("#rbd-inline-content")).ToHaveCountAsync(0);

        await Page.Locator("#rbd-throw-inline").ClickAsync();
        await Expect(Page.Locator("#rbd-inline-content")).ToHaveTextAsync("Inline content");
        await Expect(Page.Locator("#rbd-inline")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Tool_ARendererThatFailsOnPartialArguments_ShowsTheNotice_AndRecoversOnceTheyAreWhole()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Page.Locator("#rbd-tool-start").ClickAsync();

        var card = Page.Locator("#rbd-tool-card");
        await Expect(card).ToHaveClassAsync(HasClass("bit-aitcv-cst"));
        var tag = card.Locator(".bit-airbd .bit-tag");
        await Expect(tag).ToHaveTextAsync("The renderer for 'e2e_rbd_tool' failed");
        await Expect(tag).ToHaveAttributeAsync("title", "The chart arguments are incomplete.");

        // The arguments are completed off the renderer's thread, with no render of the page: the card that follows its
        // call renders the boundary again, which tries the renderer again.
        await Page.Locator("#rbd-tool-complete").ClickAsync();

        await Expect(card.Locator(".e2e-rbd-tool")).ToHaveTextAsync("Chart of {\"series\":[1,2,3]}");
        await Expect(card.Locator(".bit-airbd")).ToHaveCountAsync(0);
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Part_ARendererThatFailsOnAPartialValue_ShowsTheNotice_AndRecoversWhenThePartChanges()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        var view = Page.Locator("#rbd-part-view");
        await Expect(view.Locator(".e2e-rbd-part")).ToHaveTextAsync("Value: complete");
        await Expect(view.Locator(".bit-airbd")).ToHaveCountAsync(0);

        // The part changes off the renderer's thread, with no render of the page: its part view renders the boundary
        // again, which tries the renderer again - failing on the partial value, drawing the whole one.
        await Page.Locator("#rbd-part-break").ClickAsync();
        var tag = view.Locator(".bit-airbd .bit-tag");
        await Expect(tag).ToHaveTextAsync("The renderer for 'e2e-rbd-part' failed");
        await Expect(tag).ToHaveAttributeAsync("title", "The value is only partial.");
        await Expect(view.Locator(".e2e-rbd-part")).ToHaveCountAsync(0);

        await Page.Locator("#rbd-part-complete").ClickAsync();
        await Expect(view.Locator(".e2e-rbd-part")).ToHaveTextAsync("Value: complete");
        await Expect(view.Locator(".bit-airbd")).ToHaveCountAsync(0);

        await Page.Locator("#rbd-part-break").ClickAsync();
        await Expect(tag).ToHaveTextAsync("The renderer for 'e2e-rbd-part' failed");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task Style_StylesClassesAndDirection_LandOnTheNotice()
    {
        AllowConsoleErrors = IsWasm;
        await OpenAsync(Route);

        await Page.Locator("#rbd-throw-style").ClickAsync();

        var styled = Page.Locator("#rbd-styles");
        await Expect(styled).ToHaveCSSAsync("padding-top", "5px");
        await Expect(styled).ToHaveClassAsync(HasClass("e2e-rbd-root"));
        await Expect(styled.Locator(".bit-tag")).ToHaveTextAsync("Styled notice");

        var rtl = Page.Locator("#rbd-rtl");
        await Expect(rtl).ToHaveAttributeAsync("dir", "rtl");
        await Expect(rtl).ToHaveClassAsync(HasClass("bit-rtl"));
        var (_, _, iconRight, _) = await rtl.Locator(".bit-tag-icn").RectAsync();
        var (_, _, textRight, _) = await rtl.Locator(".bit-tag-tex").RectAsync();
        Assert.IsTrue(iconRight > textRight, "Right to left, the glyph leads the label from the right.");
        var (_, _, rtlRight, _) = await rtl.RectAsync();
        var (_, _, tagRight, _) = await rtl.Locator(".bit-tag").RectAsync();
        Assert.AreEqual(rtlRight, tagRight, 1.5, "Right to left, the tag starts at the right of its block.");
    }
}
