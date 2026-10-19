using Bit.Bsqlite.Demo.Server.Services;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// Every reference tool's answers over the protocol: the thing a key names, the index a missing key
/// answers with, and the guidance a wrong key gets instead of an error.
/// </summary>
[TestClass]
public class McpToolTests
{
    // ---- Documents ---------------------------------------------------------

    [TestMethod]
    public async Task The_document_index_lists_the_readme_the_guides_and_the_packages()
    {
        var index = await McpCall.TextAsync("GetBsqliteDocument");

        foreach (var name in new[] { "readme", "getting-started", "storage", "live-queries", "architecture", "limits", "extending", "Bit.Bsqlite.Testing", "Bit.Bsqlite.Storage.Opfs" })
        {
            Assert.Contains($"- **{name}** - ", index);
        }

        Assert.Contains("  - Selection (", index, "A guide's sections are listed under it.");
    }

    [TestMethod]
    public void The_guides_are_listed_in_the_order_the_readme_links_them()
    {
        var guides = BsqliteSourceCatalog.Documents.Where(document => document.Kind == "Guide").Select(document => document.Name).ToArray();

        CollectionAssert.AreEqual(new[] { "getting-started", "storage", "live-queries", "architecture", "limits", "extending" }, guides);
    }

    [TestMethod]
    public async Task A_whole_document_comes_back_verbatim()
    {
        var storage = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "storage" });

        Assert.AreEqual(BsqliteSourceCatalog.GetDocument("storage"), storage);
        Assert.StartsWith("# Storage", storage);
    }

    [TestMethod]
    public async Task A_document_is_found_whatever_it_is_called()
    {
        foreach (var name in new[] { "STORAGE", "storage.md", "docs/storage.md" })
        {
            Assert.StartsWith("# Storage", await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = name }), name);
        }

        // A package README by the part of its name that says something.
        Assert.StartsWith("# Bit.Bsqlite.Testing", await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "testing" }));
        Assert.StartsWith("# Bit.Bsqlite.Storage.Opfs", await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "opfs" }));
        Assert.StartsWith("# bit Bsqlite", await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "README" }));
    }

    [TestMethod]
    public async Task A_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "storage", ["section"] = "first party BACKENDS" });

        Assert.StartsWith("## First-party backends", section);
        Assert.Contains("`opfs`", section);
        Assert.DoesNotContain("## Selection", section, "A section stops at the next heading of its rank.");
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections()
    {
        var section = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "getting-started", ["section"] = "Use EF Core" });

        Assert.Contains("### Without the factory", section);
        Assert.DoesNotContain("## Use ADO.NET", section);
    }

    [TestMethod]
    public async Task An_unknown_section_lists_the_close_ones_or_all_of_them()
    {
        var close = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "storage", ["section"] = "backends" });
        var none = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "storage", ["section"] = "zebra" });

        Assert.Contains("Did you mean", close);
        Assert.Contains("'First-party backends'", close);
        Assert.Contains("Its sections are", none);
        Assert.Contains("'Writing a provider'", none);
    }

    [TestMethod]
    public async Task An_unknown_document_lists_the_real_ones()
    {
        var answer = await McpCall.TextAsync("GetBsqliteDocument", new() { ["name"] = "changelog" });

        Assert.Contains("There is no Bit.Bsqlite document called 'changelog'", answer);
        foreach (var document in BsqliteSourceCatalog.Documents) Assert.Contains($"'{document.Name}'", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_groups_the_types_by_package_and_namespace()
    {
        var index = await McpCall.TextAsync("GetBsqliteApi");

        foreach (var package in new[] { "Bit.Bsqlite", "Bit.Bsqlite.Storage.Opfs", "Bit.Bsqlite.Storage.IndexedDb", "Bit.Bsqlite.Storage.CacheStorage", "Bit.Bsqlite.Testing" })
        {
            Assert.Contains($"## Package {package}", index);
        }

        Assert.Contains("### `using Microsoft.Extensions.DependencyInjection;`", index, "The DI entry points live in Microsoft's namespace.");

        foreach (var type in new[] { "BsqliteDatabase", "BsqliteConnection", "IBsqliteStorageProvider", "BsqliteStorageSelectionBuilder", "IBsqliteDbContextFactory<TContext>", "BsqliteInProcessTransport", "BitBsqlite" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference.
        Assert.DoesNotContain("BsqlitePclProvider", index);
        Assert.DoesNotContain("BsqliteSaveChangesGate", index);
        Assert.DoesNotContain("BsqliteOpfsStorageAdmin", index);
    }

    [TestMethod]
    public async Task A_type_reference_names_its_namespace_and_package()
    {
        var reference = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteOpfsStorageProvider" });

        Assert.StartsWith("# BsqliteOpfsStorageProvider (Class)", reference);
        Assert.Contains("package `Bit.Bsqlite.Storage.Opfs`", reference);
        Assert.Contains("`using Bit.Bsqlite.Storage.Opfs;`", reference);
        Assert.Contains("implements `IBsqliteStorageProvider`", reference);
        Assert.Contains("**ProviderName** : `string` = `\"opfs\"`", reference, "A constant reads as its real value.");
        Assert.Contains("## Constructor", reference);
        Assert.Contains("(IJSRuntime js = null)", reference);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var options = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteWorkerTransportOptions" });

        Assert.Contains("**EnableForeignKeys** : `bool` = `true`", options);
    }

    [TestMethod]
    public async Task A_required_member_is_marked_and_given_no_made_up_default()
    {
        var reference = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteStorageCapabilities" });

        var build = reference.Split('\n').Single(line => line.StartsWith("- **RequiredBuild**", StringComparison.Ordinal));
        var contexts = reference.Split('\n').Single(line => line.StartsWith("- **SupportedContexts**", StringComparison.Ordinal));

        Assert.Contains("**(required)**", build);
        Assert.DoesNotContain(" = `", build, "A required member has no default: the compiler refuses to leave it out.");
        Assert.Contains("= `BsqliteExecutionContexts.DedicatedWorker`", contexts);
    }

    [TestMethod]
    public async Task The_conformance_kit_lists_the_protected_member_a_test_class_overrides()
    {
        var reference = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteStorageProviderConformanceTests" });

        Assert.Contains("- protected **CreateProvider**() : `IBsqliteStorageProvider`", reference);
        Assert.Contains("`using Bit.Bsqlite.Storage.ConformanceTests;`", reference);
    }

    [TestMethod]
    public async Task An_inheritdoc_member_carries_the_documentation_it_inherits()
    {
        var reference = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteInMemoryStorageProvider" });

        var nameLine = reference.Split('\n').Single(line => line.StartsWith("- **Name**", StringComparison.Ordinal));

        Assert.Contains("Stable identifier", nameLine, "BsqliteInMemoryStorageProvider.Name says <inheritdoc />, so its summary is the interface's.");
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        var reference = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "BsqliteStorageResolution" });

        Assert.DoesNotContain("- **Equals**", reference);
        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("EqualityContract", reference);
        Assert.DoesNotContain("<Clone>", reference);
    }

    [TestMethod]
    public async Task A_generic_type_is_found_with_or_without_its_type_parameters()
    {
        var bare = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "IBsqliteDbContextFactory" });
        var closed = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "IBsqliteDbContextFactory<AppDbContext>" });

        Assert.StartsWith("# IBsqliteDbContextFactory<TContext> (Interface)", bare);
        Assert.AreEqual(bare, closed);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "LiveQuery" });

        Assert.Contains("BsqliteLiveQuery", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBsqliteApi", new() { ["typeName"] = "Zebra" });

        Assert.Contains("prefixed 'Bsqlite'", answer);
    }

    // ---- Storage backends --------------------------------------------------

    [TestMethod]
    public async Task The_backend_table_lists_every_first_party_backend()
    {
        var table = await McpCall.TextAsync("GetBsqliteStorageProviders");

        foreach (var name in new[] { "opfs", "indexeddb", "cache-storage", "in-memory" }) Assert.Contains($"| `{name}` |", table);
    }

    [TestMethod]
    public async Task A_backend_comes_with_its_capabilities_modules_and_refused_statements()
    {
        var opfs = await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "opfs" });

        Assert.StartsWith("# BsqliteOpfsStorageProvider (`opfs`)", opfs);
        Assert.Contains("**RequiredBuild** = `Synchronous`", opfs);
        Assert.Contains("**SupportsMultiDatabaseTransactions** = `false`", opfs);
        Assert.Contains("`ATTACH DATABASE 'other.db' AS other` - refused: ATTACH is not available", opfs);
        Assert.Contains("`PRAGMA page_size = 8192` - accepted", opfs);
        Assert.Contains("bit-bsqlite-opfs-vfs.js", opfs);
        Assert.Contains("`opfs-coop-sync`", opfs, "The VFS name is read off the provider's own constant.");

        var memory = await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "in-memory" });

        Assert.Contains("VFS: none", memory);
        Assert.Contains("**IsPersistent** = `false`", memory);
    }

    [TestMethod]
    public async Task A_backend_is_found_by_its_type_name_and_as_a_person_spells_it()
    {
        var byName = await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "indexeddb" });

        Assert.AreEqual(byName, await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "BsqliteIndexedDbStorageProvider" }));
        Assert.AreEqual(byName, await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "IndexedDB" }));
        Assert.StartsWith("# BsqliteCacheStorageProvider", await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "Cache Storage" }));
    }

    [TestMethod]
    public async Task An_unknown_backend_lists_the_real_ones_and_the_way_to_write_one()
    {
        var answer = await McpCall.TextAsync("GetBsqliteStorageProviders", new() { ["provider"] = "localstorage" });

        Assert.Contains("No first-party storage backend is called 'localstorage'", answer);
        Assert.Contains("custom-provider", answer);
        foreach (var provider in BsqliteStorageCatalog.Providers) Assert.Contains(provider.Name, answer);
    }

    // ---- Setup guides ------------------------------------------------------

    [TestMethod]
    public async Task Every_scenario_gets_a_checklist_and_the_documentation_it_rests_on()
    {
        foreach (var scenario in BsqliteSetupGuide.Scenarios)
        {
            var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = scenario });

            Assert.StartsWith("# Setting Bsqlite up - ", guide, scenario);
            Assert.Contains("## Checklist", guide, scenario);
            Assert.Contains("## From the documentation", guide, scenario);
            Assert.DoesNotContain("[truncated", guide, scenario);

            foreach (var tool in ToolNames.MentionedIn(guide)) CollectionAssert.Contains(McpSurfaceTests.ExpectedTools, tool, $"'{scenario}' mentions '{tool}', which is not a tool.");
        }
    }

    [TestMethod]
    public async Task Every_section_and_file_a_guide_quotes_is_in_it()
    {
        // A guide leaves out a quote it cannot find, so a heading renamed or a file moved underneath it would
        // only make the guide quietly shorter. Each one is looked for in the text a client receives.
        foreach (var scenario in BsqliteSetupGuide.Scenarios)
        {
            var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = scenario });
            var (quotes, files) = BsqliteSetupGuide.Sources(scenario);

            Assert.IsNotEmpty(quotes, scenario);

            foreach (var (document, section) in quotes)
            {
                Assert.IsNotNull(BsqliteSourceCatalog.GetDocumentSection(document, section), $"'{scenario}' quotes '{document} > {section}', which does not exist.");
                Assert.Contains($"_{document}:_", guide, scenario);
            }

            foreach (var file in files)
            {
                Assert.IsNotNull(BsqliteSourceCatalog.GetSourceFile(file), $"'{scenario}' quotes '{file}', which is not embedded.");
                Assert.Contains($"### `{file}`", guide, scenario);
            }
        }
    }

    [TestMethod]
    public async Task The_hosting_guides_register_in_both_containers_and_open_only_when_interactive()
    {
        var wasm = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "wasm" });

        Assert.Contains("RendererInfo.IsInteractive", wasm);
        Assert.Contains("BOTH containers", wasm);
        Assert.Contains("dotnet add package Bit.Bsqlite", wasm);
        Assert.Contains("AddBitBsqliteServices", wasm);
        Assert.Contains("BsqliteSynchronousApiNotSupportedException", wasm);
        Assert.IsTrue(wasm.Split('\n').Any(line => line.TrimEnd() == "### Where it runs"), "The quoted README section should be demoted under 'From the documentation'.");

        var auto = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "auto" });

        Assert.Contains("OperatingSystem.IsBrowser()", auto);
        Assert.Contains("@rendermode InteractiveWebAssembly", auto);
    }

    [TestMethod]
    public async Task The_webassembly_guide_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "wasm" });

        Assert.Contains(BsqliteSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!.TrimStart('﻿').TrimEnd(), guide);
        Assert.Contains(BsqliteSourceCatalog.GetSourceFile("Demo/Client/Components/SessionLifecycle.razor")!.TrimStart('﻿').TrimEnd(), guide);
        Assert.Contains("### `Demo/Server/Program.cs`", guide);
    }

    [TestMethod]
    public async Task The_server_guide_says_it_is_not_supported()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "server" });

        Assert.Contains("not supported", guide);
        Assert.Contains("InteractiveWebAssembly", guide);
    }

    [TestMethod]
    public async Task The_besql_guide_puts_cache_storage_first_under_besqls_own_file_name()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "move-from-besql" });

        Assert.Contains(".Prefer(BsqliteCacheStorageProvider.ProviderName)", guide);
        Assert.Contains("AddDatabase(\"Offline-Client.db\")", guide);
        Assert.Contains("/data/cache/", guide);
        Assert.Contains("bit-besql.js", guide);
    }

    [TestMethod]
    public async Task The_storage_guide_tabulates_what_the_provider_types_declare()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "storage" });

        Assert.Contains("| `opfs` | `Bit.Bsqlite.Storage.Opfs` | Synchronous | true | false | refused | allowed |", guide);
        Assert.Contains("| `indexeddb` | `Bit.Bsqlite.Storage.IndexedDb` | AsyncCapable | true | false | allowed | refused |", guide);
        Assert.Contains("AllowNonPersistentFallback()", guide);
    }

    [TestMethod]
    public async Task The_provider_guide_lists_the_contract_read_off_the_interfaces()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "custom-provider" });

        foreach (var member in new[] { "Name", "Capabilities", "VfsModule", "Admin", "ProbeAsync", "ExistsAsync", "ListAsync", "DeleteAsync", "ExportAsync", "ImportAsync" })
        {
            Assert.Contains($" {member}", guide, member);
        }

        Assert.Contains("export async function register(", guide);
    }

    [TestMethod]
    public async Task The_testing_guide_uses_the_in_process_transport_and_the_kit()
    {
        var guide = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "testing" });

        Assert.Contains("new BsqliteInProcessTransport()", guide);
        Assert.Contains(": BsqliteStorageProviderConformanceTests", guide);
        Assert.Contains("UseBindingStore<BsqliteInMemoryStorageBindingStore>()", guide);
    }

    [TestMethod]
    public async Task An_unknown_scenario_lists_the_known_ones()
    {
        // [AllowedValues] puts the values in the schema for a client to honour; a value that gets past it
        // anyway is answered with the list rather than with an error.
        var answer = await McpCall.TextAsync("GetBsqliteSetupGuide", new() { ["scenario"] = "electron" });

        Assert.Contains("'electron' is not a known scenario", answer);
        foreach (var scenario in BsqliteSetupGuide.Scenarios) Assert.Contains(scenario, answer);
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_and_its_model_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBsqliteSourceFile");

        Assert.Contains("Demo/Client/Pages/Storage.razor", index);
        Assert.Contains("Demo/Client/Components/SessionLifecycle.razor", index);
        Assert.Contains("Demo/Data/AppDbContext.cs", index);
        Assert.Contains("Demo/Server/Program.cs", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFoundPage.razor", index);
        Assert.DoesNotContain("ErrorPage.razor", index);
        Assert.DoesNotContain("/Migrations/", index);
        Assert.DoesNotContain("/obj/", index);
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBsqliteSourceFile", new() { ["path"] = "Demo/Client/Pages/Storage.razor" });

        Assert.Contains("@page \"/storage\"", file);
        Assert.Contains("BsqliteStorageResolution", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBsqliteSourceFile", new() { ["path"] = "SessionLifecycle" });

        Assert.Contains("Demo/Client/Components/SessionLifecycle.razor", answer);
    }
}
