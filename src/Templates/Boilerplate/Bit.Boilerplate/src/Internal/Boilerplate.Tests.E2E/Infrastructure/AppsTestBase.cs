namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Base for tests written once and run against every platform that carries the app: the feature's tests live in an
/// abstract class under Features/Core, and a shell class per platform inherits it, only picking its
/// <see cref="AppOpener"/> and the platform's category/parallelism attributes. The tests call <see cref="OpenApp"/>
/// and drive the returned page without knowing whether it lives in a browser tab or an installed app's WebView.
/// <para>
/// The hybrid shells inherit the browser-page plumbing too and simply never touch the tab it opens; that costs
/// milliseconds and keeps the timeout, remote-server and video-on-retry setup single-sourced in AppPageTest.
/// </para>
/// </summary>
public abstract class AppsTestBase : AppPageTest
{
    private readonly List<IAsyncDisposable> cleanups = [];

    protected abstract IAppOpener AppOpener { get; }

    /// <summary>
    /// What an <see cref="IAppOpener"/> created lives exactly as long as the test: registered here, closed by
    /// <see cref="AppsCleanup"/> (e.g. the launched hybrid app).
    /// </summary>
    public void RegisterForCleanup(IAsyncDisposable disposable) => cleanups.Add(disposable);

    /// <summary>
    /// A live page of <paramref name="app"/> on this shell's platform; inconclusive when the app has no build there,
    /// so a coverage gap shows up as skipped rather than hiding as a pass.
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
