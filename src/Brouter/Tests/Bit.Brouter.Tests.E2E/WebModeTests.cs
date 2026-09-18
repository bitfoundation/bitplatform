using Bit.Brouter.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Brouter.Tests.E2E;

/// <summary>An interactive render mode of the web harness host, each test in a browser context of its own.</summary>
public abstract class WebModeTests : InteractiveHarnessTests
{
    private readonly WebSession _session = new();

    /// <summary>The BrouterHarness:Mode the host is started with.</summary>
    protected abstract string Mode { get; }

    protected IBrowserContext Context => _session.Context;

    protected override string BaseUrl => _session.Host.BaseUrl;

    protected override string Framework => E2EEnvironment.Framework;

    protected override Task<IPage> OpenPageAsync() => _session.OpenAsync(Mode);

    protected override Task ClosePageAsync() => _session.CloseAsync();
}

/// <summary>What only a prerendered interactive mode has to get right: the first response, and the handover.</summary>
public abstract class PrerenderedWebModeTests : WebModeTests
{
    protected override bool Prerenders => true;

    [TestMethod]
    public async Task The_first_response_already_contains_the_matched_route()
    {
        var response = await GotoAsync("/items/42");

        var html = await response!.TextAsync();
        StringAssert.Contains(html, "id=\"page-item\"");
        StringAssert.Contains(html, "data-item-id=\"42\"");
    }

    [TestMethod]
    public async Task An_unmatched_deep_link_is_answered_with_the_not_found_status_and_content()
    {
        AllowConsoleError("404");

        var response = await GotoAsync("/nope/deeper");

        // Only .NET 10 gives a router a way to set the status (NavigationManager.NotFound).
        Assert.AreEqual(E2EEnvironment.FrameworkHasNotFound(Framework) ? 404 : 200, response!.Status);
        await Expect(Page.Locator("#not-found")).ToHaveAttributeAsync("data-path", "/nope/deeper");

        await WaitForInteractiveAsync();
        await Expect(Page.Locator("#not-found")).ToHaveAttributeAsync("data-path", "/nope/deeper");
    }
}

[TestClass]
public class ServerModeTests : PrerenderedWebModeTests
{
    protected override string Mode => "server";

    protected override IReadOnlyList<string> ExpectedRenderers => ["Server"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet"];
}

[TestClass]
public class WebAssemblyModeTests : PrerenderedWebModeTests
{
    protected override string Mode => "wasm";

    protected override IReadOnlyList<string> ExpectedRenderers => ["WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["browser"];
}

[TestClass]
public class AutoModeTests : PrerenderedWebModeTests
{
    protected override string Mode => "auto";

    // Auto picks per visit: Server until the WebAssembly runtime is available, which on a fast
    // connection can already be the first visit.
    protected override IReadOnlyList<string> ExpectedRenderers => ["Server", "WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet", "browser"];

    [TestMethod]
    public async Task A_later_visit_runs_on_WebAssembly_and_keeps_routing()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();

        // The first visit downloads the WebAssembly runtime in the background; a later visit in the
        // same browser finds it cached and starts on WebAssembly - where Brouter has to pick up
        // routing on a different runtime than the one that served the session so far.
        IPage? later = null;
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            later = await Context.NewPageAsync();
            await later.GotoAsync(BaseUrl + "/items/5", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await Expect(later.Locator("#status")).ToHaveAttributeAsync("data-interactive", "true", new() { Timeout = 90_000 });

            if (await later.Locator("#status").GetAttributeAsync("data-platform") == "browser") break;

            await later.CloseAsync();
            if (DateTime.UtcNow > deadline) Assert.Fail("Auto never switched a later visit to WebAssembly.");
            await Task.Delay(2000);
        }

        await Expect(later.Locator("#page-item")).ToHaveAttributeAsync("data-item-id", "5");
        await later.Locator("#nav-about").ClickAsync();
        await Expect(later.Locator("#page-about")).ToBeVisibleAsync();
        await Expect(later).ToHaveURLAsync(BaseUrl + "/about");
    }
}

[TestClass]
public class ServerNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "server-noprerender";

    protected override bool Prerenders => false;

    protected override IReadOnlyList<string> ExpectedRenderers => ["Server"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet"];
}

[TestClass]
public class WebAssemblyNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "wasm-noprerender";

    protected override bool Prerenders => false;

    protected override IReadOnlyList<string> ExpectedRenderers => ["WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["browser"];
}

[TestClass]
public class AutoNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "auto-noprerender";

    protected override bool Prerenders => false;

    protected override IReadOnlyList<string> ExpectedRenderers => ["Server", "WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet", "browser"];
}
