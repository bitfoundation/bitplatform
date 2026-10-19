using Bit.Bdnd.Demo.Server.Services;

namespace Bit.Bdnd.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBdndGuideSection");

        foreach (var heading in new[] { "Getting started", "The provider", "Draggables", "Drop targets", "Nesting", "Custom drag layers", "Drag handles", "Sortable lists", "Native drags", "The monitor", "Hosting models", "Troubleshooting", "Coming from React DnD", "Component reference" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_number_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "coming REACT dnd" });
        var singular = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "drop result" });

        Assert.StartsWith("## Coming from React DnD", section);
        Assert.Contains("useDrag", section);
        Assert.StartsWith("### Drop results", singular);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### Install", section);
        Assert.Contains("### Register the service", section);
        Assert.Contains("### A first drag and drop", section);
        Assert.DoesNotContain("## The provider", section);
    }

    [TestMethod]
    public async Task A_sub_section_is_reached_on_its_own()
    {
        var section = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "Nested drop targets" });

        Assert.StartsWith("### Nested drop targets", section);
        Assert.Contains("IsOverShallow", section);
        Assert.DoesNotContain("## Custom drag layers", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_reached_through_its_key()
    {
        // "Collected state" is a sub-section of both Draggables and Drop targets.
        var keyed = BdndSourceCatalog.GuideSections.Where(section => section.Key != section.Heading).ToArray();

        Assert.IsNotEmpty(keyed, "The README no longer repeats a heading; this test has nothing left to check.");

        foreach (var section in keyed)
        {
            var text = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }

        var dropTargets = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "Drop targets > Collected state" });

        Assert.Contains("IsOverShallow", dropTargets);
    }

    [TestMethod]
    public async Task A_heading_with_an_entity_in_the_readme_is_read_as_it_renders()
    {
        // The README writes "BdndDraggable&lt;TItem&gt;", which Markdown shows as BdndDraggable<TItem>.
        var section = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "BdndDraggable<TItem>" });

        Assert.Contains("| `Type` |", section);
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBdndGuideSection", new() { ["heading"] = "drag" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Drag handles", answer);
        Assert.Contains("Custom drag layers", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBdndApi");

        foreach (var type in new[] { "BdndProvider", "BdndDraggable<TItem>", "BdndDropTarget<TItem>", "BdndDragLayer<TItem>", "BdndDragHandle", "BdndDragDropManager", "IBdndMonitor", "BdndDropState<TItem>", "BdndDragEndContext<TItem>", "BdndNativeTypes", "BdndInterop", "BitBdnd" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery and the compiler's leftovers stay out of the reference.
        Assert.DoesNotContain("BdndDropTargetEntry", index);
        Assert.DoesNotContain("BdndDropTargetScope", index);
        Assert.DoesNotContain("BdndJsRegistration", index);
        Assert.DoesNotContain("BdndDragSourceOptions", index);
        Assert.DoesNotContain("_Imports", index);
    }

    [TestMethod]
    public async Task A_generic_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDraggable" });

        Assert.StartsWith("# BdndDraggable<TItem> (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**CanDrag** : `bool` = `true`", reference);
        Assert.Contains("**DropEffect** : `string` = `\"move\"`", reference);
        Assert.Contains("**Tag** : `string` = `\"div\"`", reference);
        Assert.Contains("**UseHandle** : `bool` = `false`", reference);
        Assert.Contains("**Type** : `string` **(required)**", reference);
        Assert.Contains("**OnEnd** : `EventCallback<BdndDragEndContext<TItem>>` - ", reference);
    }

    [TestMethod]
    public async Task The_entry_points_of_the_script_are_not_part_of_the_reference()
    {
        // Public for JS interop only, and [EditorBrowsable(Never)] for that reason.
        var draggable = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDraggable" });
        var target = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDropTarget<TItem>" });

        Assert.DoesNotContain("HandleDragEnd", draggable);
        Assert.DoesNotContain("HandleDragMove", draggable);
        Assert.DoesNotContain("HandleHoverChanged", target);
        Assert.DoesNotContain("HandleNativeDrop", target);
        Assert.Contains("**CanDropPredicate** : `Func<TItem, IBdndMonitor, bool>`", target);
    }

    [TestMethod]
    public async Task A_cref_to_a_member_of_a_generic_type_keeps_the_member()
    {
        // The documentation says <see cref="BdndDraggable{TItem}.ItemFactory"/>, compiled as
        // "P:Bit.Bdnd.BdndDraggable`1.ItemFactory" - which has to read "BdndDraggable.ItemFactory".
        var reference = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDraggable" });

        Assert.Contains("Ignored when BdndDraggable.ItemFactory is supplied.", reference);
    }

    [TestMethod]
    public async Task The_manager_is_its_own_monitor_and_a_generic_overload_is_kept()
    {
        var manager = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndDragDropManager" });
        var monitor = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "IBdndMonitor" });

        Assert.Contains("**Monitor** : `IBdndMonitor` = `this`", manager);
        Assert.Contains("- **GetItem**() : `object`", monitor);
        Assert.Contains("- **GetItem<T>**() : `T`", monitor);
        Assert.Contains("- **IsOver**(string handlerId, bool shallow = false) : `bool`", monitor);
    }

    [TestMethod]
    public async Task The_native_types_and_the_service_registration_are_documented()
    {
        var native = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BdndNativeTypes" });
        var services = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "BitBdnd" });

        Assert.Contains("**File** : `string` = `\"__NATIVE_FILE__\"`", native);
        Assert.Contains("**Url** : `string` = `\"__NATIVE_URL__\"`", native);
        Assert.Contains("- **AddBitBdndServices**(this IServiceCollection services) : `IServiceCollection`", services);
        Assert.Contains("builder.Services.AddBitBdndServices();", services);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "DropTarget" });

        Assert.Contains("BdndDropTarget<TItem>", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBdndApi", new() { ["typeName"] = "useDrag" });

        Assert.Contains("prefixed 'Bdnd'", answer);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_render_mode_gets_the_checklist_the_service_and_the_guide_on_its_hosting_model()
    {
        foreach (var mode in BdndSetupGuide.RenderModes)
        {
            var guide = await McpCall.TextAsync("GetBdndSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("dotnet add package Bit.Bdnd", guide, mode);
            Assert.Contains("@using Bit.Bdnd", guide, mode);
            Assert.Contains("AddBitBdndServices()", guide, mode);
            Assert.Contains("NO stylesheet", guide, mode);
            Assert.Contains("<BdndProvider>", guide, mode);
            Assert.Contains("## From the guide", guide, mode);
            Assert.Contains("### Install", guide, mode);
            Assert.Contains("### Register the service", guide, mode);
            Assert.DoesNotContain("bit-bdnd.css", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_web_app_modes_register_the_service_in_both_projects()
    {
        foreach (var mode in new[] { "wasm", "auto" })
        {
            var guide = await McpCall.TextAsync("GetBdndSetupGuide", new() { ["renderMode"] = mode });

            Assert.Contains("BOTH Program.cs", guide, mode);
            Assert.Contains("### Blazor Web App", guide, mode);
        }
    }

    [TestMethod]
    public async Task The_server_mode_explains_the_round_trip()
    {
        var guide = await McpCall.TextAsync("GetBdndSetupGuide", new() { ["renderMode"] = "server" });

        Assert.Contains("SignalR", guide);
        Assert.Contains("### Blazor Server", guide, "The guide's own section on Blazor Server should be quoted.");
        Assert.Contains("@rendermode InteractiveServer", guide);
        Assert.Contains("builder.Services.AddBitBdndServices();", guide);
    }

    [TestMethod]
    public async Task The_webassembly_mode_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBdndSetupGuide", new() { ["renderMode"] = "wasm" });

        Assert.Contains(BdndSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains(BdndSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("services.AddBitBdndServices();", guide);
    }

    [TestMethod]
    public async Task An_unknown_render_mode_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBdndSetupGuide", new() { ["renderMode"] = "electron" });

        Assert.Contains("'electron' is not a known render mode", answer);
        foreach (var mode in BdndSetupGuide.RenderModes) Assert.Contains(mode, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_pages_and_the_library_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBdndSourceFile");

        Assert.Contains("Demo/Client/Pages/Sortable/SortableCard.razor", index);
        Assert.Contains("Demo/Client/Pages/Nesting/DropTargets.razor", index);
        Assert.Contains("`/sortable/simple`", index);
        Assert.Contains("**Sortable - Simple**", index);
        Assert.Contains(BdndSourceCatalog.ScriptPath, index);
        Assert.Contains("Library/Components/BdndDropTarget.cs", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBdndSourceFile", new() { ["path"] = "Demo/Client/Pages/Sortable/SortableCard.razor" });

        Assert.Contains("<BdndDraggable", file);
        Assert.Contains("<BdndDropTarget", file);
        Assert.Contains("OnHover=\"HandleHover\"", file);
    }

    [TestMethod]
    public async Task A_library_razor_component_is_described_by_its_partial_class()
    {
        var index = await McpCall.TextAsync("GetBdndSourceFile");

        var line = index.Split('\n').Single(line => line.Contains("`Library/Components/BdndProvider.razor`", StringComparison.Ordinal));

        Assert.Contains("The root of a drag-and-drop context", line);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBdndSourceFile", new() { ["path"] = "SortableCard" });

        Assert.Contains("Demo/Client/Pages/Sortable/SortableCard.razor", answer);
    }
}
