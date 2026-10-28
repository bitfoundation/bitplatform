using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiReasoningPartView at /e2e/aireasoningpartview in a real browser: a finished reasoning collapsed by
/// default under its title, the header reached with Tab and toggled with Enter, Space and the pointer, the panel open
/// with its dots while the model thinks and collapsing once it is done unless the user chose otherwise, the option that
/// keeps it open, the reasoning script played by the transport, the Lightbulb glyph of the AI font, right to left,
/// reduced motion, the --bit-AiReasoningPartView-* variables and Classes/Styles.
/// </summary>
[TestClass]
public class BitAiReasoningPartViewE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aireasoningpartview";

    private static readonly Regex ThoughtFor = new(@"^Thought for \d+(\.\d)?(ms|s)$");

    [TestMethod]
    public async Task Basic_AFinishedReasoning_IsCollapsedByDefaultUnderItsTitle()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-basic");
        var header = panel.Locator("button.bit-acd-hdr");

        await Expect(panel).ToHaveClassAsync(HasClass("bit-airpv"));
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveTextAsync("Reasoning");
        await Expect(header).ToHaveAccessibleNameAsync(new Regex("Reasoning"));

        // Collapsed, the reasoning is not in the page at all, so it neither competes with the answer nor is read out.
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveCountAsync(0);
        await Expect(panel.Locator(".bit-ldn")).ToHaveCountAsync(0);

        // A part that streamed and finished says how long the model thought for.
        await Expect(Page.Locator("#rpv-timed .bit-airpv-ttl")).ToHaveTextAsync(ThoughtFor);
        await Expect(Page.Locator("#rpv-timed button.bit-acd-hdr")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Keyboard_TheHeader_IsReachedWithTabAndOpensAndClosesWithEnterAndSpace()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-basic");
        var header = panel.Locator("button.bit-acd-hdr");

        await Page.Locator("#rpv-before").FocusAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(header).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");

        // The reasoning is Markdown, in the region the header controls.
        var body = panel.Locator(".bit-airpv-bdy");
        await Expect(body).ToBeVisibleAsync();
        await Expect(body.Locator("strong")).ToHaveTextAsync("render modes");
        await Expect(body.Locator("li.bit-aimdv-itm")).ToHaveTextAsync(["Server keeps the state", "WebAssembly runs offline"]);
        var region = Page.Locator($"#{await header.GetAttributeAsync("aria-controls")}");
        await Expect(region).ToHaveAttributeAsync("role", "region");
        await Expect(region.Locator(".bit-airpv-bdy")).ToHaveCountAsync(1);

        await Page.Keyboard.PressAsync(" ");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(body).ToHaveCountAsync(0);
        await Expect(header).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Enter");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(body).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Pointer_ClickingTheHeader_OpensAndClosesThePanel()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-basic");
        var header = panel.Locator("button.bit-acd-hdr");

        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel.Locator(".bit-airpv-bdy")).ToBeVisibleAsync();

        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveCountAsync(0);
    }

    // ---------- streaming ----------

    [TestMethod]
    public async Task Streaming_WhileTheModelThinks_ThePanelIsOpenWithItsDotsAndCollapsesOnceDone()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-stream");
        var header = panel.Locator("button.bit-acd-hdr");

        // Open while streaming, so the user sees progress rather than a spinner.
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveTextAsync("Reasoning");
        var dots = panel.Locator(".bit-airpv-hdr .bit-ldn");
        await Expect(dots).ToBeVisibleAsync();
        // The dots say what the title says already, from inside the button: a picture, not a status of their own.
        await Expect(dots).ToHaveAttributeAsync("role", "none");
        await Expect(dots).Not.ToHaveAttributeAsync("aria-live", new Regex(".+"));

        await AppendAsync(1);
        var body = panel.Locator(".bit-airpv-bdy");
        await Expect(body).ToHaveTextAsync("First, the user wants to know where the state lives.");
        await Expect(body.Locator(".bit-aimdv-crt")).ToHaveCountAsync(1);

        await AppendAsync(2);
        await Expect(body).ToHaveTextAsync("First, the user wants to know where the state lives. Then, what happens when the circuit drops.");

        await Page.Locator("#rpv-complete").ClickAsync();

        // Done: it collapses so it does not compete with the answer, and says how long the model thought.
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(body).ToHaveCountAsync(0);
        await Expect(dots).ToHaveCountAsync(0);
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveTextAsync(ThoughtFor);
    }

    [TestMethod]
    public async Task Streaming_TheUsersOwnChoice_WinsOverTheCollapseAtTheEnd()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-stream-user");
        var header = panel.Locator("button.bit-acd-hdr");
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");

        // The user closes it and opens it again while it streams: they want to watch it.
        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "false");
        await header.ClickAsync();
        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");

        await AppendAsync(1);
        await Page.Locator("#rpv-complete").ClickAsync();
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveTextAsync(ThoughtFor);

        await Expect(header).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveTextAsync("First, the user wants to know where the state lives.");
        await Expect(panel.Locator(".bit-aimdv-crt")).ToHaveCountAsync(0);

        // The other panel, left alone, collapsed.
        await Expect(Page.Locator("#rpv-stream button.bit-acd-hdr")).ToHaveAttributeAsync("aria-expanded", "false");
    }

    [TestMethod]
    public async Task Options_CollapseReasoningOff_KeepsAFinishedReasoningOpen()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#rpv-open button.bit-acd-hdr")).ToHaveAttributeAsync("aria-expanded", "true");
        await Expect(Page.Locator("#rpv-open .bit-airpv-bdy strong")).ToHaveTextAsync("render modes");
    }

    [TestMethod]
    public async Task Transport_TheReasoningScript_StreamsTheThinkingInOpenThenCollapsesForTheAnswer()
    {
        await OpenAsync(Route);

        // How long the text of the open panel is at every change: zero while it is collapsed or not there yet.
        await Page.RecordTextLengthsAsync("rpv-live", "#rpv-live .bit-airpv-bdy");

        await Page.Locator("#rpv-send").ClickAsync();
        await Expect(Page.Locator("#rpv-live-state")).ToHaveTextAsync("Complete");

        var panel = Page.Locator("#rpv-live-panel");
        await Expect(panel.Locator("button.bit-acd-hdr")).ToHaveAttributeAsync("aria-expanded", "false");
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveTextAsync(ThoughtFor);
        await Expect(Page.Locator("#rpv-live-answer")).ToHaveTextAsync("Two costs are worth separating: the connection, and the generation.");

        // The thinking was shown open and growing word by word before the panel collapsed.
        var lengths = await Page.TextLengthsAsync("rpv-live");
        var open = lengths.Where(l => l > 0).ToArray();
        Assert.IsTrue(open.Length >= 5, $"The reasoning was seen streaming in: {string.Join(",", lengths)}.");
        Assert.IsTrue(open.Max() >= "The user asks about the cost of a round trip.".Length, $"The reasoning was seen in full: {string.Join(",", lengths)}.");
        Assert.AreEqual(0, lengths[^1], "The panel collapsed at the end.");

        // Opened again, it holds the whole of it.
        await panel.Locator("button.bit-acd-hdr").ClickAsync();
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveTextAsync("The user asks about the cost of a round trip. I should separate the fixed cost from the per-token cost.");
    }

    // ---------- glyph, rtl, motion ----------

    [TestMethod]
    public async Task Glyph_TheHeaderIcon_IsTheLightbulbOfTheAiFont()
    {
        await OpenAsync(Route);

        var icon = Page.Locator("#rpv-basic .bit-airpv-ico");
        await icon.AssertAiGlyphAsync("Lightbulb");
        await Expect(icon).ToBeVisibleAsync();
        Assert.IsTrue(await Page.AiGlyphFontLoadsAsync(""), "The AI icon font is fetched and draws the glyph.");
    }

    [TestMethod]
    public async Task Rtl_TheHeaderOfARightToLeftPanel_LeadsWithItsChevronAtTheRight()
    {
        await OpenAsync(Route);

        foreach (var id in new[] { "rpv-rtl", "rpv-rtl-inherited" })
        {
            var chevron = await Page.Locator($"#{id} .bit-acd-eiw").RectAsync();
            var title = await Page.Locator($"#{id} .bit-airpv-ttl").RectAsync();
            var icon = await Page.Locator($"#{id} .bit-airpv-ico").RectAsync();
            Assert.IsTrue(chevron.Left > title.Left, $"{id}: the chevron is at the right of the title.");
            Assert.IsTrue(icon.Left > title.Left, $"{id}: the icon comes before the title, from the right.");
        }

        var ltrChevron = await Page.Locator("#rpv-ltr .bit-acd-eiw").RectAsync();
        var ltrTitle = await Page.Locator("#rpv-ltr .bit-airpv-ttl").RectAsync();
        Assert.IsTrue(ltrChevron.Left < ltrTitle.Left, "Left to right, the chevron leads at the left.");
        await Expect(Page.Locator("#rpv-rtl")).ToHaveAttributeAsync("dir", "rtl");
    }

    [TestMethod]
    public async Task ReducedMotion_Requested_SlowsTheDotsOfAStreamingPanel()
    {
        await OpenAsync(Route);

        var dot = Page.Locator("#rpv-stream .bit-ldn-bnd-ch1");
        Assert.AreEqual(850, await dot.AnimationDurationAsync("bit-ldn-bnd-anm") ?? 0, 1);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });
        await AiE2EHelpers.PollAsync(async () => Math.Abs((await dot.AnimationDurationAsync("bit-ldn-bnd-anm") ?? 0) - 2550) <= 1,
            "Under reduced motion the dots keep moving, three times slower.");
    }

    // ---------- style ----------

    [TestMethod]
    public async Task Variables_ThePublicVariables_RestyleTheReasoningAndCapItsHeight()
    {
        await OpenAsync(Route);

        await Page.Locator("#rpv-vars button.bit-acd-hdr").ClickAsync();
        var body = Page.Locator("#rpv-vars .bit-airpv-bdy");
        await Expect(body).ToBeVisibleAsync();
        await Expect(body).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(body).ToHaveCSSAsync("font-size", "11px");
        await Expect(body).ToHaveCSSAsync("max-height", "60px");
        await Expect(body).ToHaveCSSAsync("overflow-y", "auto");

        // Long reasoning scrolls inside the panel rather than pushing the answer away.
        var sizes = await body.EvaluateAsync<double[]>("e => [e.scrollHeight, e.clientHeight]");
        Assert.IsTrue(sizes[0] > sizes[1], "The reasoning scrolls inside the panel.");
        Assert.IsTrue(sizes[1] <= 60.5);

        await Page.Locator("#rpv-defaults button.bit-acd-hdr").ClickAsync();
        var defaults = Page.Locator("#rpv-defaults .bit-airpv-bdy");
        await Expect(defaults).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(defaults).ToHaveCSSAsync("font-size", await defaults.ResolveLengthInAsync("var(--bit-tpg-fs-sm)"));
    }

    [TestMethod]
    public async Task StyleAndClass_ClassesAndStyles_ReachTheirParts()
    {
        await OpenAsync(Route);

        var panel = Page.Locator("#rpv-parts");
        await Expect(panel).ToHaveClassAsync(HasClass("e2e-rpv-class"));
        await Expect(panel).ToHaveClassAsync(HasClass("e2e-rpv-root"));
        await Expect(panel).ToHaveCSSAsync("outline-color", "rgb(4, 5, 6)");
        await Expect(panel.Locator(".bit-airpv-hdr")).ToHaveClassAsync(HasClass("e2e-rpv-header"));
        await Expect(panel.Locator(".bit-airpv-ico")).ToHaveClassAsync(HasClass("e2e-rpv-icon"));
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveClassAsync(HasClass("e2e-rpv-title"));
        await Expect(panel.Locator(".bit-airpv-ttl")).ToHaveCSSAsync("letter-spacing", "3px");

        await panel.Locator("button.bit-acd-hdr").ClickAsync();
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveClassAsync(HasClass("e2e-rpv-body"));
        await Expect(panel.Locator(".bit-airpv-bdy")).ToHaveCSSAsync("padding-top", "9px");
        await Expect(panel.Locator(".bit-aimdv-par").First).ToHaveClassAsync(HasClass("e2e-rpv-paragraph"));
    }

    private async Task AppendAsync(int sentences)
    {
        await Page.Locator("#rpv-append").ClickAsync();
        await Expect(Page.Locator("#rpv-sentences")).ToHaveTextAsync(sentences.ToString(System.Globalization.CultureInfo.InvariantCulture));
    }
}
