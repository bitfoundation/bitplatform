using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Net.Http.Json;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Testing;
using Bit.Bsqlite.Storage.Opfs;
using System.Text.RegularExpressions;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Storage.InMemory;
using Bit.Bsqlite.Storage.IndexedDb;
using Bit.Bsqlite.Demo.Server.Services;
using Bit.Bsqlite.Storage.CacheStorage;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the storage catalog against the provider types
/// it was read from, the storage lab against the library's own resolver, the API index against the
/// assemblies, the document index against the documents, and the HTTP mirror against the protocol it
/// mirrors. No expected answers are written down here - the library, its files and the protocol are the
/// expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    private static readonly IBsqliteStorageProvider[] _firstParty =
    [
        new BsqliteOpfsStorageProvider(),
        new BsqliteIndexedDbStorageProvider(),
        new BsqliteCacheStorageProvider(),
        new BsqliteInMemoryStorageProvider(),
    ];

    [TestMethod]
    public void The_storage_catalog_is_what_the_provider_types_declare()
    {
        CollectionAssert.AreEqual(_firstParty.Select(provider => provider.Name).ToArray(), BsqliteStorageCatalog.Providers.Select(provider => provider.Name).ToArray());

        foreach (var provider in _firstParty)
        {
            var entry = BsqliteStorageCatalog.Find(provider.Name)!;

            Assert.AreEqual(provider.GetType().Name, entry.TypeName);
            Assert.AreEqual(provider.GetType().Assembly.GetName().Name, entry.Package);
            Assert.AreEqual(provider.VfsModule?.ModuleUrl, entry.VfsModule, provider.Name);

            foreach (var property in typeof(BsqliteStorageCapabilities).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.Name != "EqualityContract"))
            {
                var capability = entry.Capabilities.SingleOrDefault(capability => capability.Name == property.Name);

                Assert.IsNotNull(capability, $"{provider.Name} does not report {property.Name}.");
                Assert.AreEqual(BsqliteStorageCatalog.FormatCapability(property.GetValue(provider.Capabilities)), capability.Value, $"{provider.Name}.{property.Name}");
            }
        }
    }

    [TestMethod]
    public void Every_capability_is_documented()
    {
        // The capability rows explain themselves out of the contract's XML documentation; a capability
        // without it is a row of values nobody can interpret.
        foreach (var capability in BsqliteStorageCatalog.Providers[0].Capabilities)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(capability.Summary), capability.Name);
        }
    }

    /// <summary>
    /// The lab's verdicts against the library's resolver, run here directly over providers of the test's own:
    /// the real capabilities of the first-party types and a probe that says what the configuration says. A
    /// spread of configurations - preference orders, missing backends, bindings, the fallback opt-in - is
    /// planned both ways, and every candidate's status has to agree.
    /// </summary>
    [TestMethod]
    public async Task The_storage_lab_agrees_with_the_librarys_resolver()
    {
        var random = new Random(20261018);
        string[] names = ["opfs", "indexeddb", "cache-storage", "in-memory"];

        for (var i = 0; i < 40; i++)
        {
            var candidates = names.OrderBy(_ => random.Next()).Take(random.Next(1, names.Length + 1)).ToArray();
            var unavailable = names.Where(_ => random.Next(3) == 0).ToArray();
            var allow = random.Next(2) == 0;
            var binding = random.Next(3) == 0 ? candidates[random.Next(candidates.Length)] : null;
            var context = $"plan {i}: [{string.Join(", ", candidates)}], unavailable [{string.Join(", ", unavailable)}], allow {allow}, binding {binding ?? "none"}";

            var plan = await McpStorageLabTests.PlanAsync(new()
            {
                ["candidates"] = string.Join(", ", candidates),
                ["unavailable"] = string.Join(", ", unavailable),
                ["allowNonPersistentFallback"] = allow,
                ["existingBinding"] = binding,
            });

            var bindings = new BsqliteInMemoryStorageBindingStore();
            if (binding is not null) await bindings.SetProviderNameAsync("app.db", binding);

            var resolver = new BsqliteStorageProviderResolver(
                candidates.Select(name => new DescribedProvider(_firstParty.Single(provider => provider.Name == name), unavailable.Contains(name))),
                bindings);

            var selection = BsqliteStorageSelectionBuilder.Create(builder =>
            {
                builder.Prefer(candidates[0]);
                foreach (var name in candidates.Skip(1)) builder.Fallback(name);
                if (allow) builder.AllowNonPersistentFallback();
            });

            IReadOnlyList<BsqliteStorageCandidateOutcome> attempts;
            BsqliteStorageResolution? resolution = null;

            try
            {
                resolution = await resolver.ResolveAsync("app.db", selection);
                attempts = resolution.Attempts;
            }
            catch (BsqliteStorageUnavailableException exception)
            {
                attempts = exception.Attempts;
                Assert.AreEqual(exception.Message, plan.Error, context);
            }

            Assert.AreEqual(resolution is not null, plan.Opens, context);
            Assert.AreEqual(resolution?.Provider.Name, plan.Provider, context);
            Assert.AreEqual(resolution?.IsFirstChoice, plan.IsFirstChoice, context);
            Assert.AreEqual(resolution?.WasDecidedByExistingData, plan.WasDecidedByExistingData, context);
            Assert.AreEqual(resolution?.BetterProviderAvailable?.Name, plan.BetterProviderAvailable, context);
            Assert.AreEqual(resolution?.Provider.Capabilities.RequiredBuild.ToString(), plan.EngineBuild, context);

            foreach (var attempt in attempts)
            {
                var candidate = plan.Candidates.Single(candidate => candidate.Name == attempt.ProviderName);

                Assert.AreEqual(attempt.Status.ToString(), candidate.Status, $"{context}: {attempt.ProviderName}");
            }
        }
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = BsqliteApiCatalog.Assemblies
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsNested is false)
            .Select(BsqliteApiCatalog.FriendlyName)
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BsqliteApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
    }

    [TestMethod]
    public void The_api_covers_every_package_and_the_entry_points()
    {
        CollectionAssert.AreEquivalent(
            new[] { typeof(BsqliteConnection), typeof(BsqliteOpfsStorageProvider), typeof(BsqliteIndexedDbStorageProvider), typeof(BsqliteCacheStorageProvider), typeof(BsqliteInProcessTransport) }
                .Select(type => type.Assembly).ToArray(),
            BsqliteApiCatalog.Assemblies);

        var entryPoints = BsqliteApiCatalog.GetTypeDetails("BitBsqlite")!;

        Assert.AreEqual("Microsoft.Extensions.DependencyInjection", entryPoints.Namespace);
        Assert.IsTrue(entryPoints.Members.Any(member => member.Name == "AddBitBsqliteServices"));
        Assert.IsTrue(entryPoints.Members.Any(member => member.Name == "AddBitBsqliteDbContext<TContext>"));
        Assert.AreEqual("Microsoft.EntityFrameworkCore", BsqliteApiCatalog.GetTypeDetails("BsqliteDbContextOptionsExtensions")!.Namespace);
    }

    [TestMethod]
    public void Every_documented_public_type_has_its_summary_in_the_index()
    {
        // The XML documentation files have to reach the server's output folder for any of this to work; a
        // build that stops copying them leaves every summary empty without failing anything else.
        var withSummary = BsqliteApiCatalog.Types.Count(type => type.Summary is not null);

        Assert.IsGreaterThan(BsqliteApiCatalog.Types.Length * 9 / 10, withSummary, "Most public types should come with their XML summary.");

        foreach (var name in BsqliteXmlDocs.AssemblyNames)
        {
            Assert.IsTrue(File.Exists(Path.Combine(AppContext.BaseDirectory, $"{name}.xml")), $"{name}.xml is not next to the server.");
        }
    }

    [TestMethod]
    public void The_sections_cover_every_heading_of_every_document()
    {
        Assert.IsGreaterThanOrEqualTo(12, BsqliteSourceCatalog.Documents.Length, "The README, six guides and five package READMEs are embedded.");

        foreach (var document in BsqliteSourceCatalog.Documents)
        {
            var text = BsqliteSourceCatalog.GetDocument(document.Name)!.Replace("\r\n", "\n", StringComparison.Ordinal);

            var fenced = false;
            var headings = new List<string>();

            foreach (var line in text.Split('\n'))
            {
                if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
                if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
            }

            var sections = BsqliteSourceCatalog.GetSections(document.Name);

            CollectionAssert.AreEqual(headings, sections.Select(section => section.Heading).ToArray(), document.Name);
            Assert.HasCount(sections.Length, sections.Select(section => section.Key).Distinct(StringComparer.Ordinal), $"Every section of '{document.Name}' needs a key of its own.");

            foreach (var section in sections)
            {
                Assert.StartsWith($"{new string('#', section.Level)} {section.Heading}", BsqliteSourceCatalog.GetDocumentSection(document.Name, section.Key)!, $"{document.Name} > {section.Key}");
            }
        }
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBsqlite", "?query=live%20query&limit=3", new() { ["query"] = "live query", ["limit"] = 3 }),
            ("GetBsqliteSetupGuide", "?scenario=storage", new() { ["scenario"] = "storage" }),
            ("GetBsqliteDocument", "?name=storage&section=Selection", new() { ["name"] = "storage", ["section"] = "Selection" }),
            ("GetBsqliteApi", "?typeName=BsqliteDatabase", new() { ["typeName"] = "BsqliteDatabase" }),
            ("GetBsqliteStorageProviders", "?provider=opfs", new() { ["provider"] = "opfs" }),
            ("PlanBsqliteStorageSelection", "?unavailable=opfs&migrationMode=Manual", new() { ["unavailable"] = "opfs", ["migrationMode"] = "Manual" }),
            ("CheckBsqliteSql", "?sql=PRAGMA%20page_size%20%3D%208192", new() { ["sql"] = "PRAGMA page_size = 8192" }),
            ("GetBsqliteSourceFile", "?path=Demo/Client/Pages/Storage.razor", new() { ["path"] = "Demo/Client/Pages/Storage.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBsqlite" or "PlanBsqliteStorageSelection" or "CheckBsqliteSql")
            {
                // The structured answers are compared as JSON: the same members, the same values, the nulls left
                // out on both sides.
                var protocol = (await McpCall.RawAsync(tool, arguments)).StructuredContent!.Value;

                Assert.IsTrue(JsonElement.DeepEquals(JsonDocument.Parse(body).RootElement, protocol), tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [TestMethod]
    public async Task The_demo_pages_catalog_lists_exactly_what_the_protocol_publishes()
    {
        using var http = McpTestHost.CreateHttpClient();

        var catalog = await http.GetFromJsonAsync<BsqliteMcpCatalogDto>("/api/mcp/GetMcpCatalog");

        Assert.IsNotNull(catalog);
        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, catalog.Tools.Select(tool => tool.Name).ToArray());
        CollectionAssert.AreEquivalent((await McpTestHost.Client.ListPromptsAsync()).Select(prompt => prompt.Name).ToArray(), catalog.Prompts.Select(prompt => prompt.Name).ToArray());
        Assert.IsTrue(catalog.Tools.All(tool => tool.Title is not null && tool.Description.Length > 0));
        Assert.IsFalse(catalog.Tools.SelectMany(tool => tool.Parameters).Any(parameter => parameter.StartsWith("cancellationToken", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task The_mcp_page_and_the_existing_demo_endpoints_still_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        using var page = await http.GetAsync("/mcp-server");
        using var products = await http.GetAsync("/api/products");

        Assert.AreEqual(HttpStatusCode.OK, page.StatusCode);
        Assert.Contains("MCP server", await page.Content.ReadAsStringAsync());
        Assert.AreEqual(HttpStatusCode.OK, products.StatusCode, "The server's own catalog endpoint is untouched.");
    }

    /// <summary>A first-party backend as the test describes it: its real capabilities, and the probe the plan says it gives.</summary>
    private sealed class DescribedProvider(IBsqliteStorageProvider real, bool unavailable) : IBsqliteStorageProvider
    {
        public string Name => real.Name;

        public BsqliteStorageCapabilities Capabilities => real.Capabilities;

        public BsqliteJsModule? VfsModule => real.VfsModule;

        public IBsqliteStorageAdmin Admin => throw new NotSupportedException("Resolution never touches the admin surface.");

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
            => ValueTask.FromResult(unavailable ? BsqliteProbeResult.Unavailable($"'{Name}' was described as unavailable in this browser.") : BsqliteProbeResult.Available());
    }

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();
}
