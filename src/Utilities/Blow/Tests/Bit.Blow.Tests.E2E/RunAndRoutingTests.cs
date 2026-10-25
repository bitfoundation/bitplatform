using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Running a workflow, stepping a state machine and routing edges round nodes, in a real browser - through the demos
/// that show them.
/// </summary>
[TestClass]
public sealed class RunAndRoutingTests : BlowTest
{
    private static ILocator Node(IPage page, string id) => page.Locator($".bit-blow-node[data-id=\"{id}\"]");

    private static System.Text.RegularExpressions.Regex Class(string name) => new($@"\b{name}\b");

    [TestMethod]
    public async Task A_run_marks_each_step_and_a_failure_skips_what_follows()
    {
        var page = await OpenAsync("/examples/workflow-runner");
        await WaitForEdgesAnchoredAsync(page);

        await page.GetByLabel("Make \"Enrich\" fail").CheckAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "▶ Run" }).ClickAsync();

        await Expect(page.Locator(".wf-status")).ToContainTextAsync("Run finished", new() { Timeout = 20_000 });
        await Expect(Node(page, "fetch")).ToHaveClassAsync(Class("bit-blow-run-succeeded"));
        await Expect(Node(page, "validate")).ToHaveClassAsync(Class("bit-blow-run-succeeded"));
        await Expect(Node(page, "enrich")).ToHaveClassAsync(Class("bit-blow-run-failed"));
        await Expect(Node(page, "merge")).ToHaveClassAsync(Class("bit-blow-run-skipped"));
        await Expect(Node(page, "store")).ToHaveClassAsync(Class("bit-blow-run-skipped"));

        // The failure is shown by shape as well as colour, and read with the node.
        var badge = await Node(page, "enrich").EvaluateAsync<string>("el => getComputedStyle(el, '::before').content");
        StringAssert.Contains(badge, "✕");
        var description = await Node(page, "enrich").EvaluateAsync<string>(
            "el => el.getAttribute('aria-describedby').split(' ').map(id => document.getElementById(id).textContent).join(' ')");
        StringAssert.StartsWith(description, "Failed: The enrichment service timed out");
    }

    [TestMethod]
    public async Task A_state_machine_moves_with_its_events()
    {
        var page = await OpenAsync("/examples/state-machine");
        await WaitForEdgesAnchoredAsync(page);
        await Expect(Node(page, "idle")).ToHaveClassAsync(Class("bit-blow-run-running"));

        await page.GetByRole(AriaRole.Button, new() { Name = "fetch", Exact = true }).ClickAsync();
        await page.GetByRole(AriaRole.Button, new() { Name = "reject", Exact = true }).ClickAsync();

        await Expect(Node(page, "error")).ToHaveClassAsync(Class("bit-blow-run-running"));
        await Expect(Node(page, "idle")).Not.ToHaveClassAsync(Class("bit-blow-run-running"));
        await Expect(page.Locator(".bit-blow-a11y-live").First).ToContainTextAsync("reject: now Error.");
        await Expect(page.Locator(".bit-blow-edge[data-id=\"loading-reject\"]")).ToHaveClassAsync(Class("animated"));
    }

    [TestMethod]
    public async Task A_routed_edge_keeps_clear_of_the_block_in_its_way_and_follows_it_when_dragged()
    {
        var page = await OpenAsync("/examples/edge-routing");
        await WaitForEdgesAnchoredAsync(page);

        // Every point along the route from Input A to Transform stays outside the first block.
        async Task<bool> ClearOfBlock()
        {
            return await page.EvaluateAsync<bool>(
                """
                () => {
                  const path = document.querySelector('.bit-blow-edge[data-id="a-mid"] .bit-blow-edge-path');
                  const block = document.querySelector('.bit-blow-node[data-id="block-1"]').getBoundingClientRect();
                  const m = path.getScreenCTM(), length = path.getTotalLength();
                  for (let d = 0; d <= length; d += 2) {
                    const p = path.getPointAtLength(d).matrixTransform(m);
                    if (p.x > block.left && p.x < block.right && p.y > block.top && p.y < block.bottom) return false;
                  }
                  return true;
                }
                """);
        }

        Assert.IsTrue(await ClearOfBlock(), "the routed edge runs through the block");
        var before = await page.Locator(".bit-blow-edge[data-id=\"a-mid\"] .bit-blow-edge-path").GetAttributeAsync("d");

        var block = await NodeBoxAsync(page, "block-1");
        await DragAsync(page, block.Centre, block.Centre.Offset(0, 60));
        await WaitForQuietAsync(page);

        Assert.AreNotEqual(before, await page.Locator(".bit-blow-edge[data-id=\"a-mid\"] .bit-blow-edge-path").GetAttributeAsync("d"),
            "the route did not follow the block");
        Assert.IsTrue(await ClearOfBlock(), "after the drag the routed edge runs through the block");
    }

    [TestMethod]
    public async Task The_graph_exports_as_an_svg_image()
    {
        var page = await OpenAsync("/examples/edge-routing");
        await WaitForEdgesAnchoredAsync(page);

        var href = await page.Locator("a[download=\"routing.svg\"]").GetAttributeAsync("href");
        StringAssert.StartsWith(href, "data:image/svg+xml;base64,");
        var svg = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(href!["data:image/svg+xml;base64,".Length..]));
        var doc = System.Xml.Linq.XDocument.Parse(svg);
        XNamespaceCheck(doc);
        StringAssert.Contains(svg, ">Transform</text>");
    }

    private static void XNamespaceCheck(System.Xml.Linq.XDocument doc)
        => Assert.AreEqual("http://www.w3.org/2000/svg", doc.Root!.Name.NamespaceName);
}
