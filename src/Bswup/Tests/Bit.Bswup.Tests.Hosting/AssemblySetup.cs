using Bit.Bswup.Tests.Hosting.Infrastructure;

// Classes in parallel, tests within a class in order: every test reads responses from a factory it shares
// read-only, on a session (host name) of its own.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Bit.Bswup.Tests.Hosting;

[TestClass]
public static class AssemblySetup
{
    [AssemblyCleanup]
    public static async Task CleanupAsync()
    {
        await HarnessHostFactory.DisposeSharedAsync();
#if NET11_0_OR_GREATER
        await SampleHostFactories.DisposeAsync();
#endif
    }
}
