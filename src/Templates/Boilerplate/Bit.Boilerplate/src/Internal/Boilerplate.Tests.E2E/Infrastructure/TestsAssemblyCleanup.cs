namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// There is nothing to initialize - this suite starts no server, it talks to the deployed ones - but
/// <see cref="TestHost"/>, built on first use, holds pooled connections to a live database and is closed here.
/// </summary>
[TestClass]
public partial class TestsAssemblyCleanup
{
    [AssemblyCleanup]
    public static void Cleanup() => TestHost.Shutdown();
}
