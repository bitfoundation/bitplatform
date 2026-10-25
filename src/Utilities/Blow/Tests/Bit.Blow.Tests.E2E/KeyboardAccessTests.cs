using Bit.Blow.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blow.Tests.E2E;

/// <summary>
/// The graph without a pointer, in a real browser: a connection made from the keyboard, validation marks a user can
/// see, a search that takes focus to its result, the context-menu key on a node, and the minimap steered by keys.
/// </summary>
[TestClass]
public sealed class KeyboardAccessTests : BlowTest
{
    private static ILocator Node(IPage page, string id) => page.Locator($".bit-blow-node[data-id=\"{id}\"]");

    private static ILocator Live(IPage page) => page.Locator(".bit-blow-a11y-live").First;

    [TestMethod]
    public async Task A_connection_is_made_with_the_connection_key_focus_and_Enter()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page, "edge");

        await Node(page, "fmt").FocusAsync();
        await page.Keyboard.PressAsync("c");
        await Expect(Live(page)).ToContainTextAsync("Connecting from Number to text, text.");
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(1);

        await Node(page, "out").FocusAsync();
        await Expect(Live(page)).ToContainTextAsync("Press Enter to connect to Display, text.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Live(page)).ToContainTextAsync("Connected Number to text to Display");
        await EventuallyAsync(async () => (await IdsAsync(page, "edge")).Length == before.Length + 1, "the edge was added");
        await Expect(page.Locator(".bit-blow-node[data-id=\"out\"] .tp-display")).ToHaveTextAsync("7");
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_refused_target_is_explained_and_Escape_abandons_the_connection()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var before = await IdsAsync(page, "edge");

        await Node(page, "n3").FocusAsync();
        await page.Keyboard.PressAsync("c");
        await Node(page, "out").FocusAsync();
        await Expect(Live(page)).ToContainTextAsync("incompatible types");

        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Escape");
        await Expect(Live(page)).ToContainTextAsync("Connection cancelled.");
        CollectionAssert.AreEquivalent(before, await IdsAsync(page, "edge"));
    }

    [TestMethod]
    public async Task An_end_of_an_edge_is_moved_with_the_connection_key_focus_and_Enter()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var path = page.Locator(".bit-blow-edge[data-id=\"e2\"] .bit-blow-edge-path");
        var before = await path.GetAttributeAsync("d");

        // The second press picks up the source end: the second number is swapped for the third.
        await page.Locator(".bit-blow-edge[data-id=\"e2\"]").FocusAsync();
        await page.Keyboard.PressAsync("c");
        await Expect(Live(page)).ToContainTextAsync("Moving the end of Edge from Number to Add at Add.");
        await page.Keyboard.PressAsync("c");
        await Expect(Live(page)).ToContainTextAsync("Moving the end of Edge from Number to Add at Number.");
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(1);

        await Node(page, "n3").FocusAsync();
        await Expect(Live(page)).ToContainTextAsync("Press Enter to connect to Number, value.");
        await page.Keyboard.PressAsync("Enter");

        await Expect(Live(page)).ToContainTextAsync("Reconnected Number to Add");
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(0);
        await EventuallyAsync(async () => await path.GetAttributeAsync("d") != before, "the edge was not redrawn from its new source");
        await Expect(page.Locator(".bit-blow-edge")).ToHaveCountAsync(3);
    }

    [TestMethod]
    public async Task Escape_leaves_an_edge_whose_end_was_picked_up_as_it_was()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);
        var path = page.Locator(".bit-blow-edge[data-id=\"e1\"] .bit-blow-edge-path");
        var before = await path.GetAttributeAsync("d");

        await page.Locator(".bit-blow-edge[data-id=\"e1\"]").FocusAsync();
        await page.Keyboard.PressAsync("c");
        await Node(page, "fmt").FocusAsync();
        await page.Keyboard.PressAsync("Escape");

        await Expect(Live(page)).ToContainTextAsync("Reconnection cancelled. The edge is unchanged.");
        await Expect(page.Locator(".bit-blow-connectionline")).ToHaveCountAsync(0);
        Assert.AreEqual(before, await path.GetAttributeAsync("d"));
    }

    [TestMethod]
    public async Task Problems_are_marked_by_shape_as_well_as_colour_and_read_with_the_node()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);

        // The display's input is required and unconnected.
        var display = Node(page, "out");
        await Expect(display).ToHaveClassAsync(new System.Text.RegularExpressions.Regex(@"\bbit-blow-error\b"));
        var badge = await display.EvaluateAsync<string>("el => getComputedStyle(el, '::after').content");
        StringAssert.Contains(badge, "!", "the error has no badge");
        await Expect(page.Locator(".bit-blow-node[data-id=\"out\"] .bit-blow-handle-error")).ToHaveCountAsync(1);

        var description = await display.EvaluateAsync<string>(
            "el => el.getAttribute('aria-describedby').split(' ').map(id => document.getElementById(id).textContent).join(' ')");
        StringAssert.StartsWith(description, "Error: ");
    }

    [TestMethod]
    public async Task The_search_box_takes_focus_to_the_node_it_finds()
    {
        var page = await OpenAsync("/examples/typed-ports");
        await WaitForEdgesAnchoredAsync(page);

        await page.GetByLabel("Find a node").FillAsync("display");
        await Expect(page.Locator(".tp-found")).ToHaveTextAsync("1 match");
        await page.GetByLabel("Find a node").PressAsync("Enter");

        await EventuallyAsync(async () => await page.EvaluateAsync<string?>("() => document.activeElement?.dataset?.id") == "out",
            "the found node took focus");
        CollectionAssert.AreEqual(new[] { "out" }, await SelectedNodesAsync(page));
    }

    [TestMethod]
    public async Task The_context_menu_key_on_a_focused_node_opens_its_menu()
    {
        var page = await OpenAsync("/interactions");
        await WaitForFlowAsync(page);
        var id = (await IdsAsync(page))[0];

        await Node(page, id).FocusAsync();
        await page.Keyboard.PressAsync("Shift+F10");

        await Expect(page.Locator(".event-log")).ToContainTextAsync($"context menu on node {id}");
    }

    [TestMethod]
    public async Task The_arrow_keys_on_the_minimap_move_the_view_and_not_the_page()
    {
        var page = await OpenAsync("/examples/interactive-minimap");
        await WaitForFlowAsync(page);
        var before = await ViewportAsync(page);
        var scrolled = await page.EvaluateAsync<double>("() => window.scrollY");

        await page.Locator(".bit-blow-minimap").FocusAsync();
        await page.Keyboard.PressAsync("ArrowRight");
        await page.Keyboard.PressAsync("ArrowDown");

        await EventuallyAsync(async () => (await ViewportAsync(page)).X < before.X - 1, "ArrowRight moved the view");
        Assert.IsLessThan(before.Y - 1, (await ViewportAsync(page)).Y, "ArrowDown moved the view");
        Assert.AreEqual(scrolled, await page.EvaluateAsync<double>("() => window.scrollY"), "the arrow keys scrolled the page");
    }
}
