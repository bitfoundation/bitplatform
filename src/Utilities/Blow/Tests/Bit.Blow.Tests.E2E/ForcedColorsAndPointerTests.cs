using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Windows high contrast, emulated: what the forced-colours rules actually draw; and moving a node with single clicks
/// rather than a drag (WCAG 2.5.7).
/// </summary>
[TestClass]
public sealed class ForcedColorsAndPointerTests : BlowTest
{
    [TestMethod]
    public async Task In_forced_colours_selection_problems_and_run_states_keep_their_shapes_in_system_colours()
    {
        var page = await OpenAsync("/examples/workflow-runner");
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        await WaitForEdgesAnchoredAsync(page);

        await page.GetByLabel("Make \"Enrich\" fail").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "▶ Run" }).ClickAsync();
        await Expect(page.Locator(".wf-status")).ToContainTextAsync("Run finished", new() { Timeout = 20_000 });

        var styles = await page.EvaluateAsync<Dictionary<string, string>>(
            """
            () => {
              const node = id => document.querySelector(`.bit-blow-node[data-id="${id}"]`);
              const before = id => getComputedStyle(node(id), '::before');
              const probe = document.createElement('div');
              probe.style.color = 'CanvasText';
              document.body.appendChild(probe);
              const canvasText = getComputedStyle(probe).color;
              probe.remove();
              return {
                forced: String(matchMedia('(forced-colors: active)').matches),
                canvasText,
                failedBadge: before('enrich').backgroundColor,
                failedGlyph: before('enrich').content,
                skippedGlyph: before('merge').content,
              };
            }
            """);

        Assert.AreEqual("true", styles["forced"], "forced colours were not emulated");
        // The badge is drawn in the system's text colour, and its glyph - the shape - is still there.
        Assert.AreEqual(styles["canvasText"], styles["failedBadge"]);
        StringAssert.Contains(styles["failedGlyph"], "✕");
        StringAssert.Contains(styles["skippedGlyph"], "–");
    }

    [TestMethod]
    public async Task In_forced_colours_a_selected_node_has_a_solid_outline()
    {
        var page = await OpenAsync("/examples/a11y");
        await page.EmulateMediaAsync(new() { ForcedColors = ForcedColors.Active });
        await WaitForFlowAsync(page);

        await page.Locator(".bit-blow-node[data-id=\"2\"]").ClickAsync();
        await Expect(page.Locator(".bit-blow-node[data-id=\"2\"]")).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bselected\b"));

        var outline = await page.Locator(".bit-blow-node[data-id=\"2\"]").EvaluateAsync<string>(
            "el => { const s = getComputedStyle(el); return s.outlineStyle + ' ' + s.outlineWidth; }");
        Assert.AreEqual("solid 2px", outline);
    }

    [TestMethod]
    public async Task A_selected_node_moves_with_single_clicks_on_its_toolbar()
    {
        var page = await OpenAsync("/examples/a11y");
        await WaitForFlowAsync(page);
        await page.Locator(".bit-blow-node[data-id=\"2\"]").ClickAsync();
        await WaitForQuietAsync(page);
        var before = await NodeBoxAsync(page, "2");
        var zoom = (await ViewportAsync(page)).Zoom;

        await page.GetByRole(AriaRole.Button, new() { Name = "Move right" }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "Move down" }).ClickAsync();

        await EventuallyAsync(async () => Math.Abs((await NodeBoxAsync(page, "2")).Y - (before.Y + 20 * zoom)) < 0.5, "Move down moved the node");
        AssertNear(before.X + 20 * zoom, (await NodeBoxAsync(page, "2")).X, 0.5, "Move right moved the node");
        StringAssert.Contains(await page.Locator(".bit-blow-a11y-live").First.TextContentAsync(), "moved");
    }
}
