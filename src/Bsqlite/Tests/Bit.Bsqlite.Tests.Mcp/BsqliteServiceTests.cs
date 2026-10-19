using System.Text.RegularExpressions;
using Bit.Bsqlite.Demo.Server.Services;
using Bit.Bsqlite.Demo.Server.Controllers;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// The services the tools are built from, called directly: the rules that are awkward to reach through
/// a schema-validated protocol call - an argument a client would never send, a document longer than any
/// that exists - and the small parsing decisions every answer depends on.
/// </summary>
[TestClass]
public partial class BsqliteServiceTests
{
    public TestContext TestContext { get; set; } = null!;

    // ---- Completions -------------------------------------------------------

    [TestMethod]
    public void An_argument_with_no_set_completes_with_nothing_rather_than_failing()
    {
        Assert.IsEmpty(BsqliteCompletions.Complete("dataSource", "Off").Values);
        Assert.IsEmpty(BsqliteCompletions.Complete(null, null).Values);
    }

    [TestMethod]
    public void A_typed_prefix_ranks_above_a_typed_substring()
    {
        var values = BsqliteCompletions.Complete("typeName", "Storage").Values;

        Assert.IsNotEmpty(values);
        Assert.IsTrue(values.All(value => value.Contains("Storage", StringComparison.OrdinalIgnoreCase)));

        // "IBsqliteStorageProvider" contains the prefix, "BsqliteStorageMigrator" starts with it: the second kind first.
        var prefixed = BsqliteCompletions.Complete("typeName", "BsqliteStorage").Values;
        var firstSubstring = prefixed.ToList().FindIndex(value => value.StartsWith("BsqliteStorage", StringComparison.Ordinal) is false);

        Assert.IsGreaterThan(0, firstSubstring);
        Assert.IsTrue(prefixed.Skip(firstSubstring).All(value => value.StartsWith("BsqliteStorage", StringComparison.Ordinal) is false));
    }

    [TestMethod]
    public void A_set_larger_than_the_protocol_allows_says_there_is_more()
    {
        var completion = BsqliteCompletions.Complete("typeName", string.Empty);

        Assert.AreEqual(BsqliteApiCatalog.Types.Length, completion.Total);
        Assert.IsLessThanOrEqualTo(100, completion.Values.Count);
        Assert.AreEqual(BsqliteApiCatalog.Types.Length > 100, completion.HasMore);
    }

    // ---- Names a caller writes ---------------------------------------------

    [TestMethod]
    public void A_document_name_is_resolved_the_way_people_write_it()
    {
        Assert.AreEqual("storage", BsqliteSourceCatalog.ResolveDocumentName("Storage.md"));
        Assert.AreEqual("live-queries", BsqliteSourceCatalog.ResolveDocumentName("docs\\live-queries.md"));
        Assert.AreEqual("live-queries", BsqliteSourceCatalog.ResolveDocumentName("Live Queries"));
        Assert.AreEqual(BsqliteSourceCatalog.ReadmeName, BsqliteSourceCatalog.ResolveDocumentName("README.md"));
        Assert.AreEqual("Bit.Bsqlite.Storage.IndexedDb", BsqliteSourceCatalog.ResolveDocumentName("indexeddb"));
        Assert.AreEqual("Bit.Bsqlite.Storage.CacheStorage", BsqliteSourceCatalog.ResolveDocumentName("Storage.CacheStorage"));
        Assert.AreEqual("Bit.Bsqlite.Testing", BsqliteSourceCatalog.ResolveDocumentName("Testing"));
        Assert.IsNull(BsqliteSourceCatalog.ResolveDocumentName("changelog"));
        Assert.IsNull(BsqliteSourceCatalog.ResolveDocumentName("   "));
    }

    [TestMethod]
    public void A_scenario_is_resolved_from_the_spellings_people_reach_for()
    {
        Assert.AreEqual("auto", BsqliteSetupGuide.Resolve("InteractiveAuto"));
        Assert.AreEqual("server", BsqliteSetupGuide.Resolve("Blazor Server"));
        Assert.AreEqual("wasm", BsqliteSetupGuide.Resolve("InteractiveWebAssembly"));
        Assert.AreEqual("standalone-wasm", BsqliteSetupGuide.Resolve("standalone"));
        Assert.AreEqual("move-from-besql", BsqliteSetupGuide.Resolve("Bit.Besql"));
        Assert.AreEqual("testing", BsqliteSetupGuide.Resolve("MSTest"));
        Assert.AreEqual("custom-provider", BsqliteSetupGuide.Resolve("custom_storage_provider"));
        Assert.IsNull(BsqliteSetupGuide.Resolve("electron"));

        foreach (var scenario in BsqliteSetupGuide.Scenarios) Assert.AreEqual(scenario, BsqliteSetupGuide.Resolve(scenario.ToUpperInvariant()));
    }

    [TestMethod]
    public void A_backend_is_resolved_however_it_is_named()
    {
        Assert.AreEqual("cache-storage", BsqliteStorageCatalog.Find("CacheStorage")?.Name);
        Assert.AreEqual("in-memory", BsqliteStorageCatalog.Find("InMemory")?.Name);
        Assert.AreEqual("opfs", BsqliteStorageCatalog.Find("Bit.Bsqlite.Storage.Opfs")?.Name);
        Assert.IsNull(BsqliteStorageCatalog.Find("localstorage"));
        Assert.IsNull(BsqliteStorageCatalog.Instance("acme-encrypted"));
    }

    // ---- Bounds ------------------------------------------------------------

    [TestMethod]
    public void A_long_answer_is_cut_and_says_so()
    {
        var text = new string('a', 50_000);
        var cut = McpController.Truncate(text);

        Assert.StartsWith(new string('a', 40_000), cut);
        Assert.Contains("[truncated", cut);
        Assert.AreEqual("short", McpController.Truncate("short"));
    }

    [TestMethod]
    public void A_cut_never_splits_a_surrogate_pair()
    {
        // An emoji straddling the cut: half a pair is not text.
        var text = new string('a', 39_999) + "\U0001F600" + new string('b', 100);
        var cut = McpController.Truncate(text);

        Assert.IsFalse(char.IsHighSurrogate(cut[cut.IndexOf("\n\n[truncated", StringComparison.Ordinal) - 1]));
    }

    [TestMethod]
    public async Task The_planner_refuses_input_no_configuration_has()
    {
        var mode = await BsqliteStorageLab.PlanAsync(null, null, null, null, false, "Sometimes", null, null, null, TestContext.CancellationToken);
        var numeric = await BsqliteStorageLab.PlanAsync(null, null, null, null, false, "2", null, null, null, TestContext.CancellationToken);
        var many = await BsqliteStorageLab.PlanAsync(string.Join(",", Enumerable.Range(0, 20).Select(i => $"p{i}")), null, null, null, false, null, null, null, null, TestContext.CancellationToken);
        var longName = await BsqliteStorageLab.PlanAsync(new string('x', 100), null, null, null, false, null, null, null, null, TestContext.CancellationToken);

        Assert.Contains("is not a migration mode", mode.Error!);
        Assert.Contains("is not a migration mode", numeric.Error!, "A mode is named, as WithMigrationMode is written - not numbered.");
        Assert.Contains("at most", many.Error!);
        Assert.Contains("longer than a provider name", longName.Error!);
        Assert.IsTrue(new[] { mode, numeric, many, longName }.All(plan => plan.Opens is false && plan.Log.Length == 0));
    }

    [TestMethod]
    public async Task The_planner_honours_the_database_name()
    {
        var plan = await BsqliteStorageLab.PlanAsync("indexeddb", null, "indexeddb", null, false, null, null, null, "orders.db", TestContext.CancellationToken);

        Assert.Contains("No storage provider could open database 'orders.db'", plan.Error!);
        Assert.Contains("AddDatabase(\"orders.db\")", plan.Configuration);
    }

    [TestMethod]
    public void Blank_sql_is_the_guard_reference()
    {
        Assert.HasCount(BsqliteStorageCatalog.GuardedStatements.Length, BsqliteSqlCheck.Check("   ").Statements);
    }

    // ---- Documentation text ------------------------------------------------

    [TestMethod]
    public void An_inheritdoc_cref_overload_carries_the_documentation_it_points_at()
    {
        // The generic UseBitBsqlite<TContext> overloads say <inheritdoc cref="..."/> and nothing else.
        var extensions = BsqliteApiCatalog.GetTypeDetails("BsqliteDbContextOptionsExtensions")!;
        var overloads = extensions.Members.Where(member => member.Name.StartsWith("UseBitBsqlite", StringComparison.Ordinal)).ToArray();

        Assert.IsGreaterThan(3, overloads.Length);
        foreach (var overload in overloads) Assert.IsNotNull(overload.Summary, $"{overload.Name}{overload.Signature}");
    }

    [TestMethod]
    public void No_rendered_documentation_carries_a_generic_arity_marker()
    {
        // A cref to a member of a generic type ("IBsqliteLiveQuery`1.RefreshAsync") must read as the member,
        // not be cut off at the marker.
        foreach (var type in BsqliteApiCatalog.Types)
        {
            var details = BsqliteApiCatalog.GetTypeDetails(type.Name)!;

            foreach (var text in details.Members.SelectMany(member => new[] { member.Summary, member.Remarks }).Append(details.Summary).Append(details.Remarks))
            {
                if (text is null) continue;

                Assert.IsFalse(ArityRegex().IsMatch(text), $"{type.Name}: {text}");
            }
        }
    }

    [GeneratedRegex(@"\w`\d")]
    private static partial Regex ArityRegex();

    [TestMethod]
    public void A_document_without_sections_is_its_own_preamble()
    {
        Assert.AreEqual(BsqliteSourceCatalog.GetDocument("limits")!.Trim().Replace("\r\n", "\n", StringComparison.Ordinal),
                        BsqliteSourceCatalog.GetPreamble("limits")!.Replace("\r\n", "\n", StringComparison.Ordinal));

        Assert.DoesNotContain("## ", BsqliteSourceCatalog.GetPreamble("storage")!);
        Assert.IsNull(BsqliteSourceCatalog.GetPreamble("changelog"));
    }

    [TestMethod]
    public void A_source_file_is_described_by_what_it_says_about_itself()
    {
        string? Describe(string path) => BsqliteSourceCatalog.SourceFiles.Single(file => file.Path == path).Description;

        // The class summary, not the first enum value's above it.
        Assert.StartsWith("The sample's view of the library's shared BsqliteDatabase", Describe("Demo/Client/BrowserDatabase.cs")!);
        // The page header a reader sees.
        Assert.StartsWith("Choosing where the file lives:", Describe("Demo/Client/Pages/Storage.razor")!);
        // A component's own comment, not its parameters' documentation.
        Assert.StartsWith("Shows the code that powers the demo", Describe("Demo/Client/Components/CodeBlock.razor")!);
        // A file that explains itself only in its code.
        Assert.StartsWith("Every page is prerendered on the server first", Describe("Demo/Client/Components/SessionLifecycle.razor")!);
        // A <see cref="..."/> reads as the name it refers to.
        Assert.StartsWith("Named SalesOrder rather than Order", Describe("Demo/Domain/SalesOrder.cs")!);
    }
}
