using Bit.Blex.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blex.Tests.E2E;

/// <summary>
/// Static server-side rendering: no interactive runtime at all. <c>&lt;BlexProvider&gt;</c> still renders its
/// content and the stores still render their state, but nothing that needs JS interop - browser storage,
/// the DevTools bridge - may be attempted, and nothing may fail for want of it.
/// </summary>
[TestClass]
public class StaticSsrModeTests : HarnessTest
{
    private readonly WebSession _session = new();

    protected override string BaseUrl => _session.Host.BaseUrl;

    protected override string Framework => E2EEnvironment.Framework;

    protected override Task<IPage> OpenPageAsync() => _session.OpenAsync("ssr");

    protected override Task ClosePageAsync() => _session.CloseAsync();

    [TestMethod]
    public async Task The_page_renders_the_stores_state_without_an_interactive_runtime()
    {
        var response = await GotoAsync("/");

        Assert.AreEqual(200, response!.Status);
        await ExpectCountAsync(0);
        await Expect(Page.Locator("#token")).ToHaveAttributeAsync("data-token", new System.Text.RegularExpressions.Regex("^[0-9a-f]{32}$"));
        await Expect(Page.Locator("#status")).ToHaveAttributeAsync("data-interactive", "false");
    }

    [TestMethod]
    public async Task No_blex_module_is_requested_during_static_rendering()
    {
        await GotoAsync("/");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var requests = await BlexModuleRequestsAsync();

        Assert.AreEqual(0, requests.Length, $"Static rendering requested Blex modules: {string.Join(", ", requests.Select(r => r.Url))}");
    }

    [TestMethod]
    public async Task Every_harness_page_renders_statically()
    {
        foreach (var (path, page) in new[] { ("/", "#page-home"), ("/selector", "#page-selector"), ("/effects", "#page-effects") })
        {
            var response = await GotoAsync(path);

            Assert.AreEqual(200, response!.Status, $"{path} answered {response.Status}.");
            await Expect(Page.Locator(page)).ToBeVisibleAsync();
        }
    }
}
