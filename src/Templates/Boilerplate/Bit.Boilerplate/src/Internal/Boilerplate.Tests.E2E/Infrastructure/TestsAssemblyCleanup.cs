namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Nothing to initialize - this suite starts no server, and <see cref="WindowsAppData"/> backs up on the first Windows
/// launch rather than in every stage - but the Windows apps' own data is put back here, and
/// <see cref="DeployedApiClientProvider"/>'s live MCP session and database connections are closed.
/// </summary>
[TestClass]
public partial class TestsAssemblyCleanup
{
    /// <summary>MSTest awaits a Task-returning cleanup, so nothing here blocks on a disposal.</summary>
    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        try
        {
            WindowsAppData.Restore();
        }
        finally
        {
            await DeployedApiClientProvider.ShutdownAsync();
        }
    }
}
