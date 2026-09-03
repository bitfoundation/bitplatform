namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Base for tests written once and run on every platform that carries the app: the tests live in an abstract class
/// under Features/Core, and a shell class per platform inherits it, picking only its <see cref="AppOpener"/> and the
/// platform's category/parallelism attributes. The hybrid shells inherit AppPageTest's browser page too and never
/// touch it, which keeps its timeout, remote-server and video-on-retry setup single-sourced.
/// </summary>
public abstract class AppsTestBase : AppPageTest
{
    private readonly List<IAsyncDisposable> cleanups = [];

    protected abstract IAppOpener AppOpener { get; }

    /// <summary>Keeps what an <see cref="IAppOpener"/> created (e.g. a launched hybrid app) alive for exactly the test.</summary>
    public void RegisterForCleanup(IAsyncDisposable disposable) => cleanups.Add(disposable);

    /// <summary>
    /// A live page of <paramref name="app"/> on this shell's platform; inconclusive when the app has no build there,
    /// so a coverage gap shows up as skipped instead of hiding as a pass.
    /// </summary>
    protected async Task<IPage> OpenApp(App app)
    {
        var page = await AppOpener.TryOpen(this, app);

        if (page is null)
            Assert.Inconclusive($"{app} has no build on this platform.");

        return page!;
    }

    [TestCleanup]
    public async ValueTask AppsCleanup()
    {
        foreach (var cleanup in cleanups)
            await cleanup.DisposeAsync();

        cleanups.Clear();
    }
}
