using System.Text.RegularExpressions;
using Bit.Blex.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Blex.Tests.E2E;

/// <summary>An interactive render mode of the web harness host, each test in a browser context of its own.</summary>
public abstract class WebModeTests : InteractiveHarnessTests
{
    private readonly WebSession _session = new();

    /// <summary>The BlexHarness:Mode the host is started with.</summary>
    protected abstract string Mode { get; }

    protected IBrowserContext Context => _session.Context;

    protected override string BaseUrl => _session.Host.BaseUrl;

    protected override string Framework => E2EEnvironment.Framework;

    protected override Task<IPage> OpenPageAsync() => _session.OpenAsync(Mode);

    protected override Task ClosePageAsync() => _session.CloseAsync();

    // A second page of the same browser context shares its origin storage, exactly like another tab.
    protected override Task<IPage> OpenSecondPageAsync() => Context.NewPageAsync();
}

/// <summary>What only a prerendered interactive mode has to get right: the first response, and the handover.</summary>
public abstract partial class PrerenderedWebModeTests : WebModeTests
{
    [TestMethod]
    public async Task The_first_response_already_contains_the_stores_state()
    {
        var response = await GotoAsync("/");

        var html = await response!.TextAsync();
        StringAssert.Contains(html, "id=\"count\" data-count=\"0\"");
        Assert.IsTrue(TokenRegex().IsMatch(html), "The prerendered page carries no token: the store's state never reached the first response.");
    }

    [TestMethod]
    public async Task State_produced_while_prerendering_is_handed_to_the_interactive_render()
    {
        var response = await GotoAsync("/");

        // The token is issued once, by whichever render initializes the page first and finds none. A
        // prerendered page issues it on the server; <BlexProvider> carries it across in the
        // PersistentComponentState, so the interactive render finds it set and keeps it. A new token after
        // hydration means the prerendered state was dropped and the page was initialized twice.
        var prerendered = TokenRegex().Match(await response!.TextAsync()).Groups["token"].Value;
        Assert.AreEqual(32, prerendered.Length, "The prerendered page carries no token.");

        await WaitForInteractiveAsync();
        await IncrementAsync(1);

        await Expect(Page.Locator("#token")).ToHaveAttributeAsync("data-token", prerendered);
    }

    [GeneratedRegex("id=\"token\" data-token=\"(?<token>[0-9a-f]{32})\"")]
    private static partial Regex TokenRegex();
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

    // Auto picks per visit: Server until the WebAssembly runtime is available, which on a fast connection
    // can already be the first visit.
    protected override IReadOnlyList<string> ExpectedRenderers => ["Server", "WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet", "browser"];

    [TestMethod]
    public async Task A_later_visit_on_WebAssembly_rehydrates_what_the_server_visit_persisted()
    {
        await GotoAsync("/");
        await WaitForInteractiveAsync();
        await IncrementAsync(2);

        // The first visit downloads the WebAssembly runtime in the background; a later visit in the same
        // browser finds it cached and starts on WebAssembly - a different runtime, with stores of its own,
        // which only get the count back from localStorage.
        IPage? later;
        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            later = await Context.NewPageAsync();
            await later.GotoAsync(BaseUrl + "/", new() { WaitUntil = WaitUntilState.DOMContentLoaded, Timeout = 90_000 });
            await WaitForInteractiveAsync(later);

            if (await later.Locator("#status").GetAttributeAsync("data-platform") == "browser") break;

            await later.CloseAsync();
            if (DateTime.UtcNow > deadline) Assert.Fail("Auto never switched a later visit to WebAssembly.");
            await Task.Delay(2000);
        }

        await ExpectCountAsync(later, 2);
    }
}

[TestClass]
public class ServerNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "server-noprerender";

    protected override IReadOnlyList<string> ExpectedRenderers => ["Server"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet"];
}

[TestClass]
public class WebAssemblyNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "wasm-noprerender";

    protected override IReadOnlyList<string> ExpectedRenderers => ["WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["browser"];
}

[TestClass]
public class AutoNoPrerenderModeTests : WebModeTests
{
    protected override string Mode => "auto-noprerender";

    protected override IReadOnlyList<string> ExpectedRenderers => ["Server", "WebAssembly"];

    protected override IReadOnlyList<string> ExpectedPlatforms => ["dotnet", "browser"];
}
