namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Nothing to initialize - this suite starts no server - but <see cref="DeployedApiClientProvider"/> holds a live MCP session
/// and database connections, closed here.
/// </summary>
[TestClass]
public partial class TestsAssemblyCleanup
{
    /// <summary>MSTest awaits a Task-returning cleanup, so nothing here blocks on a disposal.</summary>
    [AssemblyCleanup]
    public static Task Cleanup() => DeployedApiClientProvider.ShutdownAsync();
}
