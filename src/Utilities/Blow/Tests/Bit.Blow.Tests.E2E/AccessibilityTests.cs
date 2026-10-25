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
}
