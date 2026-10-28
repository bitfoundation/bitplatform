namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiEmptyState at /e2e/aiemptystate in a real browser: the default heading and line of the string table, the
/// sparkle glyph of the AI font hidden from assistive technologies, the centered column, the suggestions as buttons that
/// raise OnSuggestion by pointer and keyboard and wrap in a narrow box, Title/Body/ChildContent/IconName/IconTemplate,
/// Disabled, the theme tokens, every --bit-AiEmptyState-* variable, Styles/Classes and RTL.
/// </summary>
[TestClass]
public class BitAiEmptyStateE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiemptystate";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Suggestions(string id) => Root(id).Locator(".bit-aiems-sgs > .bit-btn");

    // ---------- basic ----------

    [TestMethod]
    public async Task Basic_NothingSet_ShowsTheSparkleTheHeadingAndTheLineOfTheStringTable()
    {
        await OpenAsync(Route);

        var root = Root("ems-basic");
        await Expect(root).ToHaveClassAsync(HasClass("bit-aiems"));
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveTextAsync("How can I help?");
        await Expect(root.Locator(".bit-aiems-bdy")).ToHaveTextAsync("Ask a question to get started.");
        await Expect(root.Locator(".bit-aiems-sgs")).ToHaveCountAsync(0);

        var icon = root.Locator(".bit-aiems-ico");
        await icon.AssertAiGlyphAsync("AutoEnhanceOn");
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
    }

    [TestMethod]
    public async Task Basic_TheParts_AreACenteredColumnInTheThemeTokens()
    {
        await OpenAsync(Route);

        var root = Root("ems-basic");
        await Expect(root).ToHaveCSSAsync("flex-direction", "column");
        await Expect(root).ToHaveCSSAsync("text-align", "center");
        var box = await root.RectAsync();
        foreach (var part in new[] { ".bit-aiems-ico", ".bit-aiems-ttl", ".bit-aiems-bdy" })
        {
            var rect = await root.Locator(part).RectAsync();
            Assert.AreEqual((box.Left + box.Right) / 2, (rect.Left + rect.Right) / 2, 1.5, $"{part} is centered.");
        }

        var icon = await root.Locator(".bit-aiems-ico").RectAsync();
        var title = await root.Locator(".bit-aiems-ttl").RectAsync();
        var body = await root.Locator(".bit-aiems-bdy").RectAsync();
        Assert.IsTrue(icon.Bottom <= title.Top && title.Bottom <= body.Top, "The icon, the heading and the line are stacked in that order.");

        await Expect(root).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveCSSAsync("font-size", await root.ResolveLengthInAsync("var(--bit-tpg-fs-4xl)"));
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("font-size", await root.ResolveLengthInAsync("var(--bit-tpg-fs-xl)"));
        var weight = await root.EvaluateAsync<string>("e => { const p = document.createElement('div'); p.style.fontWeight = 'var(--bit-tpg-fw-semibold)'; e.appendChild(p); const w = getComputedStyle(p).fontWeight; p.remove(); return w; }");
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("font-weight", weight);
    }

    // ---------- suggestions ----------

    [TestMethod]
    public async Task Suggestions_Pressed_RaiseOnSuggestionWithTheirText()
    {
        await OpenAsync(Route);

        await Expect(Suggestions("ems-suggestions")).ToHaveTextAsync(["Summarize this page", "Write a haiku", "Explain render modes"]);
        await Expect(Page.GetByRole(AriaRole.Button, new() { Name = "Write a haiku" }).First).ToBeVisibleAsync();

        await Suggestions("ems-suggestions").Nth(1).ClickAsync();
        await Expect(Page.Locator("#ems-log")).ToHaveTextAsync("Write a haiku");

        await Suggestions("ems-suggestions").Nth(2).ClickAsync();
        await Expect(Page.Locator("#ems-log")).ToHaveTextAsync("Write a haiku|Explain render modes");
    }

    [TestMethod]
    public async Task Suggestions_WithTheKeyboard_TabFromOneToTheNextAndEnterOrSpaceChooses()
    {
        await OpenAsync(Route);

        await Suggestions("ems-suggestions").Nth(0).FocusAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#ems-log")).ToHaveTextAsync("Summarize this page");

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Suggestions("ems-suggestions").Nth(1)).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await Expect(Page.Locator("#ems-log")).ToHaveTextAsync("Summarize this page|Write a haiku");
    }

    [TestMethod]
    public async Task Suggestions_TheButtons_AreRoundedChipsOnTheSecondaryBackgroundThatWrapInANarrowBox()
    {
        await OpenAsync(Route);

        var first = Suggestions("ems-suggestions").First;
        await Expect(first).ToHaveClassAsync(HasClass("bit-btn-dft-sbg"));
        var height = AiE2EHelpers.ParsePx(await first.EvaluateAsync<string>("e => getComputedStyle(e).height"));
        Assert.IsTrue(AiE2EHelpers.ParsePx(await first.EvaluateAsync<string>("e => getComputedStyle(e).borderTopLeftRadius")) >= height / 2 - 0.5, "The suggestion is rounded.");

        // In a wide box they are one centered row; in a narrow one they wrap onto more rows, still centered.
        var row = await Suggestions("ems-suggestions").EvaluateAllAsync<double[]>("els => els.map(e => e.getBoundingClientRect().top)");
        Assert.AreEqual(1, row.Distinct().Count(), "One row in a wide box.");
        var narrow = await Suggestions("ems-narrow").EvaluateAllAsync<double[]>("els => els.map(e => e.getBoundingClientRect().top)");
        Assert.IsTrue(narrow.Distinct().Count() > 1, "The suggestions wrap in a narrow box.");
        var box = await Root("ems-narrow").Locator(".bit-aiems-sgs").RectAsync();
        var chip = await Suggestions("ems-narrow").Last.RectAsync();
        Assert.AreEqual((box.Left + box.Right) / 2, (chip.Left + chip.Right) / 2, 1.5, "A wrapped suggestion is centered.");
    }

    // ---------- custom ----------

    [TestMethod]
    public async Task Custom_TitleBodyAndIconName_ReplaceTheDefaults()
    {
        await OpenAsync(Route);

        var root = Root("ems-custom");
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveTextAsync("Hi, I am the docs assistant");
        await Expect(root.Locator(".bit-aiems-bdy")).ToHaveTextAsync("Ask about any component.");

        // An icon the app names is its own: the core font draws it, and the AI subset is not asked for.
        var icon = root.Locator(".bit-aiems-ico");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Robot"));
        await Expect(icon).Not.ToHaveClassAsync(HasClass("bit-icon-ai"));
        var (family, content) = await icon.GlyphAsync();
        Assert.IsFalse(family.Contains(AiE2EHelpers.AiGlyphFontFamily), family);
        Assert.AreNotEqual("none", content);
    }

    [TestMethod]
    public async Task Custom_ChildContentAndIconTemplate_TakeThePlaceOfTheLineAndTheGlyph()
    {
        await OpenAsync(Route);

        await Expect(Root("ems-child").Locator(".bit-aiems-bdy > #ems-child-content")).ToHaveTextAsync("Pick up where you left off.");
        await Expect(Root("ems-child").Locator(".bit-aiems-ttl")).ToHaveTextAsync("Welcome back");

        await Expect(Root("ems-icon-template").Locator("div.bit-aiems-ico > #ems-icon-template-content")).ToHaveTextAsync("*");
        await Expect(Root("ems-icon-template").Locator("i")).ToHaveCountAsync(0);
    }

    // ---------- disabled ----------

    [TestMethod]
    public async Task Disabled_TheSuggestions_AreDisabledAndRaiseNothing()
    {
        await OpenAsync(Route);

        await Expect(Root("ems-disabled")).ToHaveClassAsync(HasClass("bit-dis"));
        for (var i = 0; i < 3; i++)
        {
            await Expect(Suggestions("ems-disabled").Nth(i)).ToBeDisabledAsync();
        }

        await Suggestions("ems-disabled").First.ClickAsync(new() { Force = true });
        await Suggestions("ems-disabled").First.DispatchEventAsync("click");
        await Page.WaitForTimeoutAsync(300);
        await Expect(Page.Locator("#ems-disabled-log")).ToHaveTextAsync("0");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("ems-var");
        await Expect(root).ToHaveCSSAsync("row-gap", "23px");
        await Expect(root).ToHaveCSSAsync("padding-top", "31px");
        await Expect(root).ToHaveCSSAsync("padding-left", "32px");
        await Expect(root).ToHaveCSSAsync("color", "rgb(21, 22, 23)");
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveCSSAsync("color", "rgb(200, 0, 100)");
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveCSSAsync("font-size", "50px");
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("color", "rgb(31, 32, 33)");
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("font-size", "27px");
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("font-weight", "300");
        await Expect(root.Locator(".bit-aiems-sgs")).ToHaveCSSAsync("column-gap", "15px");

        var first = await Suggestions("ems-var").Nth(0).RectAsync();
        var second = await Suggestions("ems-var").Nth(1).RectAsync();
        Assert.AreEqual(15, second.Left - first.Right, 1);
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("ems-classes");
        await Expect(root).ToHaveClassAsync(HasClass("e2e-ems-root"));
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveClassAsync(HasClass("e2e-ems-title"));
        await Expect(root.Locator(".bit-aiems-ttl")).ToHaveCSSAsync("letter-spacing", "4px");
        await Expect(root.Locator(".bit-aiems-bdy")).ToHaveClassAsync(HasClass("e2e-ems-body"));
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveClassAsync(HasClass("e2e-ems-icon"));
        await Expect(root.Locator(".bit-aiems-ico")).ToHaveClassAsync(HasClass("bit-icon-ai--AutoEnhanceOn"));
        await Expect(root.Locator(".bit-aiems-sgs")).ToHaveClassAsync(HasClass("e2e-ems-sgs"));
        await Expect(Suggestions("ems-classes").First).ToHaveClassAsync(HasClass("e2e-ems-suggestion"));
        await Expect(Suggestions("ems-classes").First).ToHaveCSSAsync("outline-color", "rgb(6, 7, 8)");
    }

    [TestMethod]
    public async Task Rtl_TheSuggestions_ReadFromTheRight()
    {
        await OpenAsync(Route);

        await Expect(Root("ems-rtl")).ToHaveAttributeAsync("dir", "rtl");
        await Expect(Root("ems-rtl")).ToHaveCSSAsync("direction", "rtl");
        var first = await Suggestions("ems-rtl").Nth(0).RectAsync();
        var second = await Suggestions("ems-rtl").Nth(1).RectAsync();
        Assert.IsTrue(first.Left > second.Left, "The first suggestion is the rightmost.");
    }
}
