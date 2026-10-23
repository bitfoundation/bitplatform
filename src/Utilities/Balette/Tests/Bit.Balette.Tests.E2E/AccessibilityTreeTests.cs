using System.Text.Json;
using Bit.Balette.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Balette.Tests.E2E;

/// <summary>
/// What a screen reader is told, read from Chromium's own accessibility tree through the DevTools protocol - the roles,
/// computed names and states Blink hands to the platform's accessibility API, after every ARIA attribute and every
/// visually hidden text has been resolved. axe checks rules; this checks what comes out.
/// </summary>
/// <remarks>
/// Closer to a screen reader than any rule engine, and still not one: how NVDA, JAWS, VoiceOver or TalkBack phrase and
/// time what they read is theirs, and only testing with them settles it.
/// </remarks>
[TestClass]
public sealed class AccessibilityTreeTests : BaletteTest
{
    private sealed record AxNode(string Id, string? ParentId, string Role, string Name, IReadOnlyDictionary<string, JsonElement> Properties)
    {
        /// <summary>A boolean state that is on, or a tri-state one ("true", "mixed") reported as a token.</summary>
        public bool Is(string property) => Properties.TryGetValue(property, out var value)
            && (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && value.GetString() is "true" or "mixed"));

        public string? Token(string property) => Properties.TryGetValue(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

        public bool Has(string property) => Properties.ContainsKey(property);
    }

    /// <summary>The page's full accessibility tree, by node id.</summary>
    private static async Task<Dictionary<string, AxNode>> TreeAsync(IPage page)
    {
        var cdp = await page.Context.NewCDPSessionAsync(page);
        var result = await cdp.SendAsync("Accessibility.getFullAXTree");
        var nodes = new Dictionary<string, AxNode>(StringComparer.Ordinal);
        foreach (var node in result!.Value.GetProperty("nodes").EnumerateArray())
        {
            if (node.TryGetProperty("ignored", out var ignored) && ignored.GetBoolean())
            {
                continue;
            }

            var properties = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
            if (node.TryGetProperty("properties", out var list))
            {
                foreach (var property in list.EnumerateArray())
                {
                    var value = property.GetProperty("value");
                    properties[property.GetProperty("name").GetString()!] = value.TryGetProperty("value", out var v) ? v.Clone() : value.Clone();
                }
            }

            var id = node.GetProperty("nodeId").GetString()!;
            nodes[id] = new AxNode(
                id,
                node.TryGetProperty("parentId", out var parent) ? parent.GetString() : null,
                node.TryGetProperty("role", out var role) ? role.GetProperty("value").GetString() ?? "" : "",
                node.TryGetProperty("name", out var name) && name.TryGetProperty("value", out var nameValue) ? nameValue.GetString() ?? "" : "",
                properties);
        }

        return nodes;
    }

    /// <summary>The nearest ancestor with one of <paramref name="roles"/>, skipping the generic containers in between.</summary>
    private static AxNode? Ancestor(Dictionary<string, AxNode> tree, AxNode node, params string[] roles)
    {
        for (var id = node.ParentId; id is not null && tree.TryGetValue(id, out var parent); id = parent.ParentId)
        {
            if (roles.Contains(parent.Role))
            {
                return parent;
            }
        }

        return null;
    }

    private static string Describe(IEnumerable<AxNode> nodes) => string.Join(" | ", nodes.Select(n => $"{n.Role} '{n.Name}'"));

    [TestMethod]
    public async Task The_search_box_is_a_named_expanded_combobox_whose_active_option_is_named_by_its_row()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "settings");
        await ExpectFirstItemAsync(page, "navigation.settings");

        var tree = await TreeAsync(page);
        var combobox = tree.Values.Single(n => n.Role == "combobox" && n.Is("focused"));
        Assert.AreEqual("Search commands", combobox.Name);
        Assert.IsTrue(combobox.Is("expanded"), "the combobox does not say its list is shown");

        // The option it points at: named by its title, its subtitle or keyword, and its shortcut in words.
        var options = tree.Values.Where(n => n.Role == "option").ToList();
        var active = options.Single(n => n.Is("selected"));
        Assert.IsTrue(active.Name.StartsWith("Settings", StringComparison.Ordinal), active.Name);
        Assert.Contains("G then S", active.Name);

        // In its group, which the reader hears by name.
        // Compared ignoring case: the heading is upper-cased by the stylesheet, which a browser may carry into the name.
        Assert.AreEqual("Navigation", Ancestor(tree, active, "group", "listbox")?.Name, StringComparer.OrdinalIgnoreCase, Describe(options));
        Assert.AreEqual("listbox", Ancestor(tree, active, "listbox")?.Role);
    }

    [TestMethod]
    public async Task A_toggles_state_is_part_of_its_name()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "word wrap");
        await ExpectFirstItemAsync(page, "toggle:wordwrap");

        var tree = await TreeAsync(page);
        var option = tree.Values.First(n => n.Role == "option" && n.Name.StartsWith("Word wrap", StringComparison.Ordinal));

        // The demo starts with word wrap on, and its shortcut is Alt+Z.
        Assert.Contains("On", option.Name);
        Assert.Contains("Alt+Z", option.Name);
    }

    [TestMethod]
    public async Task A_multi_select_page_says_checked_and_not_selected()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);
        await TypeAsync(page, "set labels");
        await ExpectFirstItemAsync(page, "actions.set-labels");
        await page.Keyboard.PressAsync("Enter");
        await page.Keyboard.PressAsync("Enter");
        await Assertions.Expect(Item(page, "label:bug")).ToHaveAttributeAsync("aria-checked", "true");

        var tree = await TreeAsync(page);
        var listbox = tree.Values.Single(n => n.Role == "listbox");
        Assert.IsTrue(listbox.Is("multiselectable"));
        var options = tree.Values.Where(n => n.Role == "option").ToList();
        var bug = options.Single(n => n.Name.StartsWith("bug", StringComparison.Ordinal));
        Assert.IsTrue(bug.Is("checked"), Describe(options));
        Assert.IsFalse(options.Any(n => n.Is("selected")), "an option of a multi-select page is reported selected");
    }

    [TestMethod]
    public async Task A_prompt_is_a_text_field_named_by_its_question_with_no_list_behind_it()
    {
        var page = await OpenAsync("/");
        await page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync(page);

        var tree = await TreeAsync(page);
        // The page's root reports the focus too; the element holding it is the one below it.
        var focused = tree.Values.Single(n => n.Is("focused") && n.Role is not ("RootWebArea" or "WebArea"));
        Assert.AreEqual("textbox", focused.Role);
        Assert.AreEqual("New project name", focused.Name);
        Assert.IsFalse(tree.Values.Any(n => n.Role is "combobox" or "listbox"), "a prompt still exposes a combobox or a listbox");
    }

    [TestMethod]
    public async Task The_dialog_is_a_named_modal_and_its_announcements_are_live_regions()
    {
        var page = await OpenAsync("/");
        await OpenPaletteAsync(page);

        var tree = await TreeAsync(page);
        var dialog = tree.Values.Single(n => n.Role == "dialog");
        Assert.AreEqual("Command palette", dialog.Name);
        Assert.IsTrue(dialog.Is("modal"), "the dialog is not reported modal");

        var live = tree.Values.Select(n => n.Token("live")).Where(t => t is not null).ToList();
        Assert.Contains("polite", live);
        Assert.Contains("assertive", live);
    }
}
