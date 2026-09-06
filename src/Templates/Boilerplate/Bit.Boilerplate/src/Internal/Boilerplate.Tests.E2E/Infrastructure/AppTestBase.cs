using Boilerplate.Tests.E2E.Infrastructure.Services;

namespace Boilerplate.Tests.E2E.Infrastructure;

/// <summary>
/// Base for a test that drives one of the apps; the <see cref="IAppOpener"/> a derived class picks is what decides
/// which platform it runs on.
/// </summary>
public abstract class AppTestBase : AppPageTest
{
    private readonly List<Func<Task>> cleanups = [];

    protected abstract IAppOpener AppOpener { get; }

    /// <summary>Stops what an <see cref="IAppOpener"/> launched, at the end of exactly this test.</summary>
    public void RegisterForCleanup(Func<Task> onStop) => cleanups.Add(onStop);

    /// <summary>
    /// A live page of <paramref name="app"/>; inconclusive when it has no build on this platform, so a coverage gap
    /// shows up as skipped rather than hiding as a pass.
    /// </summary>
    protected async Task<IPage> OpenApp(App app)
    {
        var page = await AppOpener.TryOpen(this, app);

        if (page is null)
            Assert.Inconclusive($"{app} has no build on this platform.");

        return page!;
    }

    /// <summary>Generous: a first visit includes the WebAssembly boot / bswup precache on a cold cache.</summary>
    protected async Task WaitUntilInteractive(IPage page)
    {
        await Expect(page.Locator("main .main-container").First)
            .ToBeVisibleAsync(new() { Timeout = (float)TimeSpan.FromMinutes(2).TotalMilliseconds });
    }

    [TestCleanup]
    public async ValueTask AppsCleanup()
    {
        foreach (var cleanup in cleanups)
            await cleanup();

        cleanups.Clear();
    }
}
