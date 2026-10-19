using Bit.Bsqlite.Storage.Opfs;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Storage.IndexedDb;

namespace Bit.Bsqlite.Tests.Mcp;

/// <summary>
/// PlanBsqliteStorageSelection on the situations an application actually meets: a first open in a
/// browser that has everything, one that lacks OPFS, a database already bound to a backend, a better
/// backend appearing under each migration mode, a migration that cannot fit, a backend that is gone,
/// and configurations the library refuses outright. Every expectation is the library's documented
/// rule; the lab is only ever the messenger.
/// </summary>
[TestClass]
public class McpStorageLabTests
{
    [TestMethod]
    public async Task A_first_open_with_everything_available_lands_on_the_first_choice()
    {
        var plan = await PlanAsync();

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("opfs", plan.Provider);
        Assert.AreEqual("Synchronous", plan.EngineBuild);
        Assert.AreEqual(BsqliteOpfsStorageProvider.VfsModuleUrl, plan.VfsModule);
        Assert.IsTrue(plan.IsFirstChoice);
        Assert.IsFalse(plan.WasDecidedByExistingData);
        Assert.IsNull(plan.BindingBefore);
        Assert.AreEqual("opfs", plan.BindingAfter, "The binding is written once the open succeeds.");
        Assert.AreEqual("Selected", Candidate(plan, "opfs").Status);
        Assert.AreEqual("NotProbed", Candidate(plan, "indexeddb").Status);
        Assert.IsEmpty(plan.Log, "A first choice is nothing to log.");
    }

    [TestMethod]
    public async Task Without_opfs_it_falls_back_and_says_so()
    {
        var plan = await PlanAsync(new() { ["unavailable"] = "opfs" });

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.AreEqual("AsyncCapable", plan.EngineBuild);
        Assert.AreEqual(BsqliteIndexedDbStorageProvider.VfsModuleUrl, plan.VfsModule);
        Assert.IsFalse(plan.IsFirstChoice);
        Assert.AreEqual("Unavailable", Candidate(plan, "opfs").Status);
        Assert.Contains("unavailable", Candidate(plan, "opfs").Explanation!);
        Assert.IsTrue(plan.Log.Any(line => line.StartsWith("Warning:", StringComparison.Ordinal) && line.Contains("opfs", StringComparison.Ordinal)),
            "A fallback is logged as a warning naming what was rejected.");
    }

    [TestMethod]
    public async Task A_non_persistent_fallback_needs_the_opt_in()
    {
        var refused = await PlanAsync(new() { ["candidates"] = "opfs, in-memory", ["unavailable"] = "opfs" });

        Assert.IsFalse(refused.Opens);
        Assert.AreEqual("RejectedAsNonPersistent", Candidate(refused, "in-memory").Status);
        Assert.Contains("No storage provider could open database 'app.db'", refused.Error!);
        Assert.Contains("AllowNonPersistentFallback()", Candidate(refused, "in-memory").Explanation!);
        Assert.IsNull(refused.BindingAfter, "A failed open leaves no binding behind.");

        var accepted = await PlanAsync(new() { ["candidates"] = "opfs, in-memory", ["unavailable"] = "opfs", ["allowNonPersistentFallback"] = true });

        Assert.IsTrue(accepted.Opens);
        Assert.AreEqual("in-memory", accepted.Provider);
        Assert.IsTrue(accepted.Notes.Any(note => note.Contains("does not survive a reload", StringComparison.Ordinal)));
        Assert.Contains(".AllowNonPersistentFallback()", accepted.Configuration);
    }

    [TestMethod]
    public async Task A_non_persistent_first_choice_needs_no_opt_in()
    {
        var plan = await PlanAsync(new() { ["candidates"] = "in-memory" });

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("in-memory", plan.Provider);
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("never quietly substituted", StringComparison.Ordinal)), "A lone candidate is strict.");
    }

    [TestMethod]
    public async Task Existing_data_outranks_preference()
    {
        var plan = await PlanAsync(new() { ["existingBinding"] = "indexeddb" });

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.IsTrue(plan.WasDecidedByExistingData);
        Assert.AreEqual("opfs", plan.BetterProviderAvailable);
        Assert.AreEqual("indexeddb", plan.BindingAfter);
        Assert.AreEqual("NotAttempted", plan.Migration!.Outcome, "KeepExisting moves nothing.");
        Assert.Contains("BetterProviderAvailable", Candidate(plan, "opfs").Explanation!);
        Assert.IsTrue(plan.Log.Any(line => line.StartsWith("Information:", StringComparison.Ordinal) && line.Contains("KeepExisting", StringComparison.Ordinal)));
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("AutomaticOnOpen", StringComparison.Ordinal)), "The note names the mode that would move it.");
    }

    [TestMethod]
    public async Task Manual_leaves_the_move_to_the_application()
    {
        var plan = await PlanAsync(new() { ["existingBinding"] = "indexeddb", ["migrationMode"] = "Manual" });

        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.AreEqual("NotAttempted", plan.Migration!.Outcome);
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("BsqliteStorageMigrator.MigrateAsync", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task AutomaticOnOpen_moves_the_database_before_opening_it()
    {
        var plan = await PlanAsync(new() { ["existingBinding"] = "indexeddb", ["migrationMode"] = "AutomaticOnOpen" });

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("opfs", plan.Provider);
        Assert.AreEqual("indexeddb", plan.BindingBefore);
        Assert.AreEqual("opfs", plan.BindingAfter);
        Assert.AreEqual("Migrated", plan.Migration!.Outcome);
        Assert.AreEqual("indexeddb", plan.Migration.From);
        Assert.AreEqual("opfs", plan.Migration.To);
        Assert.IsTrue(plan.Migration.Copied, "The stand-in database was exported, verified and imported.");
        Assert.IsNull(plan.BetterProviderAvailable);
        Assert.Contains("AutomaticOnOpen moved it", Candidate(plan, "indexeddb").Explanation!);
        Assert.Contains(".WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen)", plan.Configuration);
    }

    [TestMethod]
    public async Task A_migration_without_room_fails_safely_and_opens_where_the_data_is()
    {
        var plan = await PlanAsync(new() { ["existingBinding"] = "indexeddb", ["migrationMode"] = "AutomaticOnOpen", ["freeBytes"] = 1000 });

        Assert.IsTrue(plan.Opens);
        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.AreEqual("indexeddb", plan.BindingAfter, "The migrator leaves the binding untouched.");
        Assert.AreEqual("Failed", plan.Migration!.Outcome);
        Assert.Contains("bytes free", plan.Migration.Error!);
        Assert.IsTrue(plan.Log.Any(line => line.StartsWith("Warning:", StringComparison.Ordinal) && line.Contains("BsqliteQuotaExceededException", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task Moving_off_in_memory_copies_nothing()
    {
        // The in-memory backend's admin store never sees what a connection wrote, so the library's migrator
        // finds nothing to export and only flips the binding - which the plan says rather than hides.
        var plan = await PlanAsync(new() { ["candidates"] = "opfs, in-memory", ["existingBinding"] = "in-memory", ["migrationMode"] = "AutomaticOnOpen" });

        Assert.AreEqual("opfs", plan.Provider);
        Assert.AreEqual("Migrated", plan.Migration!.Outcome);
        Assert.IsFalse(plan.Migration.Copied);
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("without copying anything", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_database_on_a_backend_this_browser_lacks_does_not_open_elsewhere()
    {
        var plan = await PlanAsync(new() { ["existingBinding"] = "opfs", ["unavailable"] = "opfs" });

        Assert.IsFalse(plan.Opens);
        Assert.Contains("will not open an empty", plan.Error!);
        Assert.AreEqual("Unavailable", Candidate(plan, "opfs").Status);
        Assert.AreEqual("opfs", plan.BindingAfter, "The binding still points at the data.");
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("Admin.DeleteAsync", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_database_on_a_backend_no_longer_registered_does_not_open_elsewhere()
    {
        var plan = await PlanAsync(new() { ["candidates"] = "opfs", ["existingBinding"] = "indexeddb" });

        Assert.IsFalse(plan.Opens);
        Assert.AreEqual("NotRegistered", Candidate(plan, "indexeddb").Status);
        Assert.IsFalse(Candidate(plan, "indexeddb").Registered);
        Assert.IsNull(Candidate(plan, "indexeddb").Rank, "It is the binding, not a candidate.");
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("Register it again", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_candidate_that_is_not_registered_is_named_with_its_registration()
    {
        var plan = await PlanAsync(new() { ["registered"] = "indexeddb" });

        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.AreEqual("NotRegistered", Candidate(plan, "opfs").Status);
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("AddStorageProvider<BsqliteOpfsStorageProvider>()", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_custom_backend_is_planned_with_the_facts_the_caller_gives()
    {
        var plan = await PlanAsync(new() { ["candidates"] = "acme-encrypted, indexeddb", ["nonPersistent"] = "acme-encrypted", ["unavailable"] = "acme-encrypted" });

        var custom = Candidate(plan, "acme-encrypted");

        Assert.IsFalse(custom.FirstParty);
        Assert.IsFalse(custom.Persistent);
        Assert.AreEqual("AsyncCapable", custom.EngineBuild);
        Assert.AreEqual("Unavailable", custom.Status);
        Assert.AreEqual("indexeddb", plan.Provider);
        Assert.Contains("YourStorageProvider", plan.Configuration);
        Assert.Contains(".Prefer(\"acme-encrypted\")", plan.Configuration);
        Assert.Contains(".Fallback(BsqliteIndexedDbStorageProvider.ProviderName)", plan.Configuration);
    }

    [TestMethod]
    public async Task A_first_party_backend_keeps_its_declared_persistence()
    {
        var plan = await PlanAsync(new() { ["nonPersistent"] = "opfs" });

        Assert.IsTrue(Candidate(plan, "opfs").Persistent);
        Assert.IsTrue(plan.Notes.Any(note => note.Contains("first-party backend", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_configuration_the_library_refuses_is_refused_with_its_own_message()
    {
        var twice = await PlanAsync(new() { ["candidates"] = "opfs, OPFS" });
        var duplicate = await PlanAsync(new() { ["registered"] = "opfs, OPFS" });
        var empty = await PlanAsync(new() { ["candidates"] = "" });

        Assert.IsFalse(twice.Opens);
        Assert.Contains("is already a candidate", twice.Error!);
        Assert.Contains("Two storage providers are registered under the name", duplicate.Error!);
        Assert.Contains("No storage provider was selected", empty.Error!);
        Assert.IsEmpty(twice.Log);
    }

    [TestMethod]
    public async Task A_name_with_a_line_break_is_refused_before_it_reaches_the_log()
    {
        var database = await PlanAsync(new() { ["databaseName"] = "app.db\nWarning: forged" });
        var binding = await PlanAsync(new() { ["existingBinding"] = "opfs\r\nWarning: forged" });

        Assert.IsFalse(database.Opens);
        Assert.Contains("databaseName contains a control character", database.Error!);
        Assert.IsEmpty(database.Log);
        Assert.Contains("existingBinding contains a control character", binding.Error!);
        Assert.IsEmpty(binding.Log);
    }

    [TestMethod]
    public async Task The_configuration_is_the_csharp_that_produces_the_plan()
    {
        var plan = await PlanAsync(new() { ["candidates"] = "opfs, indexeddb, in-memory", ["allowNonPersistentFallback"] = true, ["databaseName"] = "orders.db" });

        Assert.AreEqual(
            """
            builder.Services.AddBitBsqliteServices(sqlite => sqlite
                .AddStorageProvider<BsqliteOpfsStorageProvider>()
                .AddStorageProvider<BsqliteIndexedDbStorageProvider>()
                .AddStorageProvider<BsqliteInMemoryStorageProvider>()
                .UseStorage(s => s
                    .Prefer(BsqliteOpfsStorageProvider.ProviderName)
                    .Fallback(BsqliteIndexedDbStorageProvider.ProviderName)
                    .Fallback(BsqliteInMemoryStorageProvider.ProviderName)
                    .AllowNonPersistentFallback())
                .AddDatabase("orders.db"));
            """.ReplaceLineEndings("\n"), plan.Configuration);
    }

    internal static Task<BsqliteStoragePlanDto> PlanAsync(Dictionary<string, object?>? arguments = null)
        => McpCall.StructuredAsync<BsqliteStoragePlanDto>("PlanBsqliteStorageSelection", arguments);

    private static BsqliteCandidateReportDto Candidate(BsqliteStoragePlanDto plan, string name)
        => plan.Candidates.SingleOrDefault(candidate => candidate.Name == name)
           ?? throw new AssertFailedException($"The plan has no candidate '{name}': {string.Join(", ", plan.Candidates.Select(candidate => candidate.Name))}");
}
