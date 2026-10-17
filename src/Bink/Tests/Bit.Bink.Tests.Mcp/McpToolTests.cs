using Bit.Bink.Demo.Server.Services;

namespace Bit.Bink.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBinkGuideSection");

        foreach (var heading in new[] { "Getting started", "Drawing, brushes and pressure", "Erasing", "Serialization", "SVG and PNG export", "Hosting models", "Collaboration", "Extending Bink" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBinkGuideSection", new() { ["heading"] = "svg png EXPORT" });

        Assert.StartsWith("## SVG and PNG export", section);
        Assert.Contains("BinkSvgExporter", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBinkGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### A signature field", section);
        Assert.DoesNotContain("## Architecture", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_reached_through_its_key()
    {
        var duplicated = BinkSourceCatalog.GuideSections.Where(section => section.Key != section.Heading).ToArray();

        Assert.IsNotEmpty(duplicated, "The README uses no heading twice any more; this test has nothing left to guard.");

        foreach (var section in duplicated)
        {
            var text = await McpCall.TextAsync("GetBinkGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }

        // The second "Highlights" is the annotation element, not the feature list at the top of the README.
        var highlights = await McpCall.TextAsync("GetBinkGuideSection", new() { ["heading"] = "Annotations, text and images > Highlights" });

        Assert.Contains("BinkHighlightElement", highlights);
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBinkGuideSection", new() { ["heading"] = "export" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("SVG and PNG export", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBinkApi");

        foreach (var type in new[] { "BinkCanvas", "BinkDocument", "BinkStroke", "BinkPenBrush", "BinkSerializer", "BinkSvgExporter", "IBinkTool", "BinkCollaborationSession" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("BinkInterop", index);
        Assert.DoesNotContain("BinkWasmRenderBridge", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "BinkCanvas" });

        Assert.StartsWith("# BinkCanvas (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Height** : `string` = `\"300px\"`", reference);
        Assert.Contains("**EnableTouchGestures** : `bool` = `false`", reference);
        Assert.Contains("**DocumentChanged** : `EventCallback<BinkDocumentChangedEventArgs>`", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var reference = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "BinkSerializerOptions" });

        Assert.Contains("**Decimals** : `int` = `3`", reference);
        Assert.Contains("**Indented** : `bool` = `false`", reference);
        Assert.Contains("**PointEncoding** : `BinkPointEncoding` = `BinkPointEncoding.Binary`", reference);
    }

    [TestMethod]
    public async Task A_value_made_up_per_instance_is_not_reported_as_a_default()
    {
        // Every BinkDocument gets a fresh Guid: none of them is "the default".
        var reference = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "BinkDocument" });

        var idLine = reference.Split('\n').Single(line => line.StartsWith("- **Id**", StringComparison.Ordinal));

        Assert.DoesNotContain(" = `", idLine);
    }

    [TestMethod]
    public async Task An_inheritdoc_member_carries_the_documentation_it_inherits()
    {
        var reference = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "BinkPenBrush" });

        var kindLine = reference.Split('\n').Single(line => line.StartsWith("- **Kind**", StringComparison.Ordinal));

        Assert.Contains("discriminator", kindLine, "BinkPenBrush.Kind says <inheritdoc />, so its summary is BinkBrush.Kind's.");
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        var reference = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "BinkPenBrush" });

        Assert.DoesNotContain("- **Equals**", reference);
        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("<Clone>", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "PenBrush" });

        Assert.Contains("BinkPenBrush", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBinkApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Bink'", answer);
    }

    // ---- Brushes -----------------------------------------------------------

    [TestMethod]
    public async Task The_brush_table_lists_every_registered_kind()
    {
        var table = await McpCall.TextAsync("GetBinkBrushes");

        foreach (var kind in BinkBrushRegistry.Default.Kinds) Assert.Contains($"| `{kind}` |", table);
    }

    [TestMethod]
    public async Task A_brush_comes_with_its_defaults_geometry_and_json()
    {
        var pen = await McpCall.TextAsync("GetBinkBrushes", new() { ["kind"] = "pen" });

        Assert.StartsWith("# BinkPenBrush (`pen`)", pen);
        Assert.Contains("**Width** = `4`", pen);
        Assert.Contains("EaseInOut 0.2..1", pen);
        Assert.Contains("variable-width outline", pen);
        Assert.Contains("\"kind\": \"pen\"", pen);

        var marker = await McpCall.TextAsync("GetBinkBrushes", new() { ["kind"] = "marker" });

        Assert.Contains("constant-width centerline", marker);
    }

    [TestMethod]
    public async Task A_brush_is_found_by_its_type_name_too()
    {
        var byType = await McpCall.TextAsync("GetBinkBrushes", new() { ["kind"] = "BinkHighlighterBrush" });
        var byShortName = await McpCall.TextAsync("GetBinkBrushes", new() { ["kind"] = "Highlighter" });

        Assert.StartsWith("# BinkHighlighterBrush (`highlighter`)", byType);
        Assert.AreEqual(byType, byShortName);
    }

    [TestMethod]
    public async Task An_unknown_brush_lists_the_real_kinds()
    {
        var answer = await McpCall.TextAsync("GetBinkBrushes", new() { ["kind"] = "crayon" });

        Assert.Contains("no brush called 'crayon'", answer);
        foreach (var kind in BinkBrushRegistry.Default.Kinds) Assert.Contains(kind, answer);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_checklist_and_the_guide_on_its_hosting_model()
    {
        foreach (var mode in BinkSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBinkSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.Bink", guide, mode);
            Assert.Contains("@using Bit.Bink", guide, mode);
            Assert.Contains("NO services to register", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
            Assert.DoesNotContain("AddBitBink", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_server_mode_explains_wet_ink()
    {
        var guide = await McpCall.TextAsync("GetBinkSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("WetInk", guide);
        Assert.Contains("#### Blazor Server", guide, "The quoted guide section should be demoted under 'From the guide'.");
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBinkSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BinkSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBinkSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BinkSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_and_the_stylesheet_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBinkSourceFile");

        Assert.Contains("Demo/Client/Pages/Signature.razor", index);
        Assert.Contains("Demo/Client/Pages/Whiteboard.razor", index);
        Assert.Contains(BinkSourceCatalog.StylesheetPath, index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBinkSourceFile", new() { ["path"] = "Demo/Client/Pages/Signature.razor" });

        Assert.Contains("<BinkCanvas", file);
        Assert.Contains("@page \"/signature\"", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBinkSourceFile", new() { ["path"] = "Whiteboard" });

        Assert.Contains("Demo/Client/Pages/Whiteboard.razor", answer);
    }
}
