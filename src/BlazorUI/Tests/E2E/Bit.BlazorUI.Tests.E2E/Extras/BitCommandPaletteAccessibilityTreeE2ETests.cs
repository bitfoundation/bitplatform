using System.Text.Json;

namespace Bit.BlazorUI.Tests.E2E.Extras;

/// <summary>
/// What a screen reader is told about BitCommandPalette, read from Chromium's own accessibility tree through the
/// DevTools protocol - the roles, computed names and states Blink hands to the platform's accessibility API, after every
/// ARIA attribute and every visually hidden text has been resolved. axe checks rules; this checks what comes out.
/// </summary>
/// <remarks>
/// Closer to a screen reader than any rule engine, and still not one: how NVDA, JAWS, VoiceOver or TalkBack phrase and
/// time what they read is theirs, and only testing with them settles it.
/// </remarks>
[TestClass]
[E2ERetry]
public class BitCommandPaletteAccessibilityTreeE2ETests : BitCommandPaletteE2ETestBase
{
    private sealed record AxNode(string Id, string? ParentId, string Role, string Name, IReadOnlyDictionary<string, JsonElement> Properties)
    {
        /// <summary>A boolean state that is on, or a tri-state one ("true", "mixed") reported as a token.</summary>
        public bool Is(string property) => Properties.TryGetValue(property, out var value)
            && (value.ValueKind == JsonValueKind.True || (value.ValueKind == JsonValueKind.String && value.GetString() is "true" or "mixed"));

        public string? Token(string property) => Properties.TryGetValue(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }

    /// <summary>The page's full accessibility tree, by node id.</summary>
    private async Task<Dictionary<string, AxNode>> TreeAsync()
    {
        var cdp = await Page.Context.NewCDPSessionAsync(Page);
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

    private static void RequireChromium()
    {
        // Firefox and WebKit hand their accessibility trees to no protocol Playwright exposes.
        if (IsChromium is false)
        {
            Assert.Inconclusive($"The accessibility tree is read through the DevTools protocol, which {BrowserName} does not have.");
        }
    }

    [TestMethod]
    public async Task TheSearchBox_IsANamedExpandedCombobox_WhoseActiveOptionIsNamedByItsRow()
    {
        RequireChromium();
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("settings");
        await ExpectFirstItemAsync("navigation.settings");
        await ExpectActiveAsync("navigation.settings");

        var tree = await TreeAsync();
        var combobox = tree.Values.Single(n => n.Role == "combobox" && n.Is("focused"));
        Assert.AreEqual("Search commands", combobox.Name);
        Assert.IsTrue(combobox.Is("expanded"), "the combobox does not say its list is shown");

        // The option it points at: named by its title and its shortcut in words.
        var options = tree.Values.Where(n => n.Role == "option").ToList();
        var active = options.Single(n => n.Is("selected"));
        Assert.IsTrue(active.Name.StartsWith("Settings", StringComparison.Ordinal), active.Name);
        StringAssert.Contains(active.Name, "G then S");

        // In its group, which the reader hears by name (compared ignoring case: a stylesheet may upper-case the heading).
        Assert.AreEqual("Navigation", Ancestor(tree, active, "group", "listbox")?.Name, StringComparer.OrdinalIgnoreCase, Describe(options));
        Assert.AreEqual("listbox", Ancestor(tree, active, "listbox")?.Role);
    }

    [TestMethod]
    public async Task AToggle_SaysItsStateInItsName()
    {
        RequireChromium();
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("word wrap");
        await ExpectFirstItemAsync("toggle:wordwrap");

        var tree = await TreeAsync();
        var option = tree.Values.First(n => n.Role == "option" && n.Name.StartsWith("Word wrap", StringComparison.Ordinal));

        // The page starts with word wrap on, and its shortcut is Alt+Z.
        StringAssert.Contains(option.Name, "On");
        StringAssert.Contains(option.Name, "Alt+Z");
    }

    [TestMethod]
    public async Task AMultiSelectPage_SaysCheckedAndNotSelected()
    {
        RequireChromium();
        await OpenPageAsync();
        await OpenPaletteAsync();
        await TypeAsync("set labels");
        await ExpectFirstItemAsync("actions.set-labels");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Dialog).ToHaveAttributeAsync("data-multiselect", "true");
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Item("label:bug")).ToHaveAttributeAsync("aria-checked", "true");

        var tree = await TreeAsync();
        var listbox = tree.Values.Single(n => n.Role == "listbox");
        Assert.IsTrue(listbox.Is("multiselectable"));
        var options = tree.Values.Where(n => n.Role == "option").ToList();
        var bug = options.Single(n => n.Name.StartsWith("bug", StringComparison.Ordinal));
        Assert.IsTrue(bug.Is("checked"), Describe(options));
        Assert.IsFalse(options.Any(n => n.Is("selected")), "an option of a multi-select page is reported selected");
    }

    [TestMethod]
    public async Task APrompt_IsATextFieldNamedByItsQuestion_WithNoListBehindIt()
    {
        RequireChromium();
        await OpenPageAsync();
        await Page.Keyboard.PressAsync("F2");
        await ExpectOpenAsync();
        await Expect(Prompt).ToBeVisibleAsync();

        var tree = await TreeAsync();
        // The page's root reports the focus too; the element holding it is the one below it.
        var focused = tree.Values.Single(n => n.Is("focused") && n.Role is not ("RootWebArea" or "WebArea"));
        Assert.AreEqual("textbox", focused.Role);
        Assert.AreEqual("New project name", focused.Name);
        Assert.IsFalse(tree.Values.Any(n => n.Role is "combobox" or "listbox"), "a prompt still exposes a combobox or a listbox");
    }

    [TestMethod]
    public async Task TheDialog_IsANamedModal_AndItsAnnouncementsAreLiveRegions()
    {
        RequireChromium();
        await OpenPageAsync();
        await OpenPaletteAsync();

        var tree = await TreeAsync();
        var dialog = tree.Values.Single(n => n.Role == "dialog");
        Assert.AreEqual("Command palette", dialog.Name);
        Assert.IsTrue(dialog.Is("modal"), "the dialog is not reported modal");

        var live = tree.Values.Select(n => n.Token("live")).Where(t => t is not null).ToList();
        CollectionAssert.Contains(live, "polite");
        CollectionAssert.Contains(live, "assertive");
    }
}
