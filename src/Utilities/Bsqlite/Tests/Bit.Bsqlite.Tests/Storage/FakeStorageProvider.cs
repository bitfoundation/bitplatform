using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Storage.Tests;

/// <summary>
/// A provider whose availability, persistence, and probe behaviour are dictated by the test, so that
/// selection can be driven through browser conditions that are awkward to reproduce for real.
/// </summary>
/// <remarks>
/// Its admin surface answers one question - which databases it holds, from <c>holds</c> - which is
/// all selection asks of it, and only for a database with no binding. Anything more throws.
/// </remarks>
internal sealed class FakeStorageProvider(
    string name,
    bool isAvailable = true,
    bool isPersistent = true,
    Exception? probeThrows = null,
    params string[] holds) : IBsqliteStorageProvider
{
    public string Name { get; } = name;

    /// <summary>How many times the provider was probed, used to assert the resolver caches.</summary>
    public int ProbeCount { get; private set; }

    public BsqliteStorageCapabilities Capabilities { get; } = new()
    {
        RequiredBuild = BsqliteEngineBuild.AsyncCapable,
        IsPersistent = isPersistent,
    };

    public BsqliteJsModule? VfsModule => null;

    /// <summary>How many times selection asked whether a database is here.</summary>
    public int ExistsCount { get; private set; }

    public IBsqliteStorageAdmin Admin => new HoldingAdmin(this, holds);

    public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
    {
        ProbeCount++;

        if (probeThrows is not null)
        {
            throw probeThrows;
        }

        return ValueTask.FromResult(isAvailable
            ? BsqliteProbeResult.Available(quotaBytes: 1024 * 1024)
            : BsqliteProbeResult.Unavailable($"'{Name}' is switched off for this test."));
    }

    private sealed class HoldingAdmin(FakeStorageProvider owner, string[] holds) : IBsqliteStorageAdmin
    {
        public ValueTask<bool> ExistsAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            owner.ExistsCount++;
            return ValueTask.FromResult(holds.Contains(databaseName, StringComparer.Ordinal));
        }

        public ValueTask<IReadOnlyList<string>> ListAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Selection only asks whether a database exists.");

        public ValueTask DeleteAsync(string databaseName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Selection only asks whether a database exists.");

        public ValueTask<byte[]> ExportAsync(string databaseName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Selection only asks whether a database exists.");

        public ValueTask ImportAsync(string databaseName, ReadOnlyMemory<byte> contents, CancellationToken cancellationToken = default)
            => throw new NotSupportedException("Selection only asks whether a database exists.");
    }
}
