namespace Boilerplate.WorkbenchTests.Infrastructure;

/// <summary>
/// Starts one Workbench for the whole run on first use and clears the review left by an earlier run. The review is one
/// document, so tests that share the server work with comments and flows of their own.
/// </summary>
[TestClass]
public static class WorkbenchTestsInitializer
{
    private static readonly Lazy<Task<WorkbenchTestServer>> sharedServer = new(() => new WorkbenchTestServer().Start());
    private static readonly Lazy<Task<WorkbenchTestServer>> readOnlyServer = new(() => new WorkbenchTestServer().Start(isDevMode: false));

    public static Task<WorkbenchTestServer> SharedServer => sharedServer.Value;

    /// <summary>A Workbench outside dev mode, like a published demo or the plain run bit CLI starts.</summary>
    public static Task<WorkbenchTestServer> ReadOnlyServer => readOnlyServer.Value;

    [AssemblyInitialize]
    public static void Initialize(TestContext testContext)
    {
        // The texts the tests look for are the default culture's, whatever this machine's is.
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfoManager.DefaultCulture;

        // Only under this run's output: a Workbench project above it would mean the folder holds a real review.
        if (WorkbenchFolders.Project is null && Directory.Exists(WorkbenchFolders.Reviews))
        {
            Directory.Delete(WorkbenchFolders.Reviews, recursive: true);
        }
    }

    [AssemblyCleanup]
    public static async Task Cleanup()
    {
        if (sharedServer.IsValueCreated)
        {
            await (await sharedServer.Value).DisposeAsync();
        }

        if (readOnlyServer.IsValueCreated)
        {
            await (await readOnlyServer.Value).DisposeAsync();
        }
    }
}
