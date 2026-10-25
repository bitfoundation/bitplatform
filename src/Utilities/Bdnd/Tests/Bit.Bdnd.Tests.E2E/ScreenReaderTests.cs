using System.Text.Json;
using System.Text.RegularExpressions;
using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// What a screen reader is given: a keyboard source keeps its own role and is described by the instructions, a drag
/// handle is a button named after its item, the live region is an assertive, atomic status that the drag's moves are
/// written to, and the copy that follows the pointer is hidden from it. Checked on every engine with the accessible
/// name and description as the specification computes them, and in Chromium in the browser's own accessibility tree -
/// the one a screen reader reads. A listening pass with real screen readers is still a person's (see the README).
/// </summary>
[TestClass]
public sealed class ScreenReaderTests : BdndTest
{
    private static readonly Regex Instructions = new("^To pick up, press Space or Enter\\. While dragging, use the arrow keys");

    private static ILocator Card(IPage page, string title) => page.Locator("article.kanban-card", new() { HasText = title });

    [TestMethod]
    public async Task A_keyboard_source_keeps_its_role_and_is_described_by_the_instructions()
    {
        var page = await OpenAsync("/accessibility/keyboard");
        var card = Card(page, "Write the docs");

        await Expect(card).ToHaveRoleAsync(AriaRole.Article);
        await Expect(card).ToHaveAttributeAsync("tabindex", "0");
        await Expect(card).ToHaveAccessibleDescriptionAsync(Instructions);
    }

    [TestMethod]
    public async Task A_drag_handle_is_a_focusable_button_described_by_the_instructions()
    {
        var page = await OpenAsync("/customize/handles");
        var handle = page.Locator("[data-bit-bdnd-handle]").First;

        await Expect(handle).ToHaveRoleAsync(AriaRole.Button);
        await Expect(handle).ToHaveAttributeAsync("tabindex", "0");
        await Expect(handle).ToHaveAccessibleNameAsync(new Regex("\\S"));
        await Expect(handle).ToHaveAccessibleDescriptionAsync(Instructions);
    }

    [TestMethod]
    public async Task The_live_region_is_an_assertive_status_that_the_moves_are_written_to()
    {
        var page = await OpenAsync("/accessibility/keyboard");
        var live = page.Locator("#bit-bdnd-live-region");
        await Card(page, "Write the docs").FocusAsync();

        await page.Keyboard.PressAsync("Space");

        await Expect(live).ToHaveRoleAsync(AriaRole.Status);
        await Expect(live).ToHaveAttributeAsync("aria-live", "assertive");
        await Expect(live).ToHaveAttributeAsync("aria-atomic", "true");
        await Expect(live).ToHaveTextAsync(new Regex("^Picked up Write the docs\\."));
        await page.Keyboard.PressAsync("Escape");
        await Expect(live).ToContainTextAsync("cancelled");
    }

    [TestMethod]
    public async Task The_copy_that_follows_the_pointer_is_hidden_from_screen_readers()
    {
        var page = await OpenAsync("/customize/pointer");
        var glass = page.Locator(".pointer-box:not([data-bit-bdnd-preview])", new() { HasText = "Glass" });
        var from = (await BoxAsync(glass)).Centre;
        var before = Regex.Count(await page.Locator("body").AriaSnapshotAsync(), "Glass");

        await page.Mouse.MoveAsync((float)from.X, (float)from.Y);
        await page.Mouse.DownAsync();
        await MoveAsync(page, from.Offset(40, 40));
        var copy = page.Locator("[data-bit-bdnd-preview]");
        await Expect(copy).ToHaveCountAsync(1);

        await Expect(copy).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(copy).ToHaveAttributeAsync("inert", "");
        // nothing in the accessibility tree comes from it: the tree reads Glass as often as before the drag
        Assert.AreEqual(before, Regex.Count(await page.Locator("body").AriaSnapshotAsync(), "Glass"), "the copy adds to the accessibility tree");
        await page.Keyboard.PressAsync("Escape");
        await page.Mouse.UpAsync();
    }

    [TestMethod]
    public async Task Chromiums_accessibility_tree_gives_the_source_its_description_and_the_region_its_liveness()
    {
        RequireChromium("the browser's accessibility tree is read through the DevTools protocol");
        var page = await OpenAsync("/accessibility/keyboard");
        var cdp = await page.Context.NewCDPSessionAsync(page);

        var card = await AccessibilityNodeAsync(cdp, "article.kanban-card");
        StringAssert.Matches(Value(card, "description"), Instructions, "the description the browser hands a screen reader");
        Assert.AreEqual("article", Value(card, "role"));
        Assert.IsTrue(Property(card, "focusable") is "true", "the source is focusable in the accessibility tree");

        await Card(page, "Write the docs").FocusAsync();
        await page.Keyboard.PressAsync("Space");
        await Expect(page.Locator("#bit-bdnd-live-region")).ToContainTextAsync("Picked up");
        var region = await AccessibilityNodeAsync(cdp, "#bit-bdnd-live-region");
        Assert.AreEqual("status", Value(region, "role"));
        Assert.AreEqual("assertive", Property(region, "live"));
        Assert.AreEqual("true", Property(region, "atomic"));
        await page.Keyboard.PressAsync("Escape");
    }

    // The accessibility node Chromium builds for the first element matching the selector.
    private static async Task<JsonElement> AccessibilityNodeAsync(ICDPSession cdp, string selector)
    {
        var document = await cdp.SendAsync("DOM.getDocument", new Dictionary<string, object> { ["depth"] = 0 });
        var root = document!.Value.GetProperty("root").GetProperty("nodeId").GetInt32();
        var found = await cdp.SendAsync("DOM.querySelector", new Dictionary<string, object> { ["nodeId"] = root, ["selector"] = selector });
        var nodeId = found!.Value.GetProperty("nodeId").GetInt32();
        var tree = await cdp.SendAsync("Accessibility.getPartialAXTree", new Dictionary<string, object> { ["nodeId"] = nodeId, ["fetchRelatives"] = false });
        return tree!.Value.GetProperty("nodes")[0];
    }

    private static string? Value(JsonElement node, string name) =>
        node.TryGetProperty(name, out var value) && value.TryGetProperty("value", out var inner) ? inner.ToString() : null;

    private static string? Property(JsonElement node, string name)
    {
        if (node.TryGetProperty("properties", out var properties) is false) return null;
        foreach (var property in properties.EnumerateArray())
        {
            if (property.GetProperty("name").GetString() == name) return property.GetProperty("value").GetProperty("value").ToString().ToLowerInvariant();
        }
        return null;
    }
}
