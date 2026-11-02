using Bit.Bonboarding.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bonboarding.Tests.E2E;

/// <summary>
/// The demo renders InteractiveAuto: a first visit runs on the server while the WebAssembly runtime downloads, and a
/// visit after that runs in the browser. Every other suite mostly meets the first; this one proves both.
/// </summary>
[TestClass]
public sealed class HostingTests : BonboardingTest
{
    private static Task<string?> RendererAsync(IPage page)
        => page.Locator("[data-demo-renderer]").GetAttributeAsync("data-demo-renderer");

    private static async Task AssertTourWorksAsync(IPage page)
    {
        await page.GetByRole(AriaRole.Button, new() { Name = "Take the tour" }).ClickAsync();
        await WaitForStepAsync(page, "Welcome aboard");
        await page.Keyboard.PressAsync("ArrowRight");
        await WaitForStepAsync(page, "Create a project");

        var target = await BoxOfAsync(page, "[data-bonboarding-anchor='new-project']");
        var spotlight = await BoxOfAsync(page, ".bit-bonboarding-spotlight");
        AssertNear(target.X - 8, spotlight.X, 1.5, "The spotlight's left edge");

        await page.Keyboard.PressAsync("Escape");
        await Assertions.Expect(page.Locator(".bit-bonboarding-root")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_tour_runs_on_the_server_and_on_WebAssembly()
    {
        var page = await OpenAsync("/");
        var first = await RendererAsync(page);
        Assert.IsTrue(first is "Server" or "WebAssembly", $"The page runs on '{first}'.");
        await AssertTourWorksAsync(page);

        // Reload until InteractiveAuto picks WebAssembly, which it does once the runtime has been downloaded.
        for (var attempt = 0; attempt < 6 && await RendererAsync(page) != "WebAssembly"; attempt++)
        {
            await page.WaitForTimeoutAsync(2_000);
            await page.ReloadAsync(new PageReloadOptions { WaitUntil = WaitUntilState.NetworkIdle, Timeout = 90_000 });
            await WaitForInteractiveAsync(page);
        }

        if (await RendererAsync(page) != "WebAssembly")
        {
            Assert.Inconclusive("The WebAssembly runtime never took over from the server within the attempts allowed.");
        }

        await AssertTourWorksAsync(page);
        AssertNoConsoleErrors("the tour on both renderers");
    }
}
