using Bit.Bortable.Demo.Server.Services;

namespace Bit.Bortable.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBortableGuideSection");

        foreach (var heading in new[] { "Getting started", "Sorting a list", "Shared lists", "Nested lists", "Drag handles", "Filtering items", "Swap mode", "Multi-drag", "Events", "Styling", "Hosting models", "How it works", "API reference", "Coming from SortableJS", "Limitations", "Troubleshooting" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBortableGuideSection", new() { ["heading"] = "read ONLY lists" });

        Assert.StartsWith("### Read-only lists", section);
        Assert.Contains("ImmutableList", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBortableGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### Install", section);
        Assert.Contains("### Register the services", section);
        Assert.Contains("### Two-way binding", section);
        Assert.DoesNotContain("## Sorting a list", section);
    }

    [TestMethod]
    public async Task A_sub_section_is_reached_on_its_own()
    {
        var section = await McpCall.TextAsync("GetBortableGuideSection", new() { ["heading"] = "Cloning" });

        Assert.StartsWith("### Cloning", section);
        Assert.Contains("BortablePull.Clone", section);
        Assert.DoesNotContain("### Read-only lists", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_reached_through_its_key()
    {
        // The README may use no heading twice; every one that it does use twice has to answer through its key.
        foreach (var section in BortableSourceCatalog.GuideSections.Where(section => section.Key != section.Heading))
        {
            var text = await McpCall.TextAsync("GetBortableGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBortableGuideSection", new() { ["heading"] = "swap" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Swap mode", answer);
        Assert.Contains("Swap thresholds", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBortableApi");

        foreach (var type in new[] { "Bortable<TItem>", "BortableEventArgs<TItem>", "BortablePull", "BortableDirection", "BitBortable" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference, and so does the class the Razor compiler makes
        // of the library's _Imports.razor.
        Assert.DoesNotContain("BortableService", index);
        Assert.DoesNotContain("BortableDragContext", index);
        Assert.DoesNotContain("IBortableZone", index);
        Assert.DoesNotContain("BortableMoveInfo", index);
        Assert.DoesNotContain("_Imports", index);
    }

    [TestMethod]
    public async Task The_component_reference_lists_its_parameters_with_their_defaults_and_nullability()
    {
        var reference = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Bortable" });

        Assert.StartsWith("# Bortable<TItem> (Component)", reference);
        Assert.Contains("`Bit.Bortable.Bortable<TItem>`", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Pull** : `BortablePull` = `BortablePull.Move`", reference);
        Assert.Contains("**Put** : `bool` = `true`", reference);
        Assert.Contains("**Animation** : `int` = `150`", reference);
        Assert.Contains("**SwapThreshold** : `double` = `1`", reference);
        Assert.Contains("**GhostClass** : `string` = `\"bit-bortable-ghost\"`", reference);
        Assert.Contains("**Items** : `IList<TItem>?`", reference);
        Assert.Contains("**ItemClassSelector** : `Func<TItem, string?>?`", reference);
        Assert.DoesNotContain("[truncated", reference);
    }

    [TestMethod]
    public async Task A_default_made_up_per_instance_and_an_unbound_event_have_none()
    {
        var reference = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Bortable" });

        // Every new list mints a Guid for its Id, so "the default" would be a value nobody ever gets.
        var idLine = reference.Split('\n').Single(line => line.StartsWith("- **Id**", StringComparison.Ordinal));

        Assert.DoesNotContain(" = `", idLine);
        Assert.Contains("**OnEnd** : `EventCallback<BortableEventArgs<TItem>>` - ", reference);
    }

    [TestMethod]
    public async Task A_generic_type_is_found_however_its_name_is_written()
    {
        var plain = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Bortable" });

        foreach (var name in new[] { "Bortable<TItem>", "Bortable<string>", "bortable", "Bortable`1", "Bortable{TItem}" })
        {
            Assert.AreEqual(plain, await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = name }), name);
        }
    }

    [TestMethod]
    public async Task The_event_arguments_and_the_registration_are_documented_from_the_assembly()
    {
        var args = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "BortableEventArgs" });
        var registration = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "BitBortable" });

        Assert.Contains("**FromId** : `string` = `\"\"`", args);
        Assert.Contains("**FromGroup** : `string?`", args);
        Assert.Contains("**Items** : `IReadOnlyList<TItem>` = `[]`", args);
        Assert.Contains("**CrossedLists** : `bool` = `false`", args);
        Assert.Contains("**AddBitBortableServices**(this IServiceCollection services) : `IServiceCollection`", registration);
        Assert.Contains("builder.Services.AddBitBortableServices();", registration);
    }

    [TestMethod]
    public async Task A_cref_to_a_member_of_the_generic_component_keeps_the_member()
    {
        // <see cref="Bortable{TItem}.Id"/> compiles to "P:Bit.Bortable.Bortable`1.Id"; cut at the arity
        // marker it would read "the Bortable of the source list" instead of its Id.
        var args = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "BortableEventArgs" });

        Assert.Contains("The Bortable.Id of the source list.", args);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var pull = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Pull" });
        var args = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "EventArgs" });

        Assert.Contains("BortablePull", pull);
        Assert.Contains("BortableEventArgs<TItem>", args);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBortableApi", new() { ["typeName"] = "Sortable" });

        Assert.Contains("prefixed 'Bortable'", answer);
    }

    // ---- Theme -------------------------------------------------------------

    [TestMethod]
    public async Task The_theme_tells_the_declared_inputs_from_the_optional_overrides()
    {
        var theme = await McpCall.TextAsync("GetBortableTheme");

        Assert.Contains("## Declared on :root", theme);
        Assert.Contains("| `--bit-bortable-accent-rgb` | `13, 110, 253` |", theme);
        Assert.Contains("| `--bit-bortable-ghost-opacity` | `0.4` |", theme);
        Assert.Contains("## Optional overrides", theme);
        Assert.Contains("| `--bit-bortable-chosen-bg` | `rgba(var(--bit-bortable-accent-rgb), 0.12)` |", theme);
        Assert.Contains("| `--bit-bortable-swap-outline` | `2px dashed rgba(var(--bit-bortable-accent-rgb), 0.6)` |", theme);

        // The declared ones come before the overrides, whichever order the table lists them in.
        Assert.IsLessThan(theme.IndexOf("## Optional overrides", StringComparison.Ordinal), theme.IndexOf("--bit-bortable-filtered-opacity", StringComparison.Ordinal));

        foreach (var property in BortableThemeCatalog.Properties) Assert.Contains($"| `{property.Name}` |", theme);
    }

    [TestMethod]
    public async Task The_theme_lists_the_drag_state_classes_with_the_parameters_that_replace_them()
    {
        var theme = await McpCall.TextAsync("GetBortableTheme");

        Assert.Contains("## Drag-state classes", theme);
        Assert.Contains("| `bit-bortable-ghost` | The item being dragged in its list, and the drop placeholder. | `GhostClass` |", theme);
        Assert.Contains("| `bit-bortable-hidden` | The original of a moved item while it is previewed in another list. | - |", theme);
        Assert.Contains("| `bit-bortable-selected` | Items in the multi-drag selection. | `SelectedClass` |", theme);

        foreach (var cssClass in BortableStylesheetClasses.Classes) Assert.Contains($"| `{cssClass.Name}` |", theme);
    }

    [TestMethod]
    public async Task The_accent_names_every_rule_it_reaches_through_a_fallback()
    {
        var accent = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = "accent-rgb" });

        Assert.StartsWith("# `--bit-bortable-accent-rgb`", accent);
        Assert.Contains("declared on `:root`", accent);
        Assert.Contains("through the fallback of `--bit-bortable-chosen-bg`", accent);
        Assert.Contains("through the fallback of `--bit-bortable-selected-ring`", accent);
        Assert.Contains("`Bortable<TItem>.SwapClass`", accent);
        Assert.Contains(":root { --bit-bortable-accent-rgb: 109, 40, 217; }", accent);
    }

    [TestMethod]
    public async Task An_optional_override_is_told_to_be_declared_nowhere_with_its_fallback()
    {
        var chosen = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = "--bit-bortable-chosen-bg" });

        Assert.Contains("optional override", chosen);
        Assert.Contains("declares it nowhere", chosen);
        Assert.Contains("**Default** (the fallback) = `rgba(var(--bit-bortable-accent-rgb), 0.12)`", chosen);
        Assert.Contains("**Reads** `--bit-bortable-accent-rgb`", chosen);
        Assert.Contains("`Bortable<TItem>.ChosenClass`", chosen);
    }

    [TestMethod]
    public async Task A_class_comes_with_its_rules_its_properties_and_the_parameter_that_replaces_it()
    {
        var ghost = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = "ghost" });
        var hidden = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = ".bit-bortable-hidden" });

        Assert.StartsWith("# `bit-bortable-ghost`", ghost);
        Assert.Contains("**Replaced by** `GhostClass`", ghost);
        Assert.Contains("`.bit-bortable-ghost { opacity: var(--bit-bortable-ghost-opacity) }`", ghost);
        Assert.Contains("`--bit-bortable-ghost-opacity`", ghost);

        Assert.StartsWith("# `bit-bortable-hidden`", hidden);
        Assert.Contains("Always rendered under this name", hidden);
        Assert.Contains("display: none !important", hidden);
    }

    [TestMethod]
    public async Task An_unknown_name_suggests_the_close_ones()
    {
        var close = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = "swap" });
        var none = await McpCall.TextAsync("GetBortableTheme", new() { ["name"] = "--zebra-stripes" });

        Assert.Contains("Did you mean", close);
        Assert.Contains("--bit-bortable-swap-bg", close);
        Assert.Contains("bit-bortable-swap-highlight", close);
        Assert.Contains("has no custom property or class called", none);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_checklist_the_registration_the_stylesheet_and_the_guide_on_its_hosting_model()
    {
        foreach (var mode in BortableSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBortableSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.Bortable", guide, mode);
            Assert.Contains("@using Bit.Bortable", guide, mode);
            Assert.Contains("AddBitBortableServices()", guide, mode);
            Assert.Contains("_content/Bit.Bortable/bit-bortable.css", guide, mode);
            Assert.Contains("interactive", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
            Assert.Contains("### Install", guide, mode);
            Assert.Contains("### Register the services", guide, mode);
            Assert.Contains("### Reference the stylesheet", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_server_mode_explains_the_round_trip()
    {
        var guide = await McpCall.TextAsync("GetBortableSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("SignalR", guide);
        Assert.Contains("### Blazor Server", guide, "The guide's own section on Blazor Server should be quoted.");
        Assert.Contains("@rendermode InteractiveServer", guide);
        Assert.Contains("builder.Services.AddBitBortableServices();", guide);
    }

    [TestMethod]
    public async Task The_web_app_modes_register_the_services_in_both_projects()
    {
        foreach (var mode in new[] { "wasm", "auto" })
        {
            var guide = await McpCall.TextAsync("GetBortableSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("BOTH `Program.cs` files", guide, mode);
            Assert.Contains("### Blazor Web App", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBortableSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BortableSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("_content/Bit.Bortable/bit-bortable.css", BortableSourceCatalog.GetSourceFile("Demo/Server/Components/App.razor")!);
        Assert.Contains("AddBitBortableServices", BortableSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBortableSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BortableSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_pages_the_component_and_the_stylesheet_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBortableSourceFile");

        Assert.Contains("Demo/Client/Pages/Shared.razor", index);
        Assert.Contains("Demo/Client/Pages/NestedList.razor", index);
        Assert.Contains(BortableSourceCatalog.ComponentCodePath, index);
        Assert.Contains(BortableSourceCatalog.ComponentMarkupPath, index);
        Assert.Contains(BortableSourceCatalog.StylesheetPath, index);
        Assert.Contains("`/shared`", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBortableSourceFile", new() { ["path"] = "Demo/Client/Pages/Shared.razor" });

        Assert.Contains("<Bortable", file);
        Assert.Contains("@page \"/shared\"", file);
    }

    [TestMethod]
    public async Task A_page_is_found_by_its_route()
    {
        var byRoute = await McpCall.TextAsync("GetBortableSourceFile", new() { ["path"] = "/swap" });
        var byPath = await McpCall.TextAsync("GetBortableSourceFile", new() { ["path"] = "Demo/Client/Pages/SwapDemo.razor" });

        Assert.AreEqual(byPath, byRoute);
    }

    [TestMethod]
    public async Task The_sorting_engine_is_served_whole()
    {
        var engine = await McpCall.TextAsync("GetBortableSourceFile", new() { ["path"] = BortableSourceCatalog.ComponentCodePath });

        Assert.Contains("private bool CanAccept(BortableDragContext ctx)", engine);
        Assert.DoesNotContain("[truncated", engine);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBortableSourceFile", new() { ["path"] = "SwapDemo" });

        Assert.Contains("Demo/Client/Pages/SwapDemo.razor", answer);
    }
}
