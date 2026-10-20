using Bit.Balette.Demo.Server.Services;

namespace Bit.Balette.Tests.Mcp;

/// <summary>
/// Every reference tool's answers over the protocol: the thing a key names, the index a missing key
/// answers with, and the guidance a wrong key gets instead of an error.
/// </summary>
[TestClass]
public class McpToolTests
{
    // ---- Guide -------------------------------------------------------------

    [TestMethod]
    public async Task The_guide_index_lists_every_top_level_section()
    {
        var index = await McpCall.TextAsync("GetBaletteGuideSection");

        foreach (var heading in new[] { "Getting started", "Commands", "Declarative markup", "Handlers and results", "Nested pages", "Search and ranking", "Modes", "Async sources", "Keyboard", "Theming", "Hosting models", "Options", "Troubleshooting" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBaletteGuideSection", new() { ["heading"] = "recents PINS action panel" });

        Assert.StartsWith("## Recents, pins and the action panel", section);
        Assert.Contains("MaxRecents", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBaletteGuideSection", new() { ["heading"] = "Search and ranking" });

        Assert.Contains("### The fuzzy matcher", section);
        Assert.Contains("### Query operators", section);
        Assert.DoesNotContain("## Modes", section);
    }

    [TestMethod]
    public async Task Every_section_in_the_index_is_retrievable_by_its_key()
    {
        foreach (var section in BaletteSourceCatalog.GuideSections)
        {
            var text = await McpCall.TextAsync("GetBaletteGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBaletteGuideSection", new() { ["heading"] = "pages" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Nested pages", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBaletteApi");

        foreach (var type in new[] { "Balette", "BaletteTrigger", "BaletteGroup", "BaletteItem", "BaletteCommands", "BaletteCommand", "BaletteCmd", "BaletteCommandResult", "BaletteCommandMode", "IBaletteService", "BaletteOptions", "IBaletteSource", "BaletteFuzzyMatcher", "BaletteSubstringMatcher", "BaletteMatcherExtensions", "BaletteRanker", "BaletteKeyboardShortcut" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference, and so does the class Razor makes of _Imports.razor.
        Assert.DoesNotContain("BaletteQuery", index);
        Assert.DoesNotContain("BaletteCommandIdGenerator", index);
        Assert.DoesNotContain("_Imports", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "Balette" });

        Assert.StartsWith("# Balette (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Unstyled** : `bool` = `false`", reference);
        Assert.Contains("**Theme** : `string`", reference);
        Assert.Contains("**OpenAsync**(string query = null, string pageCommandId = null) : `Task`", reference);
    }

    [TestMethod]
    public async Task The_browser_layers_callbacks_are_not_listed_as_api()
    {
        // Public only because JavaScript interop can call nothing else.
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "Balette" });

        Assert.DoesNotContain("**OnKeyDown**", reference);
        Assert.DoesNotContain("**OnGlobalShortcut**", reference);
        Assert.DoesNotContain("**OnToggleShortcut**", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "BaletteOptions" });

        Assert.Contains("**DebounceMs** : `int` = `150`", reference);
        Assert.Contains("**MaxResults** : `int` = `150`", reference);
        Assert.Contains("**PriorityWeight** : `double` = `0.25`", reference);
        Assert.Contains("**FrecencyWeight** : `double` = `6`", reference);
        Assert.Contains("**HistoryStorageKey** : `string` = `\"bit-balette.history\"`", reference);
        Assert.Contains("**Matcher** : `IBaletteMatcher` = `BaletteFuzzyMatcher.Default`", reference);
        Assert.Contains("**ToggleShortcut** : `BaletteKeyboardShortcut` = `mod+k`", reference);
    }

    [TestMethod]
    public async Task The_field_weights_are_the_real_ones()
    {
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "BaletteMatchWeights" });
        var weights = new BaletteMatchWeights();

        Assert.Contains(FormattableString.Invariant($"**Title** : `double` = `{weights.Title}`"), reference);
        Assert.Contains(FormattableString.Invariant($"**Keyword** : `double` = `{weights.Keyword}`"), reference);
        Assert.Contains(FormattableString.Invariant($"**Tag** : `double` = `{weights.Tag}`"), reference);
    }

    [TestMethod]
    public async Task A_member_without_documentation_of_its_own_carries_the_interfaces()
    {
        // BaletteService implements IBaletteService and documents none of it itself.
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "BaletteService" });

        var registerLine = reference.Split('\n').First(line => line.StartsWith("- **Register**", StringComparison.Ordinal));

        Assert.Contains("Dispose", registerLine, "BaletteService.Register says nothing itself, so its summary is IBaletteService.Register's.");
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        // BaletteKeyChord is a positional record that writes none of these itself.
        var reference = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "BaletteKeyChord" });

        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("<Clone>", reference);
        Assert.Contains("- **Parse**", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "CommandMode" });

        Assert.Contains("BaletteCommandMode", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBaletteApi", new() { ["typeName"] = "CommandPalette" });

        Assert.Contains("prefixed 'Balette'", answer);
        Assert.Contains("'Balette' rather than 'CommandPalette'", answer);
    }

    // ---- Theme -------------------------------------------------------------

    [TestMethod]
    public async Task The_theme_variables_come_with_their_light_and_dark_values()
    {
        var theme = await McpCall.TextAsync("GetBaletteThemeVariables");

        Assert.Contains("| `--bit-balette-accent` | `#4f46e5` | `#818cf8` |", theme);
        Assert.Contains("| `--bit-balette-width` | `640px` | same |", theme);
        Assert.Contains("## Layout", theme);
        Assert.Contains("## Light palette", theme);
    }

    [TestMethod]
    public async Task The_theme_says_where_an_override_reaches()
    {
        var theme = await McpCall.TextAsync("GetBaletteThemeVariables");

        Assert.Contains("`:root`", theme);
        Assert.Contains("dialog[data-balette=\"dialog\"]", theme);
        Assert.Contains("the `Style` parameter", theme);
        Assert.Contains("the `Theme` parameter", theme);
        Assert.Contains("Unstyled=\"true\"", theme);
        Assert.Contains("_content/Bit.Balette/bit-balette.css", theme);
    }

    [TestMethod]
    public async Task A_variable_the_component_writes_inline_names_its_option()
    {
        var theme = await McpCall.TextAsync("GetBaletteThemeVariables");

        var rowHeight = theme.Split('\n').Single(line => line.StartsWith("| `--bit-balette-row-height`", StringComparison.Ordinal));
        var width = theme.Split('\n').Single(line => line.StartsWith("| `--bit-balette-width`", StringComparison.Ordinal));

        Assert.Contains("BaletteOptions.RowHeight", rowHeight);
        // Only while virtualizing: otherwise the stylesheet token reaches the rows, and overriding it works.
        Assert.Contains("while `Virtualize` is on", rowHeight);
        Assert.Contains("data-has-detail", width);
    }

    [TestMethod]
    public async Task The_unstyled_hooks_are_the_ones_the_markup_renders()
    {
        var theme = await McpCall.TextAsync("GetBaletteThemeVariables");

        foreach (var hook in new[] { "dialog", "root", "input", "list", "group-heading", "item", "mark", "footer", "detail", "trigger", "shortcut" })
        {
            Assert.Contains($"| `{hook}` |", theme, hook);
        }

        var item = theme.Split('\n').Single(line => line.StartsWith("| `item` |", StringComparison.Ordinal));

        foreach (var state in new[] { "data-active", "data-checked", "data-disabled", "data-destructive", "data-confirming", "data-kind", "data-value" })
        {
            Assert.Contains($"`{state}`", item, state);
        }
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_four_pieces_of_wiring()
    {
        foreach (var mode in BaletteSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.Balette", guide, mode);
            Assert.Contains("AddBitBaletteServices", guide, mode);
            Assert.Contains("<link rel=\"stylesheet\" href=\"_content/Bit.Balette/bit-balette.css\" />", guide, mode);
            Assert.Contains("NOT injected", guide, mode);
            Assert.Contains("<Balette />", guide, mode);
            Assert.Contains("@using Bit.Balette", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_web_app_modes_register_the_services_in_both_projects()
    {
        foreach (var mode in new[] { "wasm", "auto" })
        {
            var guide = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("BOTH", guide, mode);
            Assert.Contains("AddDemoServices", guide, mode);
            Assert.Contains("#### Blazor Web App", guide, $"{mode}: the quoted guide section should be demoted under 'From the guide'.");
        }
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BaletteSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!.TrimEnd(), guide);
        Assert.Contains(BaletteSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("### `Demo/Client/Layout/MainLayout.razor`", guide);
    }

    [TestMethod]
    public async Task The_server_mode_explains_what_crosses_the_circuit()
    {
        var guide = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("circuit", guide);
        Assert.Contains("#### Blazor Server", guide);
        Assert.Contains("@rendermode=\"InteractiveServer\"", guide);
    }

    [TestMethod]
    public async Task The_namespace_gotcha_is_part_of_the_checklist()
    {
        var guide = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = "standalone-wasm" });

        Assert.Contains("Bit.Balette.Balette", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBaletteSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BaletteSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_and_the_library_files_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBaletteSourceFile");

        Assert.Contains("Demo/Client/Pages/Commands.razor", index);
        Assert.Contains("Demo/Client/Services/DemoCommands.cs", index);
        Assert.Contains(BaletteSourceCatalog.StylesheetPath, index);
        Assert.Contains("Library/Components/Balette.razor", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task Every_file_in_the_index_comes_back_verbatim()
    {
        foreach (var file in BaletteSourceCatalog.SourceFiles)
        {
            var content = await McpCall.TextAsync("GetBaletteSourceFile", new() { ["path"] = file.Path });

            Assert.DoesNotContain("No source file at", content, file.Path);
            Assert.AreEqual(BaletteSourceCatalog.GetSourceFile(file.Path), content, file.Path);
        }
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBaletteSourceFile", new() { ["path"] = "Demo/Client/Services/DemoCommands.cs" });

        Assert.Contains("public static IEnumerable<BaletteCommand> Global(DemoState state)", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBaletteSourceFile", new() { ["path"] = "SearchAndRanking" });

        Assert.Contains("Demo/Client/Pages/SearchAndRanking.razor", answer);
    }
}
