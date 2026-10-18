using System.Text;
using Bit.Bsqlite.Data;
using Bit.Bsqlite.Interop;
using Bit.Bsqlite.Storage;
using Bit.Bsqlite.Demo.Server.Dtos;
using Bit.Bsqlite.Storage.InMemory;
using Microsoft.Extensions.Logging;

namespace Bit.Bsqlite.Demo.Server.Services;

/// <summary>
/// Runs Bit.Bsqlite's own storage selection on a configuration and a browser the caller describes, so
/// "which backend will this database open on, and why?" is answered by the code that decides it at
/// runtime rather than by a reading of the rules.
/// <para>
/// Nothing here re-implements a rule. The selection is built by <see cref="BsqliteStorageSelectionBuilder"/>,
/// so a configuration mistake fails with the builder's own message; the open goes through a real
/// <see cref="BsqliteSessionFactory"/> over a real <see cref="BsqliteStorageProviderResolver"/>,
/// <see cref="BsqliteStorageMigrator"/> and binding store, exactly as <c>AddBitBsqliteServices</c>
/// composes them. What stands in for the browser is the providers - each answers its probe the way the
/// caller described, keeps the capabilities the real provider type declares when it is a first-party
/// backend, and stores its databases in an in-memory admin store - and the transport, which opens
/// nothing: the library's tests drive the same composition the same way.
/// </para>
/// <para>
/// No SQL runs, here or anywhere on this server. The stand-in database a bound backend holds is a
/// SQLite header followed by zeros - enough for the migrator's header check and byte comparison to do
/// their real work, and nothing an engine could open.
/// </para>
/// </summary>
public static class BsqliteStorageLab
{
    /// <summary>The preference order a call without one plans for: the README's own, OPFS then IndexedDB.</summary>
    public const string DefaultCandidates = "opfs, indexeddb";

    public const string DefaultDatabaseName = "app.db";

    /// <summary>The size of the stand-in database an existing binding holds, which a migration has to find room for.</summary>
    public const int StandInDatabaseBytes = 64 * 1024;

    /// <summary>More names than any real configuration has: a list longer than this is a mistake, not a plan.</summary>
    private const int MaxNames = 16;

    private const int MaxNameLength = 64;

    /// <summary>The migration modes, as the enum declares them - what the tool's schema offers.</summary>
    public static readonly string[] MigrationModes = Enum.GetNames<BsqliteStorageMigrationMode>();

    private static ReadOnlySpan<byte> SqliteMagic => "SQLite format 3\0"u8;

    public static async Task<BsqliteStoragePlanDto> PlanAsync(
        string? candidates,
        string? registered,
        string? unavailable,
        string? nonPersistent,
        bool allowNonPersistentFallback,
        string? migrationMode,
        string? existingBinding,
        long? freeBytes,
        string? databaseName,
        CancellationToken cancellationToken)
    {
        var candidateNames = Names(candidates ?? DefaultCandidates);
        string[] registeredNames = registered is null ? [.. candidateNames.Distinct(StringComparer.OrdinalIgnoreCase)] : Names(registered);
        var unavailableNames = Names(unavailable).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var nonPersistentNames = Names(nonPersistent).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var binding = string.IsNullOrWhiteSpace(existingBinding) ? null : existingBinding.Trim();
        var database = string.IsNullOrWhiteSpace(databaseName) ? DefaultDatabaseName : databaseName.Trim();

        var notes = new List<string>();

        var mode = BsqliteStorageMigrationMode.KeepExisting;
        var configurationError = Validate(candidateNames, registeredNames, unavailableNames, nonPersistentNames, binding, database);

        // Parsed by name only: Enum.TryParse also takes "2" or "Manual, KeepExisting", neither of which is
        // something WithMigrationMode could be handed in code.
        if (configurationError is null && string.IsNullOrWhiteSpace(migrationMode) is false)
        {
            var modeName = MigrationModes.FirstOrDefault(name => string.Equals(name, migrationMode.Trim(), StringComparison.OrdinalIgnoreCase));

            if (modeName is null) configurationError = $"'{migrationMode}' is not a migration mode. Use one of: {string.Join(", ", MigrationModes)}.";
            else mode = Enum.Parse<BsqliteStorageMigrationMode>(modeName);
        }

        // The builder is what UseStorage(...) runs at startup, so a preference order it refuses is refused
        // with the exception the application would see, rather than with a paraphrase of it.
        BsqliteStorageSelection? selection = null;

        if (configurationError is null)
        {
            try
            {
                selection = BsqliteStorageSelectionBuilder.Create(builder =>
                {
                    for (var index = 0; index < candidateNames.Length; index++)
                    {
                        if (index == 0) builder.Prefer(candidateNames[index]);
                        else builder.Fallback(candidateNames[index]);
                    }

                    if (allowNonPersistentFallback) builder.AllowNonPersistentFallback();

                    builder.WithMigrationMode(mode);
                });
            }
            catch (Exception exception) when (exception is InvalidOperationException or ArgumentException)
            {
                configurationError = exception.Message;
            }
        }

        var providers = registeredNames.Select(name => new LabProvider(
            name,
            unavailableNames.Contains(name),
            nonPersistentNames.Contains(name),
            freeBytes)).ToArray();

        foreach (var name in nonPersistentNames.Where(name => BsqliteStorageCatalog.Instance(name) is not null))
        {
            notes.Add($"'{name}' is a first-party backend, so its persistence is what its provider type declares, not what nonPersistent says.");
        }

        var configuration = Configuration(candidateNames, registeredNames, allowNonPersistentFallback, mode, database);
        var log = new LabLog();

        // Two resolvers over the same providers and bindings: one the session factory opens through, and
        // one for the decision before any migration - ResolveAsync changes nothing, and keeping it apart
        // keeps its log lines out of the report. Constructing them is also where two providers registered
        // under one name are refused, as AddBitBsqliteServices refuses them on the first open.
        BsqliteStorageProviderResolver? diagnostic = null;
        BsqliteStorageProviderResolver? resolver = null;
        var bindings = new BsqliteInMemoryStorageBindingStore();

        if (configurationError is null)
        {
            try
            {
                diagnostic = new BsqliteStorageProviderResolver(providers, bindings);
                resolver = new BsqliteStorageProviderResolver(providers, bindings, log.For<BsqliteStorageProviderResolver>());
            }
            catch (InvalidOperationException exception)
            {
                configurationError = exception.Message;
            }
        }

        if (configurationError is not null)
        {
            return new BsqliteStoragePlanDto
            {
                Opens = false,
                BindingBefore = binding,
                BindingAfter = binding,
                Candidates = Report(candidateNames, providers, binding, attempts: null, resolution: null, migration: null),
                Error = configurationError,
                Configuration = configuration,
                Log = [],
                Notes = [.. notes, "Nothing was planned: the configuration itself is refused - at startup, by AddBitBsqliteServices, before any database is opened."]
            };
        }

        if (binding is not null)
        {
            await bindings.SetProviderNameAsync(database, binding, cancellationToken);

            // The data the binding points at. The in-memory backend never has any to give: a connection's
            // database lives in the worker's own heap, where its admin store cannot see it - so moving away
            // from it copies nothing, which is exactly what the library does with one.
            var holder = providers.FirstOrDefault(provider => string.Equals(provider.Name, binding, StringComparison.OrdinalIgnoreCase));

            if (holder is not null && string.Equals(holder.Name, BsqliteInMemoryStorageProvider.ProviderName, StringComparison.OrdinalIgnoreCase) is false)
            {
                await holder.Admin.ImportAsync(database, StandInDatabase(), cancellationToken);
            }
        }

        BsqliteStorageResolution? initial = null;
        BsqliteStorageResolution? resolution = null;
        IReadOnlyList<BsqliteStorageCandidateOutcome>? attempts = null;
        string? error = null;

        try
        {
            initial = await diagnostic!.ResolveAsync(database, selection!, cancellationToken);
        }
        catch (BsqliteStorageUnavailableException)
        {
            // The open below fails the same way, and says so with the same exception.
        }

        try
        {
            var factory = new BsqliteSessionFactory(resolver!, new LabTransportFactory(), selection!, bindings, new BsqliteStorageMigrator(), log.For<BsqliteSessionFactory>());

            await using var session = await factory.OpenAsync(database, cancellationToken);

            resolution = session.Resolution;
            attempts = resolution.Attempts;
        }
        catch (BsqliteStorageUnavailableException exception)
        {
            error = exception.Message;
            attempts = exception.Attempts;
        }

        var bindingAfter = await bindings.GetProviderNameAsync(database, cancellationToken);
        var migration = await MigrationReportAsync(mode, initial, bindingAfter, database, log, cancellationToken);

        var plan = new BsqliteStoragePlanDto
        {
            Opens = resolution is not null,
            Provider = resolution?.Provider.Name,
            EngineBuild = resolution?.Provider.Capabilities.RequiredBuild.ToString(),
            VfsModule = resolution?.Provider.VfsModule?.ModuleUrl,
            IsFirstChoice = resolution?.IsFirstChoice,
            WasDecidedByExistingData = resolution?.WasDecidedByExistingData,
            BetterProviderAvailable = resolution?.BetterProviderAvailable?.Name,
            BindingBefore = binding,
            BindingAfter = bindingAfter,
            Migration = migration,
            Candidates = Report(candidateNames, providers, binding, attempts, resolution, migration),
            Error = error,
            Configuration = configuration,
            Log = [.. log.Lines],
            Notes = []
        };

        return plan with { Notes = [.. notes, .. Explain(plan, selection!, providers)] };
    }

    /// <summary>The automatic migration the session factory ran, read back from what it left behind.</summary>
    private static async Task<BsqliteMigrationReportDto?> MigrationReportAsync(
        BsqliteStorageMigrationMode mode,
        BsqliteStorageResolution? initial,
        string? bindingAfter,
        string database,
        LabLog log,
        CancellationToken cancellationToken)
    {
        if (initial?.BetterProviderAvailable is not { } better) return null;

        if (mode is not BsqliteStorageMigrationMode.AutomaticOnOpen)
        {
            return new BsqliteMigrationReportDto { From = initial.Provider.Name, To = better.Name, Outcome = "NotAttempted" };
        }

        var migrated = string.Equals(bindingAfter, better.Name, StringComparison.OrdinalIgnoreCase);

        return new BsqliteMigrationReportDto
        {
            From = initial.Provider.Name,
            To = better.Name,
            Outcome = migrated ? "Migrated" : "Failed",
            // Whether the target now holds an image is the difference between a move and a rebinding:
            // the migrator flips the binding without copying when the source had nothing to export.
            Copied = migrated ? await better.Admin.ExistsAsync(database, cancellationToken) : null,
            Error = migrated ? null : log.LastException
        };
    }

    /// <summary>
    /// Every provider the plan involves, with what the library decided about each: the candidates in
    /// preference order, then the registered providers that are not candidates, then a bound provider that
    /// is neither - each one matters to the outcome, and only the resolver's attempts say how.
    /// </summary>
    private static BsqliteCandidateReportDto[] Report(
        string[] candidates,
        LabProvider[] providers,
        string? binding,
        IReadOnlyList<BsqliteStorageCandidateOutcome>? attempts,
        BsqliteStorageResolution? resolution,
        BsqliteMigrationReportDto? migration)
    {
        var names = new List<string>();

        foreach (var name in candidates.Concat(providers.Select(provider => provider.Name)).Append(binding ?? string.Empty))
        {
            if (name.Length > 0 && names.Contains(name, StringComparer.OrdinalIgnoreCase) is false) names.Add(name);
        }

        return [.. names.Select(name =>
        {
            var provider = providers.FirstOrDefault(provider => string.Equals(provider.Name, name, StringComparison.OrdinalIgnoreCase));
            var attempt = attempts?.FirstOrDefault(outcome => string.Equals(outcome.ProviderName, name, StringComparison.OrdinalIgnoreCase));
            var rank = Array.FindIndex(candidates, candidate => string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase));

            return new BsqliteCandidateReportDto
            {
                Name = provider?.Name ?? name,
                Rank = rank < 0 ? null : rank + 1,
                Registered = provider is not null,
                FirstParty = BsqliteStorageCatalog.Instance(name) is not null,
                Persistent = provider?.Capabilities.IsPersistent,
                Available = provider?.IsAvailable,
                EngineBuild = provider?.Capabilities.RequiredBuild.ToString(),
                Status = attempt?.Status.ToString(),
                Explanation = attempt is not null
                    ? attempt.Explanation ?? attempt.Probe?.UnavailableReason
                    : NotExamined(name, provider, binding, rank, resolution, attempts, migration)
            };
        })];
    }

    /// <summary>Why the resolver never looked at a provider - which is itself part of the answer.</summary>
    private static string? NotExamined(
        string name,
        LabProvider? provider,
        string? binding,
        int rank,
        BsqliteStorageResolution? resolution,
        IReadOnlyList<BsqliteStorageCandidateOutcome>? attempts,
        BsqliteMigrationReportDto? migration)
    {
        if (attempts is null) return null;

        if (migration is { Outcome: "Migrated" } && string.Equals(migration.From, name, StringComparison.OrdinalIgnoreCase))
        {
            return $"Held the database until AutomaticOnOpen moved it to '{migration.To}'; once the copy was verified and the binding flipped, the source was deleted.";
        }

        if (resolution?.BetterProviderAvailable is { } better && string.Equals(better.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            return "Available and ranked above the backend holding the data - reported as BetterProviderAvailable, which the migration mode decides the response to.";
        }

        if (binding is not null)
        {
            return $"Not examined: the database already lives on '{resolution?.Provider.Name ?? binding}', and existing data outranks preference.";
        }

        if (rank < 0)
        {
            return provider is null ? null : "Registered but not a candidate: selection never picks it for a new database, though a database bound to it still opens there.";
        }

        return null;
    }

    /// <summary>What the outcome means in practice, and what to change when it is not the one wanted.</summary>
    private static IEnumerable<string> Explain(BsqliteStoragePlanDto plan, BsqliteStorageSelection selection, LabProvider[] providers)
    {
        var chosen = providers.FirstOrDefault(provider => provider.Name == plan.Provider);

        if (plan.Opens && chosen is not null)
        {
            yield return chosen.Capabilities.RequiredBuild is BsqliteEngineBuild.Synchronous
                ? $"'{chosen.Name}' runs on the synchronous engine build - the smallest download, with no JSPI or Asyncify."
                : $"'{chosen.Name}' needs the async-capable engine build: JSPI where the browser has it, Asyncify (about twice the size) otherwise.";

            if (chosen.Capabilities.IsPersistent is false)
            {
                yield return plan.WasDecidedByExistingData is true
                    ? $"'{chosen.Name}' does not survive a reload: everything written in this session is gone when the page closes."
                    : $"'{chosen.Name}' does not survive a reload: everything written in this session is gone when the page closes. No binding is written for it - it would protect no data - so the next load chooses again from the preference order.";
            }

            if (plan.WasDecidedByExistingData is true && plan.Migration is not { Outcome: "Migrated" })
            {
                yield return "The sticky binding decided: a database stays on the backend that holds it, whatever the preference order says. The binding is not rewritten.";
            }
            else if (plan.IsFirstChoice is false)
            {
                yield return "This is a fallback, so the resolver logged a warning naming every higher-ranked candidate and why it was rejected; session.Resolution.Attempts carries the same.";
            }

            if (plan.WasDecidedByExistingData is not true && plan.BindingAfter is not null)
            {
                yield return $"The binding to '{plan.BindingAfter}' is written only after the open succeeds, so a failed open never leaves one pointing at nothing. From then on this database reopens there.";
            }
        }

        if (plan.BetterProviderAvailable is { } better && plan.Migration is null or { Outcome: "NotAttempted" })
        {
            yield return selection.MigrationMode is BsqliteStorageMigrationMode.Manual
                ? $"'{better}' is available and ranked higher, and Manual leaves it to the application: call BsqliteStorageMigrator.MigrateAsync with the database closed (BsqliteDatabase.WhileClosedAsync) when it suits - the demo's BrowserDatabase.SwitchStorageAsync does exactly that."
                : $"'{better}' is available and ranked higher, but KeepExisting moves nothing. WithMigrationMode(BsqliteStorageMigrationMode.AutomaticOnOpen) would copy the database there before opening it.";
        }

        if (plan.Migration is { Outcome: "Migrated" } migrated)
        {
            yield return migrated.Copied is true
                ? $"AutomaticOnOpen moved the database: exported from '{migrated.From}', header checked, imported into '{migrated.To}', read back and compared byte for byte, binding flipped, source deleted."
                : $"AutomaticOnOpen flipped the binding to '{migrated.To}' without copying anything: '{migrated.From}' held no image its admin could export. Moving off in-memory always ends like this - its data lives in the worker's heap.";
        }

        if (plan.Migration is { Outcome: "Failed" } failed)
        {
            yield return string.Equals(plan.Provider, failed.To, StringComparison.OrdinalIgnoreCase)
                ? $"The automatic migration to '{failed.To}' failed after the copy was verified and bound - deleting '{failed.From}' failed with the source no longer whole - so the database opened on '{failed.To}', where the binding points."
                : $"The automatic migration to '{failed.To}' failed and was logged; the binding still names '{failed.From}', so the database opened there with its data, and the move is offered again on the next open.";
        }

        if (plan.Candidates.Any(candidate => candidate.Status == nameof(BsqliteStorageCandidateStatus.RejectedAsNonPersistent)))
        {
            yield return "A non-persistent backend was usable but refused as a fallback: AllowNonPersistentFallback() is the opt-in that accepts losing data on reload. A non-persistent FIRST choice needs no opt-in.";
        }

        if (plan.Opens is false)
        {
            if (plan.BindingBefore is { } bound)
            {
                yield return plan.Candidates.FirstOrDefault(candidate => string.Equals(candidate.Name, bound, StringComparison.OrdinalIgnoreCase)) is { Registered: false }
                    ? $"The database lives on '{bound}', which is not registered. Register it again (AddStorageProvider) to reach the data - Bsqlite will not open an empty database elsewhere in its place."
                    : $"The database lives on '{bound}', which this browser cannot reach, and Bsqlite will not open an empty database elsewhere in its place. To start over deliberately, delete it with that provider's Admin.DeleteAsync and remove the binding (IBsqliteStorageBindingStore.RemoveAsync).";
            }
            else
            {
                yield return "No candidate could be used, so the open throws BsqliteStorageUnavailableException; its Attempts and its message name every candidate and why it was rejected.";
            }
        }

        foreach (var missing in plan.Candidates.Where(candidate => candidate.Rank is not null && candidate.Registered is false))
        {
            yield return missing.FirstParty
                ? $"'{missing.Name}' is a candidate but not registered: add AddStorageProvider<{BsqliteStorageCatalog.Find(missing.Name)?.TypeName}>() and a reference to its package, {BsqliteStorageCatalog.Find(missing.Name)?.Package}."
                : $"'{missing.Name}' is a candidate but no provider of that name is registered: register your IBsqliteStorageProvider with AddStorageProvider.";
        }

        if (selection.IsStrict)
        {
            yield return "A single candidate is strict: it is used or the open fails - a lone candidate is never quietly substituted.";
        }
    }

    /// <summary>The registration this plan describes, as the C# that produces it.</summary>
    private static string Configuration(string[] candidates, string[] registered, bool allowNonPersistentFallback, BsqliteStorageMigrationMode mode, string database)
    {
        // '\n' rather than AppendLine: the snippet is a JSON string, and it should read the same whichever
        // operating system this server runs on.
        var builder = new StringBuilder();

        builder.Append("builder.Services.AddBitBsqliteServices(sqlite => sqlite\n");

        foreach (var name in registered)
        {
            builder.Append(BsqliteStorageCatalog.Find(name) is { } known && BsqliteStorageCatalog.Instance(name) is not null
                ? $"    .AddStorageProvider<{known.TypeName}>()\n"
                : $"    .AddStorageProvider<YourStorageProvider>()   // Name == \"{name}\"\n");
        }

        builder.Append("    .UseStorage(s => s");

        for (var index = 0; index < candidates.Length; index++)
        {
            builder.Append($"\n        .{(index == 0 ? "Prefer" : "Fallback")}({NameExpression(candidates[index])})");
        }

        if (allowNonPersistentFallback) builder.Append("\n        .AllowNonPersistentFallback()");
        if (mode is not BsqliteStorageMigrationMode.KeepExisting) builder.Append($"\n        .WithMigrationMode(BsqliteStorageMigrationMode.{mode})");

        builder.Append($")\n    .AddDatabase(\"{database}\"));");

        return builder.ToString();
    }

    private static string NameExpression(string name)
        => BsqliteStorageCatalog.Instance(name) is not null && BsqliteStorageCatalog.Find(name) is { } known
            ? $"{known.TypeName}.ProviderName"
            : $"\"{name}\"";

    private static string? Validate(string[] candidates, string[] registered, HashSet<string> unavailable, HashSet<string> nonPersistent, string? binding, string database)
    {
        foreach (var (list, label) in new[] { (candidates, "candidates"), (registered, "registered"), (unavailable.ToArray(), "unavailable"), (nonPersistent.ToArray(), "nonPersistent") })
        {
            if (list.Length > MaxNames) return $"'{label}' names {list.Length} backends; this tool plans for at most {MaxNames}.";

            if (list.FirstOrDefault(name => name.Length > MaxNameLength) is { } longName) return $"'{longName[..20]}...' is longer than a provider name can usefully be ({MaxNameLength} characters).";
        }

        if (binding?.Length > MaxNameLength) return $"existingBinding is longer than a provider name can usefully be ({MaxNameLength} characters).";

        return database.Length > 256 ? "databaseName is longer than 256 characters." : null;
    }

    /// <summary>A list of provider names as a caller writes one: "opfs, indexeddb", "opfs indexeddb" or "opfs;indexeddb".</summary>
    private static string[] Names(string? text)
        => string.IsNullOrWhiteSpace(text) ? [] : text.Split([',', ';', ' ', '\t', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>A SQLite header followed by zeros: what the migrator checks and compares, and nothing an engine could open.</summary>
    private static byte[] StandInDatabase()
    {
        var image = new byte[StandInDatabaseBytes];

        SqliteMagic.CopyTo(image);

        return image;
    }

    /// <summary>
    /// A backend as the caller described it. A first-party name keeps the real provider's name,
    /// capabilities and VFS module, so selection and the engine-build choice see exactly what they would
    /// in a browser; only the probe is the caller's, since that is the browser being described.
    /// </summary>
    internal sealed class LabProvider : IBsqliteStorageProvider
    {
        private readonly BsqliteProbeResult _probe;

        public LabProvider(string name, bool unavailable, bool nonPersistent, long? freeBytes)
        {
            var real = BsqliteStorageCatalog.Instance(name);

            Name = real?.Name ?? name;
            IsAvailable = unavailable is false;

            // A custom backend is described by the two facts that cannot be guessed safely - the same two the
            // contract makes required - and the least capable answer for everything else, as the contract's
            // own defaults are.
            Capabilities = real?.Capabilities ?? new BsqliteStorageCapabilities
            {
                RequiredBuild = BsqliteEngineBuild.AsyncCapable,
                IsPersistent = nonPersistent is false,
            };

            VfsModule = real?.VfsModule;

            // An admin store of the library's own: the in-memory backend's, which exports, imports and
            // compares real bytes - what the migrator needs to do its real work.
            Admin = new BsqliteInMemoryStorageProvider(Name).Admin;

            _probe = unavailable
                ? BsqliteProbeResult.Unavailable($"'{Name}' was described as unavailable in this browser.")
                : BsqliteProbeResult.Available(quotaBytes: freeBytes, usageBytes: freeBytes is null ? null : 0);
        }

        public string Name { get; }

        public bool IsAvailable { get; }

        public BsqliteStorageCapabilities Capabilities { get; }

        public BsqliteJsModule? VfsModule { get; }

        public IBsqliteStorageAdmin Admin { get; }

        public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(_probe);
    }

    /// <summary>Hands the session factory a transport that opens nothing, as the library's own session tests do.</summary>
    private sealed class LabTransportFactory : IBsqliteTransportFactory
    {
        public IBsqliteTransport Create(IBsqliteStorageProvider provider) => new LabTransport();
    }

    /// <summary>
    /// A transport with no engine behind it. Opening is all the session factory does with one, so that
    /// is all it supports: a batch reaching it would mean SQL about to run, and that does not happen here.
    /// </summary>
    private sealed class LabTransport : IBsqliteTransport
    {
        public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(IReadOnlyList<BsqliteCommandRequest> batch, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("The storage lab has no engine: it plans an open, and never executes SQL.");

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    /// <summary>
    /// What the resolver and the session factory log while they decide - the warning a fallback raises,
    /// the information a better backend left unused is reported as, the failure of an automatic
    /// migration - captured as the lines an application's log would show.
    /// </summary>
    private sealed class LabLog
    {
        private readonly List<string> _lines = [];

        public IReadOnlyList<string> Lines => _lines;

        /// <summary>The message of the last exception logged - an automatic migration's failure.</summary>
        public string? LastException { get; private set; }

        public ILogger<T> For<T>() => new Sink<T>(this);

        private void Write(LogLevel level, string message, Exception? exception)
        {
            _lines.Add(exception is null ? $"{level}: {message}" : $"{level}: {message} ({exception.GetType().Name}: {exception.Message})");

            if (exception is not null) LastException = exception.Message;
        }

        private sealed class Sink<T>(LabLog log) : ILogger<T>
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (IsEnabled(logLevel)) log.Write(logLevel, formatter(state, exception), exception);
            }
        }
    }
}
