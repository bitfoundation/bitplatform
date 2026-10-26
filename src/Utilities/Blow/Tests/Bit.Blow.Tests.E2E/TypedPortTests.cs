using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// Declared ports in a real browser: the typed-ports demo, a small dataflow editor, refusing a wire between
/// incompatible types with the reason shown, accepting a compatible one and recomputing its values, and
/// reporting what is still unconnected.
/// </summary>
[TestClass]
public sealed class TypedPortTests : BlowTest
{
    private static async Task<Point> PortAsync(IPage page, string nodeId, string portId) =>
        (await BoxAsync(page, $".bit-blow-node[data-id=\"{nodeId}\"] .bit-blow-handle[data-handle-id=\"{portId}\"]")).Centre;

    [TestMethod]
    public async Task A_number_wired_into_a_text_input_is_refused_and_the_reason_is_shown()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page, "edge");

        await DragAsync(page, await PortAsync(page, "n3", "value"), await PortAsync(page, "out", "text"));
        await WaitForQuietAsync(page);

        CollectionAssert.AreEquivalent(before, await IdsAsync(page, "edge"), "an incompatible connection was made");
        await Expect(page.Locator(".tp-last")).ToContainTextAsync("incompatible types");
        // Screen-reader users hear the same reason.
        await Expect(page.Locator(".bit-blow-a11y-live")).ToContainTextAsync("incompatible types");
    }

    [TestMethod]
    public async Task Wiring_the_text_output_to_the_display_completes_the_graph_and_shows_the_value()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        await Expect(page.Locator(".tp-panel")).ToContainTextAsync("must be connected");

        await DragAsync(page, await PortAsync(page, "fmt", "text"), await PortAsync(page, "out", "text"));
        await WaitForQuietAsync(page);

        // 3 + 4, computed in dependency order and carried through the formatter.
        await Expect(page.Locator(".bit-blow-node[data-id=\"out\"] .tp-display")).ToHaveTextAsync("7");
        await Expect(page.Locator(".tp-panel")).ToContainTextAsync("The graph is complete");
    }

    [TestMethod]
    public async Task An_input_that_takes_one_wire_refuses_a_second()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page, "edge");

        // op1's input "a" is already fed by n1.
        await DragAsync(page, await PortAsync(page, "n3", "value"), await PortAsync(page, "op1", "a"));
        await WaitForQuietAsync(page);

        CollectionAssert.AreEquivalent(before, await IdsAsync(page, "edge"));
        await Expect(page.Locator(".tp-last")).ToContainTextAsync("no free connections");
    }

    [TestMethod]
    public async Task A_wire_from_a_node_back_into_itself_is_refused()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page, "edge");

        await DragAsync(page, await PortAsync(page, "op1", "result"), await PortAsync(page, "op1", "b"));
        await WaitForQuietAsync(page);

        CollectionAssert.AreEquivalent(before, await IdsAsync(page, "edge"), "a node was wired to itself");
        await Expect(page.Locator(".tp-last")).ToContainTextAsync("to itself");
    }
}
