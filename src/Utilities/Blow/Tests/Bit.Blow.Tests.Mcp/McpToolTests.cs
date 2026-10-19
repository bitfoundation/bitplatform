using Bit.Blow.Demo.Server.Services;

namespace Bit.Blow.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBlowGuideSection");

        foreach (var heading in new[] { "Getting started", "Nodes", "Handles and connections", "Edges", "Interaction", "The viewport", "Layout and alignment", "Add-ons", "Saving, undo and clipboard", "Theming", "Hosting models", "Coming from React Flow" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBlowGuideSection", new() { ["heading"] = "saving UNDO clipboard" });

        Assert.StartsWith("## Saving, undo and clipboard", section);
        Assert.Contains("RestoreAsync", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBlowGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### Install", section);
        Assert.Contains("### A first flow", section);
        Assert.DoesNotContain("## Nodes", section);
    }

    [TestMethod]
    public async Task A_sub_section_is_reached_on_its_own()
    {
        var section = await McpCall.TextAsync("GetBlowGuideSection", new() { ["heading"] = "Sub-flows and grouping" });

        Assert.StartsWith("### Sub-flows and grouping", section);
        Assert.Contains("ParentId", section);
        Assert.DoesNotContain("### Constraining where a node can go", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_reached_through_its_key()
    {
        // The README may use no heading twice; every one that it does use twice has to answer through its key.
        foreach (var section in BlowSourceCatalog.GuideSections.Where(section => section.Key != section.Heading))
        {
            var text = await McpCall.TextAsync("GetBlowGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBlowGuideSection", new() { ["heading"] = "layout" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Auto-layout", answer);
        Assert.Contains("Layout and alignment", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBlowApi");

        foreach (var type in new[] { "BlowCanvas", "BlowNode", "BlowEdge", "BlowHandle", "BlowMiniMap", "BlowLayout", "BlowHistory", "BlowClipboard", "BlowGraphUtils", "BlowSnapshot", "IBlowContext" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("BlowInterop", index);
        Assert.DoesNotContain("BlowInputPolicy", index);
        Assert.DoesNotContain("BlowEdgeGeometryKey", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "BlowCanvas" });

        Assert.StartsWith("# BlowCanvas (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**MinZoom** : `double` = `0.2`", reference);
        Assert.Contains("**AriaLabel** : `string` = `\"Flow diagram\"`", reference);
        Assert.Contains("**HistoryEnabled** : `bool` = `false`", reference);
        Assert.Contains("**Nodes** : `List<BlowNode>` = `[]` **(required)**", reference);
        Assert.DoesNotContain("[truncated", reference, "The canvas's reference is the longest answer, and has to arrive whole.");
    }

    [TestMethod]
    public async Task A_collection_default_is_its_contents_and_an_unbound_event_has_none()
    {
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "BlowCanvas" });

        Assert.Contains("**DeleteKeys** : `IReadOnlyList<string>` = `[\"Delete\", \"Backspace\"]`", reference);
        Assert.Contains("**PanButtons** : `IReadOnlyList<int>` = `[0]`", reference);
        Assert.Contains("**NodesChanged** : `EventCallback<List<BlowNode>>` - ", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance_including_the_inherited_ones()
    {
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "BlowLayeredLayoutOptions" });

        Assert.StartsWith("# BlowLayeredLayoutOptions (Class)", reference);
        Assert.Contains("inherits `BlowLayoutOptions`", reference);
        Assert.Contains("**LayerGap** : `double` = `80`", reference);
        Assert.Contains("**Direction** : `BlowLayoutDirection` = `BlowLayoutDirection.TopToBottom`", reference);
        Assert.Contains("**Origin** : `BlowXYPosition` = `BlowXYPosition.Zero`", reference);
    }

    [TestMethod]
    public async Task A_required_member_that_starts_out_unset_is_not_given_a_default()
    {
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "BlowNode" });

        var idLine = reference.Split('\n').Single(line => line.StartsWith("- **Id**", StringComparison.Ordinal));

        Assert.DoesNotContain(" = `", idLine);
        Assert.Contains("**Type** : `string` = `\"default\"`", reference);
        Assert.Contains("**DefaultWidth** : `double` = `150`", reference);
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        var reference = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "BlowSnapshot" });

        Assert.Contains("- **Detach**", reference);
        Assert.DoesNotContain("- **Equals**", reference);
        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("- **Deconstruct**", reference);
        Assert.DoesNotContain("<Clone>", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "Canvas" });

        Assert.Contains("BlowCanvas", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBlowApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Blow'", answer);
    }

    // ---- Theme -------------------------------------------------------------

    [TestMethod]
    public async Task The_theme_table_gives_every_property_its_light_and_dark_value()
    {
        var theme = await McpCall.TextAsync("GetBlowTheme");

        Assert.Contains("| `--bit-blow-node-selected` | `#2563eb` | `#60a5fa` via `--bit-blow-dark-accent` |", theme);
        Assert.Contains("| `--bit-blow-handle-valid` | `#22c55e` | same |", theme);
        Assert.Contains("## Dark palette", theme);
        Assert.Contains("| `--bit-blow-dark-accent` | `#60a5fa` | `--bit-blow-node-selected`, `--bit-blow-edge-selected`, `--bit-blow-connection-stroke` |", theme);

        foreach (var property in BlowThemeCatalog.Properties) Assert.Contains($"| `{property.Name}` |", theme);
    }

    [TestMethod]
    public async Task One_property_comes_with_what_paints_with_it_and_how_to_override_it()
    {
        var selected = await McpCall.TextAsync("GetBlowTheme", new() { ["property"] = "node-selected" });

        Assert.StartsWith("# `--bit-blow-node-selected`", selected);
        Assert.Contains(".bit-blow-node.selected { border-color: var(--bit-blow-node-selected) }", selected);
        Assert.Contains("`BlowNodeResizer.Color`", selected);
        Assert.Contains(".bit-blow { --bit-blow-dark-accent: <dark value>; }", selected);

        // Read by its component's markup, which no stylesheet rule and no parameter default shows.
        var pattern = await McpCall.TextAsync("GetBlowTheme", new() { ["property"] = "--bit-blow-background-pattern" });

        Assert.Contains("`BlowBackground`", pattern);
    }

    [TestMethod]
    public async Task A_dark_palette_property_names_what_it_is_copied_over()
    {
        var accent = await McpCall.TextAsync("GetBlowTheme", new() { ["property"] = "--bit-blow-dark-accent" });

        Assert.Contains("dark-palette property", accent);
        Assert.Contains("`--bit-blow-edge-selected`", accent);
        Assert.Contains("## Painted by, in dark mode", accent);
    }

    [TestMethod]
    public async Task An_unknown_property_suggests_the_close_ones()
    {
        var close = await McpCall.TextAsync("GetBlowTheme", new() { ["property"] = "edge" });
        var none = await McpCall.TextAsync("GetBlowTheme", new() { ["property"] = "--zebra-stripes" });

        Assert.Contains("Did you mean", close);
        Assert.Contains("--bit-blow-edge-stroke", close);
        Assert.Contains("declares no custom property called", none);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_checklist_the_stylesheet_and_the_guide_on_its_hosting_model()
    {
        foreach (var mode in BlowSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBlowSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.Blow", guide, mode);
            Assert.Contains("@using Bit.Blow", guide, mode);
            Assert.Contains("NO services to register", guide, mode);
            Assert.Contains("_content/Bit.Blow/bit-blow.css", guide, mode);
            Assert.Contains("height", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
            Assert.Contains("### Install", guide, mode);
            Assert.DoesNotContain("AddBitBlow", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_server_mode_explains_the_round_trip()
    {
        var guide = await McpCall.TextAsync("GetBlowSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("SignalR", guide);
        Assert.Contains("### Blazor Server", guide, "The guide's own section on Blazor Server should be quoted.");
        Assert.Contains("@rendermode InteractiveServer", guide);
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBlowSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BlowSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("_content/Bit.Blow/bit-blow.css", BlowSourceCatalog.GetSourceFile("Demo/Server/Components/App.razor")!);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBlowSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BlowSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_pages_examples_and_stylesheet_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBlowSourceFile");

        Assert.Contains("Demo/Client/Pages/CustomNodes.razor", index);
        Assert.Contains("Demo/Client/Pages/Examples/SaveRestoreExample.razor", index);
        Assert.Contains("Demo/Client/Examples/ExampleCatalog.cs", index);
        Assert.Contains(BlowSourceCatalog.StylesheetPath, index);
        Assert.Contains("`/examples/save-restore`", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBlowSourceFile", new() { ["path"] = "Demo/Client/Pages/CustomNodes.razor" });

        Assert.Contains("<BlowCanvas", file);
        Assert.Contains("@page \"/custom-nodes\"", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBlowSourceFile", new() { ["path"] = "SaveRestore" });

        Assert.Contains("Demo/Client/Pages/Examples/SaveRestoreExample.razor", answer);
    }
}
