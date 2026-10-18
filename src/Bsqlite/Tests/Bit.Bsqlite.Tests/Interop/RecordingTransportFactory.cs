using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// Hands out scripted transports and remembers which provider each one was created for, so a test
/// can see that the factory asked for the backend selection actually chose.
/// </summary>
internal sealed class RecordingTransportFactory : IBsqliteTransportFactory
{
    private readonly Func<ScriptedTransport> _create;

    public RecordingTransportFactory(Func<ScriptedTransport>? create = null)
        => _create = create ?? (() => new ScriptedTransport());

    public List<(IBsqliteStorageProvider Provider, ScriptedTransport Transport)> Created { get; } = [];

    public ScriptedTransport Last
        => Created.Count > 0
            ? Created[^1].Transport
            : throw new InvalidOperationException("No transport has been created.");

    public IBsqliteTransport Create(IBsqliteStorageProvider provider)
    {
        var transport = _create();
        Created.Add((provider, transport));
        return transport;
    }
}

/// <summary>An <see cref="IBsqliteTransport"/> whose open can be told to fail.</summary>
internal sealed class ScriptedTransport : IBsqliteTransport
{
    public string? OpenedAs { get; private set; }

    public int OpenCount { get; private set; }

    public bool Disposed { get; private set; }

    public Exception? OpenThrows { get; init; }

    public Task OpenAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        OpenCount++;
        OpenedAs = databaseName;

        if (OpenThrows is not null)
        {
            throw OpenThrows;
        }

        return Task.CompletedTask;
    }

    public Task CloseAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task<IReadOnlyList<BsqliteCommandResult>> ExecuteAsync(
        IReadOnlyList<BsqliteCommandRequest> batch,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BsqliteCommandResult>>([]);

    public ValueTask DisposeAsync()
    {
        Disposed = true;
        return ValueTask.CompletedTask;
    }
}

internal sealed class ConfigurableProvider : IBsqliteStorageProvider
{
    public required string Name { get; init; }

    public required BsqliteEngineBuild RequiredBuild { get; init; }

    public bool IsPersistent { get; init; } = true;

    public bool IsAvailable { get; init; } = true;

    public BsqliteJsModule? Vfs { get; init; }

    public BsqliteStorageCapabilities Capabilities => new()
    {
        RequiredBuild = RequiredBuild,
        IsPersistent = IsPersistent,
    };

    public BsqliteJsModule? VfsModule => Vfs;

    public IBsqliteStorageAdmin Admin => throw new NotSupportedException();

    public ValueTask<BsqliteProbeResult> ProbeAsync(CancellationToken cancellationToken = default)
        => ValueTask.FromResult(IsAvailable
            ? BsqliteProbeResult.Available()
            : BsqliteProbeResult.Unavailable($"'{Name}' is switched off."));
}
