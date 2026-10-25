using System.Text.RegularExpressions;
using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// What a user with a coarse pointer, a zoomed-out view, a keyboard or a screen reader actually gets: hit areas that
/// keep their size on screen at any zoom, the keyboard's own context-menu key reaching a node, and the descriptions
/// a screen reader reads out resolving to real text in the page.
/// </summary>
[TestClass]
public sealed class AccessibilityTests : BlowTest
{
    /// <summary>Zooms the first flow out to its minimum about the middle of the pane.</summary>
    private static async Task ZoomOutFullyAsync(IPage page)
    {
        var pane = await PaneBoxAsync(page);
        for (var i = 0; i < 12; i++) await WheelAsync(page, new Point(pane.X + pane.Width / 2, pane.Y + pane.Height / 2), 400);
        await WaitForQuietAsync(page);
    }

    [TestMethod]
    public async Task Zoomed_out_a_press_beside_a_handle_still_starts_a_connection()
    {
        var page = await OpenAsync("/basic-flow");
        await WaitForEdgesAnchoredAsync(page);
        await ZoomOutFullyAsync(page);
        Assert.IsLessThan(0.35, (await ViewportAsync(page)).Zoom, "the flow did not zoom out");

        // At this zoom the handle's dot is about two pixels across. A press four pixels to its side misses the dot by
        // a wide margin, and only lands on the handle because its hit area keeps its size on screen.
        var source = await HandleCentreAsync(page, "3", "source");
        var target = await HandleCentreAsync(page, "4", "target");
        await DragAsync(page, new Point(source.X + 4, source.Y), target);
        await WaitForQuietAsync(page);

        CollectionAssert.Contains(await IdsAsync(page, "edge"), "bit-blow-3--4", "the press beside the handle did not connect");
    }

    [TestMethod]
    public async Task Zoomed_out_a_click_beside_an_edge_still_selects_it()
    {
        var page = await OpenAsync("/basic-flow");
        await WaitForEdgesAnchoredAsync(page);
        await ZoomOutFullyAsync(page);

        // A point on the path, and the direction across it there.
        var probe = await page.EvaluateAsync<double[]>(
            """
            () => {
              const p = document.querySelector('.bit-blow-edge[data-id="e1-3"] > .bit-blow-edge-path');
              const m = p.getScreenCTM(), len = p.getTotalLength();
              const a = p.getPointAtLength(len / 2).matrixTransform(m);
              const b = p.getPointAtLength(len / 2 + 1).matrixTransform(m);
              const dx = b.x - a.x, dy = b.y - a.y, d = Math.hypot(dx, dy) || 1;
              return [a.x, a.y, -dy / d, dx / d];
            }
            """);

        // Five screen pixels off the line: inside a 14px hit path held at that size on screen, and far outside one
        // that had shrunk with the zoom.
        await page.Mouse.ClickAsync((float)(probe[0] + probe[2] * 5), (float)(probe[1] + probe[3] * 5));

        await Expect(page.Locator(".bit-blow-edge[data-id=\"e1-3\"]")).ToHaveClassAsync(new Regex(@"\bselected\b"));
    }

    [TestMethod]
    public async Task The_keyboard_context_menu_key_reaches_a_focused_node()
    {
        E2EEnvironment.RequireKeyboardContextMenu();

        var page = await OpenAsync("/interactions");
        var node = page.Locator(".bit-blow-node").First;
        var id = await node.GetAttributeAsync("data-id");

        await node.FocusAsync();
        await page.Keyboard.PressAsync("Shift+F10");

        await Expect(page.Locator(".event-log")).ToContainTextAsync($"context menu on node {id}");
    }

    [TestMethod]
    public async Task Every_description_a_node_edge_or_pane_points_at_exists_and_says_something()
    {
        var page = await OpenAsync("/basic-flow");

        var problems = await page.EvaluateAsync<string[]>(
            """
            () => [...document.querySelectorAll('.bit-blow [aria-describedby], .bit-blow[aria-describedby]')]
              .flatMap(el => el.getAttribute('aria-describedby').split(' ')
                .filter(id => !(document.getElementById(id)?.textContent.trim()))
                .map(id => `${el.className} -> ${id}`))
            """);

        Assert.IsEmpty(problems, "dangling or empty descriptions: " + string.Join(", ", problems));
        Assert.IsGreaterThan(0, await page.Locator(".bit-blow-node[aria-describedby]").CountAsync());
    }

    /// <summary>Every routable page of the demo.</summary>
    public static IEnumerable<object[]> DemoRoutes => DemoPagesTests.DemoRoutes;

    /// <summary>
    /// What a screen reader is given for everything a keyboard can reach in a flow, on every demo page: the browser's
    /// own accessibility tree, read through the DevTools protocol - the role, name and description it computed, not
    /// the attributes they were computed from. Every reachable element has a role that says what it is and a name;
    /// none is hidden from assistive technology; one with a description it points at gets that description; and each
    /// flow has its live region. No substitute for listening with a screen reader, but it covers every page.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(DemoRoutes))]
    public async Task Everything_a_keyboard_reaches_in_a_flow_has_a_role_and_a_name_for_a_screen_reader(string path)
    {
        if (E2EEnvironment.IsChromium is false) Assert.Inconclusive("The accessibility tree is read through the DevTools protocol, which only Chromium has.");

        var page = await (await NewContextAsync()).NewPageAsync();
        await GotoAsync(page, path);
        await WaitForFlowAsync(page);
        var flows = await page.Locator(".bit-blow").CountAsync();
        if (flows == 0) return;
        await WaitForQuietAsync(page);

        // The elements to look at, tagged so the protocol can find them: visible, and reachable with Tab.
        var tagged = await page.EvaluateAsync<string[]>(
            """
            () => {
              const reachable = '[tabindex]:not([tabindex="-1"]), button:not([disabled]), a[href], input:not([disabled]), select, textarea';
              const found = [...document.querySelectorAll('.bit-blow')]
                .flatMap(flow => [flow, ...flow.querySelectorAll(reachable)])
                .filter(el => el.getClientRects().length > 0 && getComputedStyle(el).visibility !== 'hidden')
                .slice(0, 120);
              return found.map((el, i) => {
                el.setAttribute('data-ax-audit', String(i));
                return `${el.tagName.toLowerCase()}.${(el.getAttribute('class') || '').trim().split(/\s+/).slice(0, 2).join('.')}`
                  + (el.dataset.id ? `[${el.dataset.id}]` : '') + (el.hasAttribute('aria-describedby') ? ' (described)' : '');
              });
            }
            """);
        Assert.IsGreaterThan(0, tagged.Length, $"{path}: a flow with nothing reachable");

        var cdp = await page.Context.NewCDPSessionAsync(page);
        await cdp.SendAsync("DOM.enable");
        await cdp.SendAsync("Accessibility.enable");
        var root = (await cdp.SendAsync("DOM.getDocument", new() { ["depth"] = 0 }))!.Value.GetProperty("root").GetProperty("nodeId").GetInt32();

        var problems = new List<string>();
        for (var i = 0; i < tagged.Length; i++)
        {
            var nodeId = (await cdp.SendAsync("DOM.querySelector", new() { ["nodeId"] = root, ["selector"] = $"[data-ax-audit='{i}']" }))!
                .Value.GetProperty("nodeId").GetInt32();
            var ax = (await cdp.SendAsync("Accessibility.getPartialAXTree", new() { ["nodeId"] = nodeId, ["fetchRelatives"] = false }))!
                .Value.GetProperty("nodes")[0];

            string Value(string property) => ax.TryGetProperty(property, out var p) && p.TryGetProperty("value", out var v)
                ? v.ToString().Trim()
                : "";

            var element = tagged[i];
            if (ax.TryGetProperty("ignored", out var ignored) && ignored.GetBoolean())
            {
                problems.Add($"{element}: reachable with Tab but hidden from assistive technology");
                continue;
            }

            var role = Value("role");
            if (role is "" or "generic" or "none" or "presentation" or "GenericContainer")
                problems.Add($"{element}: no role that says what it is ({(role == "" ? "none" : role)})");
            if (Value("name") == "") problems.Add($"{element}: no accessible name");
            if (element.EndsWith(" (described)", StringComparison.Ordinal) && Value("description") == "")
                problems.Add($"{element}: points at a description that comes out empty");
        }

        var liveRegions = await page.Locator(".bit-blow .bit-blow-a11y-live[role=status][aria-live]").CountAsync();
        if (liveRegions < flows) problems.Add($"{flows} flow(s) but {liveRegions} live region(s)");

        Assert.IsEmpty(problems, $"{path}:\n  " + string.Join("\n  ", problems));
    }
}
