using Bit.Bcrdt.Demo.Server.Services;

namespace Bit.Bcrdt.Tests.Mcp;

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
        var index = await McpCall.TextAsync("GetBcrdtGuideSection");

        foreach (var heading in new[] { "Getting started", "The five-minute version", "Choosing a data type", "Working with documents", "Binding to inputs", "Presence", "Offline and reconnection", "Persistence", "Undo", "Hosting models", "Transports and relays", "Extending it", "Architecture", "Correctness" })
        {
            Assert.Contains($"- {heading} (", index);
        }
    }

    [TestMethod]
    public async Task A_guide_section_is_found_regardless_of_case_punctuation_and_little_words()
    {
        var section = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = "choosing DATA type" });

        Assert.StartsWith("## Choosing a data type", section);
        Assert.Contains("One value wins", section);
    }

    [TestMethod]
    public async Task A_section_includes_its_sub_sections_and_stops_at_the_next_sibling()
    {
        var section = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = "Getting started" });

        Assert.Contains("### Register the services", section);
        Assert.Contains("### Map the relay", section);
        Assert.DoesNotContain("## The five-minute version", section);
    }

    [TestMethod]
    public async Task A_heading_the_guide_uses_twice_is_told_apart_by_its_parent()
    {
        // "Persistence" is a top-level section and a part of the architecture.
        var index = await McpCall.TextAsync("GetBcrdtGuideSection");
        var architecture = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = "Architecture > Persistence" });
        var topLevel = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = "Persistence" });

        Assert.Contains("key `Architecture > Persistence`", index);
        Assert.StartsWith("### Persistence", architecture);
        Assert.StartsWith("## Persistence", topLevel);
    }

    [TestMethod]
    public async Task Every_section_in_the_index_is_retrievable_by_its_key()
    {
        foreach (var section in BcrdtSourceCatalog.GuideSections)
        {
            var text = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = section.Key });

            Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", text, section.Key);
        }
    }

    [TestMethod]
    public async Task An_unknown_heading_suggests_the_close_ones()
    {
        var answer = await McpCall.TextAsync("GetBcrdtGuideSection", new() { ["heading"] = "transport" });

        Assert.Contains("Did you mean", answer);
        Assert.Contains("Writing a transport", answer);
    }

    // ---- API ---------------------------------------------------------------

    [TestMethod]
    public async Task The_api_index_lists_the_public_types_of_all_four_packages_by_package()
    {
        var index = await McpCall.TextAsync("GetBcrdtApi");

        foreach (var package in new[] { "Bit.Bcrdt", "Bit.Bcrdt.SignalR", "Bit.Bcrdt.Server", "Bit.Bcrdt.Testing" })
        {
            Assert.Contains($"## {package}\n", index.ReplaceLineEndings("\n"));
        }

        foreach (var type in new[] { "BcrdtDocument", "BcrdtText", "BcrdtMap", "BcrdtArray", "BcrdtSet", "IBcrdtCollaborationService", "BcrdtCollaborativeDocument", "BcrdtSyncProvider", "BcrdtUndoManager", "BcrdtCollaborationScope", "BcrdtTextArea", "BcrdtPresenceView", "BcrdtComponentBase", "BcrdtSignalRTransportFactory", "BcrdtHub", "BcrdtSimulatedNetwork", "BcrdtConvergenceHarness" })
        {
            Assert.Contains($"**{type}**", index);
        }

        // Internal machinery stays out of the reference, and so does the class Razor makes of _Imports.razor.
        Assert.DoesNotContain("BcrdtUpdateCodec", index);
        Assert.DoesNotContain("BcrdtSearchMarker", index);
        Assert.DoesNotContain("_Imports", index);
    }

    [TestMethod]
    public async Task A_component_reference_lists_its_parameters_with_their_defaults()
    {
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtTextArea" });

        Assert.StartsWith("# BcrdtTextArea (Component)", reference);
        Assert.Contains("package `Bit.Bcrdt`", reference);
        Assert.Contains("## Parameter", reference);
        Assert.Contains("**Field** : `string` = `\"body\"`", reference);
        Assert.Contains("**Rows** : `int` = `10`", reference);
        Assert.Contains("**Text** : `BcrdtText` **(required)**", reference);
    }

    [TestMethod]
    public async Task The_browser_layers_callbacks_are_not_listed_as_api()
    {
        // Public only because JavaScript interop can call nothing else.
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtTextArea" });

        var methods = typeof(Bit.Bcrdt.Blazor.BcrdtTextArea).GetMethods()
            .Where(method => method.IsDefined(typeof(Microsoft.JSInterop.JSInvokableAttribute), false))
            .Select(method => method.Name)
            .ToArray();

        Assert.IsNotEmpty(methods);
        foreach (var method in methods) Assert.DoesNotContain($"**{method}**", reference, method);
    }

    [TestMethod]
    public async Task Option_defaults_are_read_off_a_real_instance()
    {
        var collaboration = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtCollaborationOptions" });
        var sync = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtSyncProviderOptions" });
        var persistence = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtDocumentPersistenceOptions" });

        Assert.Contains("**PresenceTimeout** : `TimeSpan` = `TimeSpan.FromSeconds(30)`", collaboration);
        Assert.Contains("**GarbageCollect** : `bool` = `true`", collaboration);
        Assert.Contains("**ClientIdProvider** : `IBcrdtClientIdProvider` = `BcrdtRandomClientIdProvider.Instance`", collaboration);
        Assert.Contains("**Sync** : `BcrdtSyncProviderOptions` = `new BcrdtSyncProviderOptions()`", collaboration);
        Assert.Contains("**HeartbeatInterval** : `TimeSpan` = `TimeSpan.FromSeconds(10)`", sync);
        Assert.Contains("**ResyncInterval** : `TimeSpan` = `TimeSpan.Zero`", sync);
        Assert.Contains("**CompactionThreshold** : `int` = `500`", persistence);
    }

    [TestMethod]
    public async Task A_default_made_up_per_instance_is_not_reported_as_one()
    {
        // Every BcrdtDocument draws a random client id; two instances disagree, so there is no default to report.
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtDocument" });

        var clientId = reference.Split('\n').First(line => line.StartsWith("- **ClientId**", StringComparison.Ordinal));

        Assert.DoesNotContain(" = `", clientId);
    }

    [TestMethod]
    public async Task Constructors_are_listed_with_their_parameters()
    {
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtSyncProvider" });

        Assert.Contains("## Constructor", reference);
        Assert.Contains("**new BcrdtSyncProvider**(BcrdtDocument doc, IBcrdtTransport transport, BcrdtAwarenessStore awareness = null, BcrdtSyncProviderOptions options = null)", reference);
    }

    [TestMethod]
    public async Task The_protected_members_a_derived_type_uses_are_listed_and_marked()
    {
        var component = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtComponentBase" });
        var hub = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtHub" });
        var type = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtAbstractType" });

        Assert.Contains("**Track**(BcrdtAbstractType type) : `IDisposable` *(protected)*", component);
        Assert.Contains("**RenderInterval** : `TimeSpan` *(protected)*", component);
        Assert.Contains("**AuthorizeAsync**(string documentId) : `ValueTask<bool>` *(protected)*", hub);
        Assert.Contains("**CreateInstance**() : `BcrdtAbstractType` *(protected)*", type);

        // A sealed type has nobody to protect anything for, a component's own lifecycle overrides are plumbing,
        // and what a component inherits protected from BcrdtComponentBase is BcrdtComponentBase's reference.
        var text = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtText" });
        var scope = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtCollaborationScope" });

        Assert.DoesNotContain("*(protected)*", text);
        Assert.DoesNotContain("*(protected)*", scope);
        Assert.DoesNotContain("OnParametersSetAsync", scope);
        Assert.DoesNotContain("**Track**", scope);
    }

    [TestMethod]
    public async Task A_member_without_documentation_of_its_own_carries_the_interfaces()
    {
        // BcrdtInMemoryTransport implements IBcrdtTransport and documents its members with <inheritdoc/>.
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtInMemoryTransport" });

        var send = reference.Split('\n').First(line => line.StartsWith("- **SendAsync**", StringComparison.Ordinal));

        Assert.Contains(" - ", send, "BcrdtInMemoryTransport.SendAsync says nothing itself, so its summary is IBcrdtTransport.SendAsync's.");
    }

    [TestMethod]
    public async Task What_the_compiler_writes_for_a_record_is_left_out()
    {
        var reference = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "BcrdtCollaboratorInfo" });

        Assert.DoesNotContain("- **GetHashCode**", reference);
        Assert.DoesNotContain("- **ToString**", reference);
        Assert.DoesNotContain("<Clone>", reference);
        Assert.Contains("**new BcrdtCollaboratorInfo**(string Id, string Name, string Color, string AvatarUrl = null)", reference);
    }

    [TestMethod]
    public async Task An_unprefixed_type_name_points_at_the_prefixed_one()
    {
        var answer = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "SyncProvider" });

        Assert.Contains("BcrdtSyncProvider", answer);
    }

    [TestMethod]
    public async Task A_type_name_with_nothing_like_it_explains_the_prefix()
    {
        var answer = await McpCall.TextAsync("GetBcrdtApi", new() { ["typeName"] = "YText" });

        Assert.Contains("prefixed 'Bcrdt'", answer);
    }

    // ---- Setup guide -------------------------------------------------------

    [TestMethod]
    public async Task Without_a_hosting_model_the_setup_guide_answers_with_its_index()
    {
        var index = await McpCall.TextAsync("GetBcrdtSetupGuide");

        foreach (var model in BcrdtSetupGuide.HostingModels) Assert.Contains($"- `{model}` - ", index, model);
    }

    [TestMethod]
    public async Task Every_ui_hosting_model_gets_the_wiring_that_fails_silently()
    {
        foreach (var model in BcrdtSetupGuide.HostingModels.Except(["console"]))
        {
            var guide = await McpCall.TextAsync("GetBcrdtSetupGuide", new() { ["hostingModel"] = model });

            Assert.Contains("`dotnet add package Bit.Bcrdt`", guide, model);
            Assert.Contains("AddBitBcrdtServices", guide, model);
            Assert.Contains("ABSOLUTE", guide, model);
            Assert.Contains("AddBitBcrdtHub", guide, model);
            Assert.Contains("UseWebSockets", guide, model);
            Assert.Contains("@using Bit.Bcrdt.Blazor", guide, model);
            Assert.Contains("## From the guide", guide, model);
        }
    }

    [TestMethod]
    public async Task The_web_app_guide_quotes_the_demo_files_verbatim()
    {
        var guide = await McpCall.TextAsync("GetBcrdtSetupGuide", new() { ["hostingModel"] = "web-app" });

        Assert.Contains("BOTH", guide);
        Assert.Contains(BcrdtSourceCatalog.GetSourceFile("Demo/Client/Extensions/IServiceCollectionExtensions.cs")!.TrimEnd(), guide);
        Assert.Contains(BcrdtSourceCatalog.GetSourceFile("Demo/Server/Program.cs")!.TrimEnd(), guide);
        Assert.Contains("### `Demo/Client/Collaboration/LiveRelay.cs`", guide);
        Assert.Contains("### `Demo/Client/Pages/Live.razor`", guide);
        Assert.Contains("### Register the services", guide, "The quoted guide section should be demoted under 'From the guide'.");
        Assert.DoesNotContain("[truncated", guide);
    }

    [TestMethod]
    public async Task The_server_guide_explains_circuits_and_prerendering()
    {
        var guide = await McpCall.TextAsync("GetBcrdtSetupGuide", new() { ["hostingModel"] = "server" });

        Assert.Contains("scoped", guide);
        Assert.Contains("singleton", guide);
        Assert.Contains("RendererInfo.IsInteractive", guide);
        Assert.Contains("CircuitHandler", guide);
        Assert.Contains("builder.Services.AddBitBcrdtCircuitPresence();", guide);
        Assert.Contains("BcrdtPresenceService.Suspend", guide);
        Assert.Contains("BcrdtInMemoryNetwork.Shared", guide);
    }

    [TestMethod]
    public async Task The_console_guide_owns_the_document_from_one_thread()
    {
        var guide = await McpCall.TextAsync("GetBcrdtSetupGuide", new() { ["hostingModel"] = "console" });

        Assert.Contains("dotnet add package Bit.Bcrdt", guide);
        Assert.Contains("not thread-safe", guide);
        Assert.Contains("BcrdtSyncExchange.Handle", guide);
        Assert.Contains("BcrdtSyncExchange.CreateAnnouncement", guide);
        Assert.Contains("BcrdtFormatException", guide);
        Assert.DoesNotContain("@using", guide);
    }

    [TestMethod]
    public async Task A_hosting_model_is_found_by_the_names_people_use_for_it()
    {
        // The tool's schema lists the keys, but the guide behind it also answers to the obvious other names.
        Assert.AreEqual(BcrdtSetupGuide.Get("server"), BcrdtSetupGuide.Get("Blazor Server"));
        Assert.AreEqual(BcrdtSetupGuide.Get("hybrid"), BcrdtSetupGuide.Get("maui"));
        Assert.AreEqual(BcrdtSetupGuide.Get("console"), BcrdtSetupGuide.Get("worker"));
        Assert.AreEqual(BcrdtSetupGuide.Get("web-app"), BcrdtSetupGuide.Get("InteractiveWebAssembly"));

        Assert.IsNull(BcrdtSetupGuide.Get("electron"));
        Assert.Contains("'electron' is not a known hosting model", await McpCall.TextAsync("GetBcrdtSetupGuide", new() { ["hostingModel"] = "electron" }));
    }

    // ---- Source files ------------------------------------------------------

    [TestMethod]
    public async Task The_source_index_lists_the_demo_pages_and_hosting_but_not_the_site_chrome()
    {
        var index = await McpCall.TextAsync("GetBcrdtSourceFile");

        Assert.Contains("Demo/Client/Pages/Live.razor", index);
        Assert.Contains("Demo/Client/Pages/ConflictLab.razor", index);
        Assert.Contains("Demo/Client/Collaboration/ConflictLabSession.cs", index);
        Assert.Contains("Demo/Client/Components/ReplicaPanel.razor", index);
        Assert.Contains("Demo/Client/Program.cs", index);
        Assert.Contains("Demo/Server/Program.cs", index);
        Assert.Contains("Demo/Server/Components/App.razor", index);
        Assert.DoesNotContain("McpServer.razor", index);
        Assert.DoesNotContain("NotFound.razor", index);
        Assert.DoesNotContain("Error.razor", index);
        Assert.DoesNotContain("Demo/Client/Layout/", index);
        Assert.DoesNotContain("DocsCatalog.cs", index);
    }

    [TestMethod]
    public async Task Every_page_is_described_by_what_it_opens_with()
    {
        var index = await McpCall.TextAsync("GetBcrdtSourceFile");

        var pages = index.Split('\n').Where(line => line.StartsWith("- `Demo/Client/Pages/", StringComparison.Ordinal)).ToArray();

        Assert.IsNotEmpty(pages);
        foreach (var page in pages) Assert.Contains(") - ", page, $"{page} has no description.");
    }

    [TestMethod]
    public async Task Every_file_in_the_index_comes_back_verbatim()
    {
        foreach (var file in BcrdtSourceCatalog.SourceFiles)
        {
            var content = await McpCall.TextAsync("GetBcrdtSourceFile", new() { ["path"] = file.Path });

            Assert.DoesNotContain("No source file at", content, file.Path);
            Assert.AreEqual(BcrdtSourceCatalog.GetSourceFile(file.Path), content, file.Path);
        }
    }

    [TestMethod]
    public async Task A_source_file_comes_back_verbatim()
    {
        var file = await McpCall.TextAsync("GetBcrdtSourceFile", new() { ["path"] = "Demo/Client/Pages/Live.razor" });

        Assert.Contains("<BcrdtCollaborationScope", file);
        Assert.Contains("RendererInfo.IsInteractive", file);
    }

    [TestMethod]
    public async Task A_partial_path_suggests_the_full_one()
    {
        var answer = await McpCall.TextAsync("GetBcrdtSourceFile", new() { ["path"] = "ConflictLab" });

        Assert.Contains("Demo/Client/Pages/ConflictLab.razor", answer);
    }
}
