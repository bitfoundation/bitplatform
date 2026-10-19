using Bit.Borm.Demo.Server.Services;

namespace Bit.Borm.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBormGuideSection");

        foreach (var heading in new[] { "Getting started", "Generating a schema from a C# type", "The fluent builder", "JSON Schema", "Validation", "Conditional fields and wizards", "Async and cascading options", "Computed values", "Layout", "Custom renderers", "The BormView component", "BormState at a glance", "Theming", "Hosting models" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBormGuideSection", new() { ["heading"] = "ASYNC cascading options" });

        Assert.StartsWith("## Async and cascading options", section);
        Assert.Contains("OptionsFrom", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBormGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### Install", section);
        Assert.Contains("### A first form", section);
        Assert.Contains("### Ids on the page", section);
        Assert.DoesNotContain("## Generating a schema from a C# type", section);
    }

    [TestMethod]
    public async Task A_sub_section_is_reached_on_its_own()
    {
        var section = await McpCall.TextAsync("GetBormGuideSection", new() { ["heading"] = "Untrusted documents" });

        Assert.StartsWith("### Untrusted documents", section);
        Assert.Contains("TryImport", section);
        Assert.DoesNotContain("### Extensions and export", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_reached_through_its_key()
    {
        // The README may use no heading twice; every one that it does use twice has to answer through its key.
        foreach (var section in BormSourceCatalog.GuideSections.Where(section => section.Key != section.Heading))
        {
            var text = await McpCall.TextAsync("GetBormGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBormGuideSection", new() { ["heading"] = "schema" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("'JSON Schema'", answer);
        Assert.Contains("'Checking a schema'", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBormApi");

        foreach (var type in new[] { "BormView", "BormField", "BormState", "BormDefinition", "BormFieldDefinition", "BormFieldBuilder", "BormBuilder", "BormBuilder<TModel>", "BormSchemaGenerator", "BormJsonSchemaImporter", "BormJsonSchemaExporter", "IBormFieldRendererRegistry", "IBormMessageProvider", "BormMessageKeys" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // The integration ships in a package of its own, and says so.
        Assert.Contains("**BormFluentValidationIntegration** (Static class, package Bit.Borm.FluentValidation)", index);

        // Internal machinery, and what the Razor compiler makes of _Imports.razor, stay out of the reference.
        Assert.DoesNotContain("BormUuidRule", index);
        Assert.DoesNotContain("BormAdditionalPatternRule", index);
        Assert.DoesNotContain("_Imports", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormView" });

        Assert.StartsWith("# BormView (Component)", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**FocusFirstError** : `bool` = `true`", reference);
        Assert.Contains("**AllowStepNavigation** : `bool` = `true`", reference);
        Assert.Contains("**TitleLevel** : `int` = `2`", reference);
        Assert.Contains("**ShowErrorSummary** : `bool` = `false`", reference);
        Assert.Contains("**OnValidSubmit** : `EventCallback<BormState>` - ", reference);
    }

    [TestMethod]
    public async Task A_state_built_from_arguments_still_reports_the_defaults_a_caller_can_change()
    {
        // BormState has no parameterless constructor; its defaults are read off one built from the
        // plainest arguments that satisfy it - and what was passed in is not reported as a default.
        var reference = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormState" });

        Assert.StartsWith("# BormState (Class)", reference);
        Assert.Contains("**IdPrefix** : `string` = `\"borm_\"`", reference);
        Assert.Contains("**ValidationTrigger** : `BormValidationTrigger` = `BormValidationTrigger.OnChange`", reference);
        Assert.Contains("**SingleErrorPerField** : `bool` = `false`", reference);

        var definitionLine = reference.Split('\n').Single(line => line.StartsWith("- **Definition**", StringComparison.Ordinal));
        Assert.DoesNotContain(" = `", definitionLine);

        Assert.DoesNotContain("[truncated", reference, "BormState's reference is among the longest answers, and has to arrive whole.");
    }

    [TestMethod]
    public async Task A_field_definition_reports_its_defaults_but_not_what_its_constructor_was_given()
    {
        var reference = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormFieldDefinition" });

        Assert.Contains("**ShowLabel** : `bool` = `true`", reference);
        Assert.Contains("**UpdateOn** : `BormUpdateTrigger` = `BormUpdateTrigger.Change`", reference);
        Assert.Contains("**Validators** : `IList<IBormValidationRule>` = `[]`", reference);

        foreach (var member in new[] { "Name", "Type", "IsChoice", "IsContainer" })
        {
            var line = reference.Split('\n').Single(line => line.StartsWith($"- **{member}**", StringComparison.Ordinal));

            Assert.DoesNotContain(" = `", line, $"{member} depends on the constructor's arguments, so it has no default.");
        }
    }

    [TestMethod]
    public async Task The_typed_and_the_untyped_builder_are_told_apart_by_their_argument_list()
    {
        var typed = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormBuilder<TModel>" });
        var untyped = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormBuilder" });

        Assert.StartsWith("# BormBuilder<TModel> (Class)", typed);
        Assert.Contains("Expression<Func<TModel, ", typed);
        Assert.StartsWith("# BormBuilder (Class)", untyped);
        Assert.Contains("- **ArrayOf**", untyped);
    }

    [TestMethod]
    public void A_member_of_a_generic_type_keeps_its_documentation()
    {
        // A generic type's members carry the arity marker in the middle of their documentation id
        // ("BormBuilder`1.Field``1"); the reference must still find their text.
        var details = BormApiCatalog.GetTypeDetails("BormBuilder<TModel>")!;

        Assert.IsTrue(details.Members.Any(member => member.Kind == "Method" && member.Summary is not null), "None of BormBuilder<TModel>'s methods came with its documentation.");
        Assert.IsNotNull(details.Summary);
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_or_a_delegate_is_left_out()
    {
        var record = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormSchemaDiagnostic" });
        var callback = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormChangeHandler" });

        Assert.Contains("**Severity** : `BormSchemaDiagnosticSeverity` = `BormSchemaDiagnosticSeverity.Error`", record);
        Assert.DoesNotContain("- **Equals**", record);
        Assert.DoesNotContain("- **GetHashCode**", record);
        Assert.DoesNotContain("- **Deconstruct**", record);
        Assert.DoesNotContain("<Clone>", record);

        Assert.StartsWith("# BormChangeHandler (Delegate)", callback);
        Assert.Contains("- **Invoke**", callback);
        Assert.DoesNotContain("BeginInvoke", callback);
        Assert.DoesNotContain("EndInvoke", callback);
    }

    [TestMethod]
    public async Task A_type_of_the_integration_package_says_which_package_it_ships_in()
    {
        var reference = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "BormFluentValidationIntegration" });

        Assert.Contains("package `Bit.Borm.FluentValidation`", reference);
        Assert.Contains("- **UseFluentValidation**", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "Definition" });

        Assert.Contains("BormDefinition", answer);
        Assert.Contains("BormFieldDefinition", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBormApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Borm'", answer);
    }

    // ---- Theme -------------------------------------------------------------

    [TestMethod]
    public async Task The_theme_table_gives_every_property_its_light_and_dark_value()
    {
        var theme = await McpCall.TextAsync("GetBormTheme");

        Assert.Contains("| `--bit-borm-surface` | `#ffffff` | `#111827` |", theme);
        Assert.Contains("| `--bit-borm-accent` | `#4f46e5` | same |", theme);
        Assert.Contains("## Colour modes", theme);
        Assert.Contains("@media (prefers-color-scheme: dark)", theme);
        Assert.Contains("[data-theme=\"dark\"]", theme);

        foreach (var property in BormThemeCatalog.Properties) Assert.Contains($"| `{property.Name}` |", theme);
    }

    [TestMethod]
    public async Task A_property_dark_mode_changes_comes_with_an_override_for_every_colour_mode()
    {
        var surface = await McpCall.TextAsync("GetBormTheme", new() { ["property"] = "surface" });

        Assert.StartsWith("# `--bit-borm-surface`", surface);
        Assert.Contains(".bit-borm-input { background: var(--bit-borm-surface) }", surface);
        Assert.Contains(".bit-borm-form { --bit-borm-surface: <light value>; }", surface);
        Assert.Contains("@media (prefers-color-scheme: dark) { .bit-borm-form { --bit-borm-surface: <dark value>; } }", surface);
        Assert.Contains("[data-theme=\"dark\"] .bit-borm-form, .bit-borm-form[data-theme=\"dark\"] { --bit-borm-surface: <dark value>; }", surface);
        Assert.Contains("[data-theme=\"light\"] .bit-borm-form, .bit-borm-form[data-theme=\"light\"] { --bit-borm-surface: <light value>; }", surface);
    }

    [TestMethod]
    public async Task A_property_the_dark_theme_leaves_alone_needs_one_override_and_names_what_follows_it()
    {
        var accent = await McpCall.TextAsync("GetBormTheme", new() { ["property"] = "--bit-borm-accent" });

        Assert.Contains("the dark theme leaves this property alone", accent);
        Assert.Contains(".bit-borm-form { --bit-borm-accent: <value>; }", accent);
        Assert.DoesNotContain("<dark value>", accent);
        Assert.Contains("`--bit-borm-border-focus`", accent, "border-focus defaults to the accent, so it moves with it.");
    }

    [TestMethod]
    public async Task An_unknown_property_suggests_the_close_ones()
    {
        var close = await McpCall.TextAsync("GetBormTheme", new() { ["property"] = "surf" });
        var none = await McpCall.TextAsync("GetBormTheme", new() { ["property"] = "--zebra-stripes" });

        Assert.Contains("Did you mean", close);
        Assert.Contains("--bit-borm-surface-muted", close);
        Assert.Contains("declares no custom property called", none);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_hosting_model_gets_the_checklist_the_registration_the_stylesheet_and_the_guide()
    {
        foreach (var model in BormSetupGuide.HostingModels)
        {
            var guide = await McpCall.TextAsync("GetBormSetupGuide", new() { ["hostingModel"] = model });

            Assert.Contains("dotnet add package Bit.Borm", guide, model);
            Assert.Contains("@using Bit.Borm", guide, model);
            Assert.Contains("AddBorm()", guide, model);
            Assert.Contains("EVERY container", guide, model);
            Assert.Contains("_content/Bit.Borm/bit-borm.css", guide, model);
            Assert.Contains("Bit.Borm.FluentValidation", guide, model);
            Assert.Contains("## From the guide", guide, model);
            Assert.Contains("### Install", guide, model);
            Assert.Contains("### Hosting models", guide, model);
            Assert.Contains("NO script to add", guide, model);
            Assert.DoesNotContain("bit-borm.js", guide, model);
            Assert.DoesNotContain("[truncated", guide, model);
        }
    }

    [TestMethod]
    public async Task The_server_model_explains_the_round_trip()
    {
        var guide = await McpCall.TextAsync("GetBormSetupGuide", new() { ["hostingModel"] = "server" });

        Assert.Contains("SignalR", guide);
        Assert.Contains("@rendermode InteractiveServer", guide);
        Assert.Contains("BormSchemaGenerator.Generate<Contact>()", guide);
    }

    [TestMethod]
    public async Task The_web_app_model_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBormSetupGuide", new() { ["hostingModel"] = "web-app" });

        Assert.Contains(BormSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("### `Demo/Client/Extensions/IServiceCollectionExtensions.cs`", guide);
        Assert.Contains("_content/Bit.Borm/bit-borm.css", BormSourceCatalog.GetSourceFile("Demo/Server/Components/App.razor")!);
        Assert.Contains("AddBorm(", BormSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!);
        Assert.Contains("leave those lines", guide, "The demo's Program.cs maps this MCP server, which an app being set up does not want.");
    }

    [TestMethod]
    public async Task The_hosting_model_is_read_under_the_names_people_call_it()
    {
        var wasm = await McpCall.TextAsync("GetBormSetupGuide", new() { ["hostingModel"] = "webassembly" });

        Assert.AreEqual(BormSetupGuide.Get("webassembly"), BormSetupGuide.Get("Standalone WASM"));
        Assert.AreEqual(BormSetupGuide.Get("web-app"), BormSetupGuide.Get("InteractiveAuto"));
        Assert.AreEqual(BormSetupGuide.Get("hybrid"), BormSetupGuide.Get("MAUI"));
        Assert.Contains("wwwroot/index.html", wasm);
    }

    [TestMethod]
    public async Task An_unknown_hosting_model_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBormSetupGuide", new() { ["hostingModel"] = "electron" });

        Assert.Contains("'electron' is not a known hosting model", answer);
        foreach (var model in BormSetupGuide.HostingModels) Assert.Contains(model, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_pages_what_they_share_and_the_stylesheet_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBormSourceFile");

        Assert.Contains("Demo/Client/Pages/WizardDemo.razor", index);
        Assert.Contains("**Multi-step wizard**", index);
        Assert.Contains("`/wizard`", index);
        Assert.Contains("Demo/Client/Forms/Models.cs", index);
        Assert.Contains("Demo/Client/Renderers/StarRatingInput.razor", index);
        Assert.Contains(BormSourceCatalog.StylesheetPath, index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBormSourceFile", new() { ["path"] = "Demo/Client/Pages/WizardDemo.razor" });

        Assert.Contains("<BormView", file);
        Assert.Contains("@page \"/wizard\"", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBormSourceFile", new() { ["path"] = "Cascading" });

        Assert.Contains("Demo/Client/Pages/CascadingOptionsDemo.razor", answer);
    }
}
