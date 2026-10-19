using Bit.Bsqlite.Data;
using Bit.Bsqlite.Storage.ConformanceTests;
using Bit.Bsqlite.Testing;

namespace Bit.Bsqlite.Interop.Tests;

/// <summary>
/// Runs the engine kit against the in-process transport, which is how the kit is shown to be
/// honest: a rule the reference engine cannot satisfy is a bug in the rule. Crash safety is
/// correctly skipped - there is no worker to kill and nothing persists.
/// </summary>
[TestClass]
public sealed class InProcessEngineConformanceTests : BsqliteStorageEngineConformanceTests
{
    /// <inheritdoc />
    protected override async ValueTask<IBsqliteTransport> CreateOpenTransportAsync()
    {
        var transport = new BsqliteInProcessTransport();
        await transport.OpenAsync("conformance.db");
        return transport;
    }
}
