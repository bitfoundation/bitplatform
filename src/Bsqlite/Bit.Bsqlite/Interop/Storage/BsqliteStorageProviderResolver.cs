using Bit.Bsqlite.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bit.Bsqlite.Interop;

/// <summary>
/// Decides which storage backend serves a database, honouring configured preference but never at the
/// cost of data that already exists somewhere else.
/// </summary>
/// <remarks>
/// <para>
/// Two rules shape everything here. First, existing data outranks preference: if a database was
/// created on one backend, that backend is used, and if it cannot be reached the open fails rather
/// than quietly producing an empty database elsewhere. Second, nothing is substituted silently - a
/// fallback is logged, a non-persistent fallback needs an explicit opt-in, and total failure reports
/// every candidate's fate.
/// </para>
/// <para>
/// The binding store is how the first rule usually knows where the data is, but it is not the only
/// record: an application that clears <c>localStorage</c> on sign-out loses the bindings and keeps
/// the files. So a database with no binding is looked for on every available persistent backend
/// before it is treated as new, and found data is bound again rather than shadowed by an empty file
/// on a backend ranked higher.
/// </para>
/// <para>
/// <see cref="ResolveAsync"/> makes no changes, so it is safe to call for diagnostics. The chosen
/// backend is recorded only when the caller confirms the database really opened, via
/// <see cref="CommitBindingAsync"/>.
/// </para>
/// </remarks>
public sealed class BsqliteStorageProviderResolver
{
    private readonly Dictionary<string, IBsqliteStorageProvider> _providers;
    private readonly IBsqliteStorageBindingStore _bindingStore;
    private readonly ILogger _logger;
    private readonly Dictionary<string, BsqliteProbeResult> _probeCache =
        new(StringComparer.OrdinalIgnoreCase);

    public BsqliteStorageProviderResolver(
        IEnumerable<IBsqliteStorageProvider> providers,
        IBsqliteStorageBindingStore bindingStore,
        ILogger<BsqliteStorageProviderResolver>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(bindingStore);

        _bindingStore = bindingStore;
        _logger = logger ?? NullLogger<BsqliteStorageProviderResolver>.Instance;
        _providers = new Dictionary<string, IBsqliteStorageProvider>(StringComparer.OrdinalIgnoreCase);

        foreach (var provider in providers)
        {
            if (!_providers.TryAdd(provider.Name, provider))
            {
                throw new InvalidOperationException(
                    $"Two storage providers are registered under the name '{provider.Name}'. "
                    + "Provider names identify where a database lives, so they must be unique.");
            }
        }
    }

    /// <summary>Chooses a backend for <paramref name="databaseName"/> without changing anything.</summary>
    /// <exception cref="BsqliteStorageUnavailableException">
    /// No candidate could be used, or the database exists on a backend that cannot be reached.
    /// </exception>
    public async ValueTask<BsqliteStorageResolution> ResolveAsync(
        string databaseName,
        BsqliteStorageSelection selection,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        ArgumentNullException.ThrowIfNull(selection);

        var boundProviderName = await _bindingStore
            .GetProviderNameAsync(databaseName, cancellationToken)
            .ConfigureAwait(false);

        if (boundProviderName is not null)
        {
            return await ResolveExistingDatabaseAsync(databaseName, boundProviderName, selection, cancellationToken)
                .ConfigureAwait(false);
        }

        var holder = await FindUnboundDataAsync(databaseName, selection, cancellationToken).ConfigureAwait(false);
        if (holder is not null)
        {
            _logger.LogWarning(
                "Database {DatabaseName} has no storage binding, but storage provider {Provider} holds "
                + "it - the binding store was probably cleared. Opening the existing data there and "
                + "binding it again.",
                databaseName,
                holder.Name);

            return await ResolveExistingDatabaseAsync(databaseName, holder.Name, selection, cancellationToken)
                    .ConfigureAwait(false) with
                {
                    WasFoundWithoutBinding = true,
                };
        }

        return await ResolveNewDatabaseAsync(databaseName, selection, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Records the resolved backend as the home of the database. Call this once the database has
    /// actually opened, so a failed open does not leave a binding pointing at nothing.
    /// </summary>
    /// <remarks>
    /// A backend that does not persist is not recorded: its database is gone by the next load, so the
    /// binding would protect no data - and, outranking preference, it would keep every later visit on
    /// storage that empties on reload even once a persistent backend is available again.
    /// </remarks>
    public ValueTask CommitBindingAsync(
        BsqliteStorageResolution resolution,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resolution);

        var needsCommit = resolution.WasFoundWithoutBinding
            || (!resolution.WasDecidedByExistingData && resolution.Provider.Capabilities.IsPersistent);

        return needsCommit
            ? _bindingStore.SetProviderNameAsync(
                resolution.DatabaseName,
                resolution.Provider.Name,
                cancellationToken)
            : ValueTask.CompletedTask;
    }

    /// <summary>
    /// Looks for <paramref name="databaseName"/> on every available persistent backend, preferred
    /// ones first, for a database whose binding has gone missing.
    /// </summary>
    /// <remarks>
    /// Non-persistent backends are skipped: whatever they held did not survive the load that lost
    /// the binding. A backend whose admin surface cannot answer is passed over rather than allowed to
    /// fail the open - the same leniency a throwing probe gets.
    /// </remarks>
    private async ValueTask<IBsqliteStorageProvider?> FindUnboundDataAsync(
        string databaseName,
        BsqliteStorageSelection selection,
        CancellationToken cancellationToken)
    {
        var ranked = selection.Candidates
            .Select(name => _providers.GetValueOrDefault(name))
            .Concat(_providers.Values)
            .OfType<IBsqliteStorageProvider>()
            .Distinct();

        foreach (var provider in ranked)
        {
            if (!provider.Capabilities.IsPersistent)
            {
                continue;
            }

            var probe = await ProbeAsync(provider, cancellationToken).ConfigureAwait(false);
            if (!probe.IsAvailable)
            {
                continue;
            }

            try
            {
                if (await provider.Admin.ExistsAsync(databaseName, cancellationToken).ConfigureAwait(false))
                {
                    return provider;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(
                    ex,
                    "Could not check whether storage provider {Provider} holds database {DatabaseName}.",
                    provider.Name,
                    databaseName);
            }
        }

        return null;
    }

    private async ValueTask<BsqliteStorageResolution> ResolveExistingDatabaseAsync(
        string databaseName,
        string boundProviderName,
        BsqliteStorageSelection selection,
        CancellationToken cancellationToken)
    {
        if (!_providers.TryGetValue(boundProviderName, out var bound))
        {
            // Falling through to preference order here would open a different, empty database and
            // report success, so the only safe answer is to stop and say what is missing.
            throw new BsqliteStorageUnavailableException(
                databaseName,
                [
                    new BsqliteStorageCandidateOutcome
                    {
                        ProviderName = boundProviderName,
                        Status = BsqliteStorageCandidateStatus.NotRegistered,
                        Explanation =
                            $"Database '{databaseName}' was created by storage provider "
                            + $"'{boundProviderName}', which is not registered. Register it to reach "
                            + "the existing data, or delete the database to start again elsewhere.",
                    },
                ]);
        }

        var probe = await ProbeAsync(bound, cancellationToken).ConfigureAwait(false);

        if (!probe.IsAvailable)
        {
            throw new BsqliteStorageUnavailableException(
                databaseName,
                [
                    new BsqliteStorageCandidateOutcome
                    {
                        ProviderName = bound.Name,
                        Status = BsqliteStorageCandidateStatus.Unavailable,
                        Probe = probe,
                        Explanation =
                            $"Database '{databaseName}' lives on storage provider '{bound.Name}', which "
                            + "is unavailable in this browser. Bsqlite will not open an empty "
                            + "database on another provider in its place.",
                    },
                ]);
        }

        var better = await FindBetterProviderAsync(bound.Name, selection, cancellationToken)
            .ConfigureAwait(false);

        if (better is not null)
        {
            _logger.LogInformation(
                "Database {DatabaseName} is on storage provider {Provider}, but {BetterProvider} is "
                + "now available and ranked higher. Migration mode is {MigrationMode}.",
                databaseName,
                bound.Name,
                better.Name,
                selection.MigrationMode);
        }

        return new BsqliteStorageResolution
        {
            DatabaseName = databaseName,
            Provider = bound,
            Probe = probe,
            IsFirstChoice = selection.Candidates.Count > 0
                && string.Equals(selection.Candidates[0], bound.Name, StringComparison.OrdinalIgnoreCase),
            WasDecidedByExistingData = true,
            BetterProviderAvailable = better,
            Attempts =
            [
                new BsqliteStorageCandidateOutcome
                {
                    ProviderName = bound.Name,
                    Status = BsqliteStorageCandidateStatus.Selected,
                    Probe = probe,
                    Explanation = $"Holds the existing database '{databaseName}'.",
                },
            ],
        };
    }

    private async ValueTask<BsqliteStorageResolution> ResolveNewDatabaseAsync(
        string databaseName,
        BsqliteStorageSelection selection,
        CancellationToken cancellationToken)
    {
        var attempts = new List<BsqliteStorageCandidateOutcome>(selection.Candidates.Count);

        for (var index = 0; index < selection.Candidates.Count; index++)
        {
            var candidateName = selection.Candidates[index];

            if (!_providers.TryGetValue(candidateName, out var provider))
            {
                attempts.Add(new BsqliteStorageCandidateOutcome
                {
                    ProviderName = candidateName,
                    Status = BsqliteStorageCandidateStatus.NotRegistered,
                    Explanation = "No storage provider is registered under this name.",
                });
                continue;
            }

            var probe = await ProbeAsync(provider, cancellationToken).ConfigureAwait(false);

            if (!probe.IsAvailable)
            {
                attempts.Add(new BsqliteStorageCandidateOutcome
                {
                    ProviderName = candidateName,
                    Status = BsqliteStorageCandidateStatus.Unavailable,
                    Probe = probe,
                });
                continue;
            }

            // A non-persistent first choice is what the application asked for. A non-persistent
            // *fallback* is a silent downgrade to storage that empties on reload, so it needs consent.
            if (index > 0
                && !provider.Capabilities.IsPersistent
                && !selection.AllowNonPersistentFallback)
            {
                attempts.Add(new BsqliteStorageCandidateOutcome
                {
                    ProviderName = candidateName,
                    Status = BsqliteStorageCandidateStatus.RejectedAsNonPersistent,
                    Probe = probe,
                    Explanation =
                        $"'{candidateName}' does not persist across reloads, and falling back to "
                        + "non-persistent storage would lose data without warning. Call "
                        + "AllowNonPersistentFallback() to accept that.",
                });
                continue;
            }

            attempts.Add(new BsqliteStorageCandidateOutcome
            {
                ProviderName = candidateName,
                Status = BsqliteStorageCandidateStatus.Selected,
                Probe = probe,
            });

            for (var rest = index + 1; rest < selection.Candidates.Count; rest++)
            {
                attempts.Add(new BsqliteStorageCandidateOutcome
                {
                    ProviderName = selection.Candidates[rest],
                    Status = BsqliteStorageCandidateStatus.NotProbed,
                    Explanation = $"'{candidateName}' was selected first.",
                });
            }

            if (index > 0)
            {
                _logger.LogWarning(
                    "Storage provider {Provider} was selected for database {DatabaseName} after "
                    + "{RejectedCount} higher-ranked provider(s) were rejected: {Rejected}",
                    provider.Name,
                    databaseName,
                    index,
                    string.Join("; ", attempts.Take(index)));
            }

            return new BsqliteStorageResolution
            {
                DatabaseName = databaseName,
                Provider = provider,
                Probe = probe,
                Attempts = attempts,
                IsFirstChoice = index == 0,
            };
        }

        throw new BsqliteStorageUnavailableException(databaseName, attempts);
    }

    /// <summary>
    /// Finds an available backend ranked above <paramref name="currentProviderName"/>, which is what
    /// makes "a better option exists" reportable rather than something the user has to guess at.
    /// </summary>
    private async ValueTask<IBsqliteStorageProvider?> FindBetterProviderAsync(
        string currentProviderName,
        BsqliteStorageSelection selection,
        CancellationToken cancellationToken)
    {
        foreach (var candidateName in selection.Candidates)
        {
            if (string.Equals(candidateName, currentProviderName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (_providers.TryGetValue(candidateName, out var provider)
                && provider.Capabilities.IsPersistent)
            {
                var probe = await ProbeAsync(provider, cancellationToken).ConfigureAwait(false);
                if (probe.IsAvailable)
                {
                    return provider;
                }
            }
        }

        return null;
    }

    private async ValueTask<BsqliteProbeResult> ProbeAsync(
        IBsqliteStorageProvider provider,
        CancellationToken cancellationToken)
    {
        if (_probeCache.TryGetValue(provider.Name, out var cached))
        {
            return cached;
        }

        BsqliteProbeResult probe;
        try
        {
            probe = await provider.ProbeAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // A provider that throws instead of reporting unavailability must not take the whole
            // selection down with it; the next candidate still deserves a chance.
            probe = BsqliteProbeResult.Unavailable(
                $"Probing '{provider.Name}' threw {ex.GetType().Name}: {ex.Message}");
        }

        _probeCache[provider.Name] = probe;
        return probe;
    }
}
