using Bit.BlazeDb.Demo.Server.Services;

namespace Bit.BlazeDb.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBlazeDbGuideSection");

        foreach (var heading in new[] { "Install", "Quick start", "Hosting models", "Encryption at rest", "Storage quota", "Multiple tabs", "EF Core", "Schema evolution", "Compile-time diagnostics" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBlazeDbGuideSection", new() { ["heading"] = "ENCRYPTION rest" });

        Assert.StartsWith("## Encryption at rest", section);
        Assert.Contains("BlazeDbEncryptedStorage", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBlazeDbGuideSection", new() { ["heading"] = "Quick start" });

        Assert.Contains("### Rows are live objects", section);
        Assert.Contains("### Threading model", section);
        Assert.DoesNotContain("## Hosting models", section);
    }

    [TestMethod]
    public async Task Every_section_is_reached_through_its_key()
    {
        foreach (var section in BlazeDbSourceCatalog.GuideSections)
        {
            var text = await McpCall.TextAsync("GetBlazeDbGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBlazeDbGuideSection", new() { ["heading"] = "tabs" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Multiple tabs", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_of_every_package_and_not_the_internal_ones()
    {
        var index = await McpCall.TextAsync("GetBlazeDbApi");

        foreach (var package in new[] { "## Bit.BlazeDb", "## Bit.BlazeDb.Browser", "## Bit.BlazeDb.EntityFrameworkCore" })
        {
            Assert.Contains(package, index);
        }

        foreach (var type in new[] { "BlazeDbDatabase", "BlazeDbDatabaseOptions", "BlazeDbTable<TKey, TRow>", "BlazeDbQuery<TKey, TRow>", "BlazeDbOpfsStorage", "BlazeDbIndexedDbStorage", "BlazeDbTabSync", "BlazeDbDbContextOptionsExtensions" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("BlazeDbWalManager", index);
        Assert.DoesNotContain("BlazeDbOpfsInterop", index);
        Assert.DoesNotContain("BlazeDbQueryCompiler", index);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var reference = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbDatabaseOptions" });

        Assert.StartsWith("# BlazeDbDatabaseOptions (Class)", reference);
        Assert.Contains("package `Bit.BlazeDb`", reference);
        Assert.Contains("**FlushInterval** : `TimeSpan` = `TimeSpan.Parse(\"00:00:00.1000000\")`", reference);
        Assert.Contains("**CheckpointWalSize** : `long` = `4194304`", reference);
        Assert.Contains("**ReadOnly** : `bool` = `false`", reference);
    }

    [TestMethod]
    public async Task A_generic_type_is_found_with_or_without_its_arguments()
    {
        var bare = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbTable" });
        var spelled = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbTable<TKey, TRow>" });

        Assert.StartsWith("# BlazeDbTable<TKey, TRow>", bare);
        Assert.AreEqual(bare, spelled);
        Assert.Contains("- **Lookup<TIndexKey>**", bare);
        Assert.Contains("- **UpdateInPlace**", bare);
    }

    [TestMethod]
    public async Task The_arity_picks_between_a_generic_type_and_its_non_generic_base()
    {
        var generic = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbTableDescriptor<TKey, TRow>" });
        var plain = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbTableDescriptor" });

        Assert.StartsWith("# BlazeDbTableDescriptor<TKey, TRow>", generic);
        Assert.StartsWith("# BlazeDbTableDescriptor (Class)", plain);
    }

    [TestMethod]
    public async Task The_ef_core_extensions_are_documented_with_their_overloads()
    {
        var reference = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "BlazeDbDbContextOptionsExtensions" });

        Assert.Contains("package `Bit.BlazeDb.EntityFrameworkCore`", reference);
        Assert.Contains("**UseBlazeDb**(this DbContextOptionsBuilder optionsBuilder)", reference);
        Assert.Contains("**UseBlazeDb**(this DbContextOptionsBuilder optionsBuilder, IBlazeDbStorage storage)", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "OpfsStorage" });

        Assert.Contains("BlazeDbOpfsStorage", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBlazeDbApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'BlazeDb'", answer);
        Assert.Contains("InspectBlazeDbTable", answer);
    }

    // ---- Diagnostics -------------------------------------------------------

    [TestMethod]
    public async Task The_diagnostics_table_lists_every_descriptor_of_the_generator()
    {
        var table = await McpCall.TextAsync("GetBlazeDbDiagnostics");

        Assert.IsNotEmpty(BlazeDbDiagnosticsReference.Descriptors);
        foreach (var descriptor in BlazeDbDiagnosticsReference.Descriptors) Assert.Contains($"| {descriptor.Id} | {descriptor.DefaultSeverity} |", table);
    }

    [TestMethod]
    public async Task A_diagnostic_comes_with_its_cause_and_both_samples()
    {
        var entry = await McpCall.TextAsync("GetBlazeDbDiagnostics", new() { ["code"] = "BLZ001" });

        Assert.StartsWith("# BLZ001 - Table type must be partial", entry);
        Assert.Contains("Severity: **Error**", entry);
        Assert.Contains("## Rejected", entry);
        Assert.Contains("## Fix", entry);
        Assert.Contains("`partial`", entry, "The cause is written as HTML for the site and should reach an agent as Markdown.");
        Assert.DoesNotContain("<code>", entry);
    }

    [TestMethod]
    public async Task A_diagnostic_is_found_by_its_number_alone()
    {
        var full = await McpCall.TextAsync("GetBlazeDbDiagnostics", new() { ["code"] = "BLZ013" });

        Assert.AreEqual(full, await McpCall.TextAsync("GetBlazeDbDiagnostics", new() { ["code"] = "blz13" }));
        Assert.AreEqual(full, await McpCall.TextAsync("GetBlazeDbDiagnostics", new() { ["code"] = "13" }));
        Assert.Contains("Severity: **Warning**", full);
        Assert.Contains("#pragma warning disable BLZ013", full);
    }

    [TestMethod]
    public async Task An_unknown_diagnostic_lists_the_real_ones()
    {
        var answer = await McpCall.TextAsync("GetBlazeDbDiagnostics", new() { ["code"] = "BLZ999" });

        Assert.Contains("no diagnostic 'BLZ999'", answer);
        Assert.Contains("BLZ001", answer);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Every_hosting_model_gets_the_checklist_and_the_guide_on_it()
    {
        foreach (var model in BlazeDbSetupGuide.HostingModels)
        {
            var guide = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = model });

            Assert.Contains("dotnet add package Bit.BlazeDb", guide, model);
            Assert.Contains("Bit.BlazeDb.Generators", guide, model);
            Assert.Contains("## Checklist", guide, model);
            Assert.Contains("## From the guide", guide, model);
        }
    }

    [TestMethod]
    public async Task Only_the_browser_models_bring_in_the_browser_package()
    {
        foreach (var model in new[] { "wasm", "standalone-wasm", "auto" })
        {
            var guide = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = model });

            Assert.Contains("`Bit.BlazeDb.Browser`", guide, model);
            Assert.Contains("prerender", guide, model);
        }

        foreach (var model in new[] { "server", "hybrid", "desktop" })
        {
            var guide = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = model });

            Assert.Contains("BlazeDbFileStorage", guide, model);
            Assert.Contains("`Bit.BlazeDb.Browser` does not apply here", guide, model);
        }
    }

    [TestMethod]
    public async Task The_server_model_explains_the_threading_model()
    {
        var guide = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = "server" });

        Assert.Contains("SemaphoreSlim", guide);
        Assert.Contains("#### Threading model", guide, "The quoted guide section should be demoted under 'From the guide'.");
    }

    [TestMethod]
    public async Task The_webassembly_model_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = "wasm" });

        Assert.Contains(BlazeDbSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimStart('﻿').TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Components/App.razor`", guide);
        Assert.Contains("### `Demo/Client/Data/DemoDbService.cs`", guide);
        Assert.Contains("prerender: false", guide);
    }

    [TestMethod]
    public async Task An_unknown_hosting_model_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past
        // it anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBlazeDbSetupGuide", new() { ["hostingModel"] = "electron" });

        Assert.Contains("'electron' is not a known hosting model", answer);
        foreach (var model in BlazeDbSetupGuide.HostingModels) Assert.Contains(model, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_and_models_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBlazeDbSourceFile");

        Assert.Contains("Demo/Client/Pages/Todos.razor", index);
        Assert.Contains("Demo/Client/Pages/EfCore.razor", index);
        Assert.Contains("Demo/Client/Data/DemoModels.cs", index);
        Assert.Contains("Demo/Server/Program.cs", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
        Assert.DoesNotContain("CodeHighlighter.cs", index);
        Assert.DoesNotContain("Icon.razor", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBlazeDbSourceFile", new() { ["path"] = "Demo/Client/Data/DemoModels.cs" });

        Assert.Contains("[BlazeDbTable(\"todos\")]", file);
        Assert.Contains("public partial class TodoEntry", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBlazeDbSourceFile", new() { ["path"] = "Durability" });

        Assert.Contains("Demo/Client/Pages/Durability.razor", answer);
    }
}
