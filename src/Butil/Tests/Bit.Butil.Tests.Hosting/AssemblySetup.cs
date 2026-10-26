using ButilTests.Hosting.Infrastructure;

// Classes in parallel, tests within a class in order: every test reads responses from a factory it either shares
// read-only or owns.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace ButilTests.Hosting;

[TestClass]
public static class AssemblySetup
{
    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
#if NET10_0_OR_GREATER
        // One host failing to stop still leaves the others to be stopped.
        try
        {
            await HarnessHostFactory.DisposeSharedAsync();
        }
        finally
        {
            await SampleHostFactories.DisposeAsync();
        }
#else
        await HarnessHostFactory.DisposeSharedAsync();
#endif
    }
}
