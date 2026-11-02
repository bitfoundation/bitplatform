namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiThinkingIndicator at /e2e/aithinkingindicator in a real browser: three bouncing dots and a label, the
/// label a polite live region, drawn at the size of the text; disabled, reduced motion (slowed, never stopped) and
/// ForceAnimation, right to left, the --bit-AiThinkingIndicator-* variables and Classes/Styles; and in a chat message,
/// shown while the reply has produced nothing yet - or its status line - and gone once text streams in.
/// </summary>
[TestClass]
public class BitAiThinkingIndicatorE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aithinkingindicator";

    [TestMethod]
    public async Task Basic_TheIndicator_BouncesThreeDotsBesideALabelAnnouncedPolitely()
    {
        await OpenAsync(Route);

        var indicator = Page.Locator("#thi-basic");
        await Expect(indicator).ToHaveClassAsync(HasClass("bit-aithi"));
        await Expect(indicator).ToHaveCSSAsync("display", "inline-flex");
        await Expect(indicator).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(indicator).ToHaveCSSAsync("font-size", await indicator.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));

        // The label is what a screen reader hears, politely, so it does not interrupt the user.
        var status = indicator.GetByRole(AriaRole.Status);
        await Expect(status).ToHaveCountAsync(1);
        await Expect(status).ToHaveAttributeAsync("aria-live", "polite");
        await Expect(status).ToHaveTextAsync("Thinking");
        await Expect(indicator.Locator(".bit-ldn-ccn")).ToHaveAttributeAsync("aria-hidden", "true");

        // Three dots, bouncing, at the size of the text.
        var dots = indicator.Locator(".bit-ldn-bnd-chl");
        await Expect(dots).ToHaveCountAsync(3);
        for (var i = 0; i < 3; i++)
        {
            Assert.AreEqual(850, await dots.Nth(i).AnimationDurationAsync("bit-ldn-bnd-anm") ?? 0, 1, $"Dot {i + 1} bounces.");
        }
        await Expect(dots.First).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-pri)"));
        var fontSize = AiE2EHelpers.ParsePx(await indicator.EvaluateAsync<string>("e => getComputedStyle(e).fontSize"));
        Assert.AreEqual(fontSize, (await indicator.Locator(".bit-ldn-ccn").BoundingBoxAsync())!.Width, 0.5, "The dots are drawn at 1em.");

        await Expect(Page.Locator("#thi-label").GetByRole(AriaRole.Status)).ToHaveTextAsync("Searching the documentation");
    }

    [TestMethod]
    public async Task Disabled_ADisabledIndicator_HoldsItsDotsStillAndDims()
    {
        await OpenAsync(Route);

        var loader = Page.Locator("#thi-disabled .bit-ldn");
        await Expect(loader).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(loader).ToHaveCSSAsync("opacity", await Page.EvaluateAsync<string>(
            "() => { const p = document.createElement('div'); p.style.opacity = 'var(--bit-opa-dis)'; document.body.appendChild(p); const o = getComputedStyle(p).opacity; p.remove(); return o; }"));
        await Expect(loader.Locator(".bit-ldn-bnd-chl").First).ToHaveCSSAsync("animation-play-state", "paused");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_SlowsTheDotsRatherThanStoppingThemUnlessForceAnimationIsSet()
    {
        await OpenAsync(Route);
        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        // Stopping a loader outright would drop the only sign that work is going on, so it goes three times slower.
        var dot = Page.Locator("#thi-basic .bit-ldn-bnd-ch1");
        await AiE2EHelpers.PollAsync(async () => Math.Abs((await dot.AnimationDurationAsync("bit-ldn-bnd-anm") ?? 0) - 2550) <= 1,
            "Under reduced motion the dots keep bouncing, three times slower.");

        Assert.AreEqual(850, await Page.Locator("#thi-fam .bit-ldn-bnd-ch1").AnimationDurationAsync("bit-ldn-bnd-anm") ?? 0, 1,
            "ForceAnimation keeps the full pace.");
    }

    [TestMethod]
    public async Task Rtl_ARightToLeftIndicator_MirrorsTheDotsAndTheLabel()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#thi-rtl")).ToHaveAttributeAsync("dir", "rtl");

        var ltrLabelFirst = await LabelIsLeftOfTheDotsAsync("thi-ltr");

        foreach (var id in new[] { "thi-rtl", "thi-rtl-inherited" })
        {
            await Expect(Page.Locator($"#{id} .bit-ldn")).ToHaveCSSAsync("direction", "rtl");
            Assert.AreEqual(!ltrLabelFirst, await LabelIsLeftOfTheDotsAsync(id), $"{id}: the label and the dots swap sides.");
        }
    }

    [TestMethod]
    public async Task Variables_ThePublicVariables_RestyleTheLabelAndTheDotsFollowItsSize()
    {
        await OpenAsync(Route);

        var indicator = Page.Locator("#thi-vars");
        await Expect(indicator).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(indicator).ToHaveCSSAsync("font-size", "24px");
        Assert.AreEqual(24, (await indicator.Locator(".bit-ldn-ccn").BoundingBoxAsync())!.Width, 0.5);
        await Expect(indicator.Locator(".bit-ldn-bnd-chl").First).ToHaveCSSAsync("background-color", "rgb(4, 5, 6)");
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheRootAndTheLoader()
    {
        await OpenAsync(Route);

        var indicator = Page.Locator("#thi-parts");
        await Expect(indicator).ToHaveClassAsync(HasClass("e2e-thi-class"));
        await Expect(indicator).ToHaveClassAsync(HasClass("e2e-thi-root"));
        await Expect(indicator).ToHaveCSSAsync("outline-color", "rgb(7, 8, 9)");
        await Expect(indicator).ToHaveCSSAsync("padding-top", "6px");
        await Expect(indicator.Locator(".bit-ldn")).ToHaveClassAsync(HasClass("e2e-thi-loading"));
        await Expect(indicator.Locator(".bit-ldn-lbl")).ToHaveClassAsync(HasClass("e2e-thi-label"));
        await Expect(indicator.Locator(".bit-ldn-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
    }

    // ---------- in a chat message ----------

    [TestMethod]
    public async Task Chat_WaitingForTheFirstToken_ShowsTheIndicatorUntilTextStreamsIn()
    {
        await OpenAsync(Route);

        await Page.Locator("#thi-send").ClickAsync();

        var reply = Page.Locator("#thi-list article[data-role='assistant']");
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "true");

        // Nothing has been written yet: the reply says it is thinking.
        var indicator = reply.Locator(".bit-aithi");
        await Expect(indicator).ToBeVisibleAsync();
        await Expect(indicator.GetByRole(AriaRole.Status)).ToHaveTextAsync("Thinking");
        await Expect(reply.Locator(".bit-aitpv")).ToHaveCountAsync(0);

        // The first words arrive: the text takes its place while the rest is still on its way.
        await Expect(reply.Locator(".bit-aitpv")).ToContainTextAsync("You");
        await Expect(indicator).ToHaveCountAsync(0);
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "true");

        await Expect(Page.Locator("#thi-state")).ToHaveTextAsync("Complete");
        await Expect(reply.Locator(".bit-aitpv")).ToHaveTextAsync("You said: hello there");
        await Expect(indicator).ToHaveCountAsync(0);
        await Expect(reply).ToHaveAttributeAsync("aria-busy", "false");
    }

    [TestMethod]
    public async Task Chat_AStatusLine_IsTheLabelOfTheIndicatorUntilTheAnswerStreamsIn()
    {
        await OpenAsync(Route);

        await Page.Locator("#thi-send-status").ClickAsync();

        var reply = Page.Locator("#thi-status-list article[data-role='assistant']");
        var indicator = reply.Locator(".bit-aithi");

        await Expect(indicator.GetByRole(AriaRole.Status)).ToHaveTextAsync("Searching the documentation");

        // The status is cleared before the answer: until its first words arrive, the reply is still thinking.
        await Expect(indicator.GetByRole(AriaRole.Status)).ToHaveTextAsync("Thinking");

        await Expect(reply.Locator(".bit-aitpv")).ToContainTextAsync("I");
        await Expect(indicator).ToHaveCountAsync(0);

        await Expect(Page.Locator("#thi-status-state")).ToHaveTextAsync("Complete");
        await Expect(reply.Locator(".bit-aitpv")).ToHaveTextAsync("I found what you were looking for.");
        await Expect(indicator).ToHaveCountAsync(0);
    }

    // Whether the label of an indicator is laid out to the left of its dots.
    private async Task<bool> LabelIsLeftOfTheDotsAsync(string id)
    {
        var label = await Page.Locator($"#{id} .bit-ldn-lbl").RectAsync();
        var dots = await Page.Locator($"#{id} .bit-ldn-ccn").RectAsync();

        return label.Left < dots.Left;
    }
}
