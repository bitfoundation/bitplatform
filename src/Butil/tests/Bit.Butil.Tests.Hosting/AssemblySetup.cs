using ButilTests.Hosting.Infrastructure;

// Classes in parallel, tests within a class in order: every test reads responses from a factory it either shares
// read-only or owns.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace ButilTests.Hosting;

[TestClass]
public static class AssemblySetup
{
    [AssemblyCleanup]
    public static async Task CleanupAsync() => await HarnessHostFactory.DisposeSharedAsync();
}
