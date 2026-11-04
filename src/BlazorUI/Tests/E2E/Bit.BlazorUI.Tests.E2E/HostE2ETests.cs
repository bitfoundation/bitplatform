namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// Pins the host itself: it answers, every page it lists becomes interactive, and a page's errors reach the test.
/// </summary>
[TestClass]
public class HostE2ETests : E2ETestBase
{
    [TestMethod]
    public async Task TheHomePageBecomesInteractive()
    {
        await OpenAsync("/");

        await Expect(Page.Locator("h1")).ToHaveTextAsync("Bit.BlazorUI E2E Test Host");
        Assert.IsFalse(await IsCircuitDeadAsync());
    }

    [TestMethod]
    public async Task EveryListedPageBecomesInteractiveWithoutAnError()
    {
        await OpenAsync("/");

        var routes = await Page.Locator("#pages a").EvaluateAllAsync<string[]>("links => links.map(a => a.getAttribute('href'))");

        Assert.IsTrue(routes.Length > 0, "The host lists no E2E page.");

        // A few pages show images from made-up hosts (images.e2e.test, other.e2e.test), which their own tests answer
        // through Page.RouteAsync. Unanswered here, each would log a failed request - or not, depending on whether the
        // request fails before the page is left - so they are answered with an image of nothing.
        await Page.RouteAsync(new System.Text.RegularExpressions.Regex(@"^https?://[^/]+\.test(/|$)"), route => route.FulfillAsync(new()
        {
            ContentType = "image/svg+xml",
            Body = """<svg xmlns="http://www.w3.org/2000/svg" width="1" height="1"/>"""
        }));

        // Every page's errors are told apart here, so the run-wide check of the base is not the one to fail the test. What
        // a page raises before the next one commits is raised while it is left, rather than by the page being opened:
        // Firefox closes the connection of the page it leaves first and keeps running its scripts until the next page
        // commits, so every call to .NET a page makes in that time fails - an error that only says the connection is gone
        // (some of which Firefox reports only after the next page has committed), and the requests it has under way are
        // cut off. None of those is a page's own; a page whose own circuit has gone is what the circuit check is for.
        AllowConsoleErrors = AllowPageErrors = true;
        (int Console, int Page) atCommit = (0, 0);
        Page.FrameNavigated += (_, frame) =>
        {
            if (frame.ParentFrame is null) atCommit = (ConsoleErrors.Count, PageErrors.Count);
        };

        var failures = new List<string>();
        var previous = "/";

        foreach (var route in routes)
        {
            (int Console, int Page) before = (ConsoleErrors.Count, PageErrors.Count);

            try
            {
                await OpenAsync(route);
            }
            catch (Exception ex)
            {
                failures.Add($"{route}: never became ready ({ex.Message.Split(Environment.NewLine)[0]})");
                previous = route;
                continue;
            }

            var leaving = ConsoleErrors.Skip(before.Console).Take(atCommit.Console - before.Console)
                                       .Concat(PageErrors.Skip(before.Page).Take(atCommit.Page - before.Page))
                                       .Where(error => IsTheConnectionGone(error) is false && IsCutOffByLeaving(error) is false)
                                       .ToList();
            if (leaving.Count > 0) failures.Add($"{previous}: raised an error while it was left ({leaving[0].Split('\n')[0]})");

            if (await IsCircuitDeadAsync()) failures.Add($"{route}: the circuit died");

            var raised = ConsoleErrors.Skip(atCommit.Console).Concat(PageErrors.Skip(atCommit.Page))
                                      .Where(error => IsTheConnectionGone(error) is false)
                                      .ToList();
            if (raised.Count > 0) failures.Add($"{route}: raised an error ({raised[0].Split('\n')[0]})");

            previous = route;
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    // The errors Blazor and SignalR raise for a call made over a connection that has already been closed, or by a circuit
    // the page has already let go of (blazor.web.js, throwIfDispatchingWhenDisposed).
    private static bool IsTheConnectionGone(string error)
    {
        return error.Contains("Cannot send data if the connection is not in the 'Connected' State", StringComparison.Ordinal)
            || error.Contains("WebSocket is not in the OPEN state", StringComparison.Ordinal)
            || error.Contains("The circuit associated with this dispatcher is no longer available", StringComparison.Ordinal);
    }

    // A request of the page being left that the navigation cut off, which Firefox reports as failed with the status of a
    // load aborted (NS_BINDING_ABORTED, 0x804B0002) - a font it was still downloading, say.
    private static bool IsCutOffByLeaving(string error)
    {
        return error.Contains("status=2152398850", StringComparison.Ordinal);
    }
}
