using Bit.Bonboarding.Demo.Server.Dtos;
using Bit.Bonboarding.Demo.Server.Services;

namespace Bit.Bonboarding.Tests.Mcp;

/// <summary>
/// Every tool's answers over the protocol: the thing a key names, the index a missing key answers
/// with, and the guidance a wrong key gets instead of an error.
/// </summary>
[TestClass]
public class McpToolTests
{
    // ---- Guide -------------------------------------------------------------

    [TestMethod]
    public async Task The_guide_index_lists_every_section()
    {
        var index = await McpCall.TextAsync("GetBonboardingGuideSection");

        foreach (var heading in new[] { "Getting started", "Installation", "Dynamic and async targets", "Conditional and branching flows", "Multi-page tours", "Persistence", "API reference" })
        {
            Assert.Contains(heading, index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_and_punctuation()
    {
        var section = await McpCall.TextAsync("GetBonboardingGuideSection", new() { ["heading"] = "multi page TOURS" });

        Assert.StartsWith("## Multi-page tours", section);
        Assert.Contains("Route", section);
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBonboardingGuideSection", new() { ["heading"] = "branching flow" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Conditional and branching flows", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_components_and_the_service()
    {
        var index = await McpCall.TextAsync("GetBonboardingApi");

        foreach (var type in new[] { "BonboardingRoot", "BonboardingTour", "BonboardingStep", "IBonboardingService", "BonboardingTourDefinition" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("**BonboardingTourSession**", index);
        Assert.DoesNotContain("BonboardingHostRegistry", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_documentation()
    {
        var reference = await McpCall.TextAsync("GetBonboardingApi", new() { ["typeName"] = "BonboardingStep" });

        Assert.StartsWith("# BonboardingStep (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Anchor** : `string`", reference);
        Assert.Contains("data-bonboarding-anchor", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var reference = await McpCall.TextAsync("GetBonboardingApi", new() { ["typeName"] = "BonboardingOptions" });

        Assert.Contains("**Persist** : `bool` = `true`", reference);
        Assert.Contains("**StorageKeyPrefix** : `string` = `\"bit-bonboarding:\"`", reference);
        Assert.Contains("**AutoNavigateToStepRoute** : `bool` = `true`", reference);
    }

    [TestMethod]
    public async Task A_value_made_up_per_instance_is_not_reported_as_a_default()
    {
        // BonboardingStepDefinition.Id generates a fresh id for every instance: none of them is "the default".
        var reference = await McpCall.TextAsync("GetBonboardingApi", new() { ["typeName"] = "BonboardingStepDefinition" });

        var idLine = reference.Split('\n').Single(line => line.StartsWith("- **Id**", StringComparison.Ordinal));

        Assert.DoesNotContain(" = `", idLine);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBonboardingApi", new() { ["typeName"] = "TourDefinition" });

        Assert.Contains("BonboardingTourDefinition", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBonboardingApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Bonboarding'", answer);
    }

    // ---- Theme -------------------------------------------------------------

    [TestMethod]
    public async Task The_theme_variables_come_with_their_light_and_dark_values()
    {
        var theme = await McpCall.TextAsync("GetBonboardingThemeVariables");

        Assert.Contains("| `--bit-bonboarding-accent` | `#2563eb` | `#6ea8fe` |", theme);
        Assert.Contains("## Typography", theme);
    }

    [TestMethod]
    public async Task The_variables_the_options_write_inline_say_so()
    {
        var theme = await McpCall.TextAsync("GetBonboardingThemeVariables");

        var line = theme.Split('\n').Single(line => line.StartsWith("| `--bit-bonboarding-overlay-opacity`", StringComparison.Ordinal));

        Assert.Contains("`OverlayOpacity`", line);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_four_pieces_of_wiring()
    {
        foreach (var mode in BonboardingSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBonboardingSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("AddBitBonboardingServices", guide, mode);
            Assert.Contains("bit-bonboarding.css", guide, mode);
            Assert.Contains("BonboardingRoot", guide, mode);
            Assert.Contains("@using Bit.Bonboarding", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_web_app_modes_quote_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBonboardingSetupGuide", new() { ["renderMode"] = "auto" });

        Assert.Contains(BonboardingSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Program.cs`", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the four values in the schema for a client to honour; a value that gets
        // past it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBonboardingSetupGuide", new() { ["renderMode"] = "hybrid" });

        Assert.Contains("'hybrid' is not a known render mode", answer);
        foreach (var mode in BonboardingSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBonboardingSourceFile");

        Assert.Contains("Demo/Client/Pages/Flows.razor", index);
        Assert.Contains("Demo/Client/Shared/WorkspaceTour.cs", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("ReconnectModal", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBonboardingSourceFile", new() { ["path"] = "Demo/Client/Shared/WorkspaceTour.cs" });

        Assert.Contains("public static BonboardingTourDefinition Create()", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBonboardingSourceFile", new() { ["path"] = "Flows" });

        Assert.Contains("Demo/Client/Pages/Flows.razor", answer);
    }

    // ---- Placement ---------------------------------------------------------

    [TestMethod]
    public async Task A_placement_that_does_not_fit_is_flipped_and_says_so()
    {
        var analysis = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
        {
            ["targetX"] = 1200, ["targetY"] = 20, ["targetWidth"] = 60, ["targetHeight"] = 30, ["placement"] = "Right"
        });

        Assert.AreEqual("Right", analysis.Requested);
        Assert.AreEqual("Left", analysis.Side);
        Assert.IsFalse(analysis.UsedPreferred);
        Assert.AreEqual(0, analysis.Overflow);
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("flipped", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_step_without_a_target_is_a_centred_card()
    {
        var analysis = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
        {
            ["targetX"] = 0, ["targetY"] = 0, ["targetWidth"] = 0, ["targetHeight"] = 0
        });

        Assert.AreEqual("Center", analysis.Side);
        Assert.IsFalse(analysis.ShowArrow);
        Assert.IsNull(analysis.Spotlight);
        Assert.AreEqual(1280 / 2d, analysis.Popover.X + (analysis.Popover.Width / 2), 0.01);
    }

    [TestMethod]
    public async Task A_popover_larger_than_the_viewport_is_reported_as_clipped()
    {
        var analysis = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
        {
            ["targetX"] = 100, ["targetY"] = 100, ["targetWidth"] = 50, ["targetHeight"] = 50,
            ["popoverWidth"] = 400, ["popoverHeight"] = 900, ["viewportWidth"] = 360, ["viewportHeight"] = 640
        });

        Assert.IsGreaterThan(0, analysis.Overflow);
        Assert.IsTrue(analysis.Notes.Any(note => note.Contains("larger than the area", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Placement_names_are_parsed_forgivingly_and_bad_ones_are_explained()
    {
        var kebab = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
        {
            ["targetX"] = 500, ["targetY"] = 300, ["targetWidth"] = 100, ["targetHeight"] = 40, ["placement"] = "bottom-start"
        });

        Assert.AreEqual("BottomStart", kebab.Requested);
        Assert.AreEqual("Start", kebab.Align);

        var bad = await McpCall.StructuredAsync<BonboardingPlacementAnalysisDto>("InspectBonboardingPlacement", new()
        {
            ["targetX"] = 500, ["targetY"] = 300, ["targetWidth"] = 100, ["targetHeight"] = 40, ["placement"] = "Sideways", ["fallbacks"] = "Top, Nowhere"
        });

        Assert.AreEqual("Auto", bad.Requested);
        Assert.IsTrue(bad.Notes.Any(note => note.Contains("'Sideways' is not a placement", StringComparison.Ordinal)));
        Assert.IsTrue(bad.Notes.Any(note => note.Contains("'Nowhere'", StringComparison.Ordinal)));
    }
}
