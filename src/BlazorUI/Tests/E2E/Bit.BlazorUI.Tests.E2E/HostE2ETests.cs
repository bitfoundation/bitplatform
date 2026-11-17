using System.Threading;

namespace Bit.BlazorUI.Tests.E2E;

/// <summary>
/// Pins the host itself: it answers, every page it lists becomes interactive, and a page's errors reach the test.
/// </summary>
[TestClass]
public partial class HostE2ETests : E2ETestBase
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
        // cut off. None of those is a page's own: a page whose own circuit has gone is what the circuit check is for, and
        // one whose own connection dropped - which raises the very same errors - the connection check.
        AllowConsoleErrors = AllowPageErrors = true;
        (int Console, int Page) atCommit = (0, 0);

        // The requests a navigation cut off: issued by the page on screen and failed once the navigation away from it had
        // started. WebKit reports the fetches among them in words it also uses for a real failure (see
        // IsCutOffByLeaving), so only a failure of one of these - read off the browser's own record of the request, never
        // off how the host's address happens to be spelled - is taken for one cut off. A navigation is counted as started
        // by the test, as it sets off (BeginLeavingAsync), as well as by the browser's request for the next page: WebKit
        // stops the loads of the page being left before it reports that request, so a failure it reports first would
        // otherwise be taken for one the page saw while still on screen.
        var navigations = 0;
        var issuedDuring = new Dictionary<IRequest, int>();
        var cutOff = new HashSet<string>(StringComparer.Ordinal);
        Page.Request += (_, request) =>
        {
            if (request.IsNavigationRequest && request.Frame == Page.MainFrame) Interlocked.Increment(ref navigations);
            else issuedDuring[request] = Volatile.Read(ref navigations);
        };
        Page.RequestFailed += (_, request) =>
        {
            if (issuedDuring.Remove(request, out var issued) && issued < Volatile.Read(ref navigations))
            {
                lock (cutOff) cutOff.Add(NormalizeUrl(request.Url));
            }
        };
        Page.RequestFinished += (_, request) => issuedDuring.Remove(request);
        Page.FrameNavigated += (_, frame) =>
        {
            if (frame.ParentFrame is null) atCommit = (ConsoleErrors.Count, PageErrors.Count);
        };

        var failures = new List<string>();
        // Where the errors already read end: what a page raises after its own check is read once it has been left.
        (int Console, int Page) checkedTo = (ConsoleErrors.Count, PageErrors.Count);

        // Moves on to the next navigation once every event the page raised so far is in (the answer to an evaluation comes
        // after them), so a request that failed while the page was still on screen is not taken for one cut off.
        async Task BeginLeavingAsync()
        {
            try
            {
                await Page.EvaluateAsync("() => 0");
            }
            catch (PlaywrightException) { }

            Interlocked.Increment(ref navigations);
        }

        // What the page just left raised from the end of its own check until the next page committed.
        string? CheckLeaving(string page)
        {
            var commit = atCommit;
            List<string> leaving;
            lock (cutOff)
            {
                leaving = ConsoleErrors.Skip(checkedTo.Console).Take(commit.Console - checkedTo.Console)
                                       .Concat(PageErrors.Skip(checkedTo.Page).Take(commit.Page - checkedTo.Page))
                                       .Where(error => IsTheConnectionGone(error) is false && IsCutOffByLeaving(error, cutOff) is false)
                                       .ToList();
            }

            checkedTo = (Math.Max(checkedTo.Console, commit.Console), Math.Max(checkedTo.Page, commit.Page));

            return leaving.Count > 0 ? $"{page}: raised an error while it was left ({leaving[0].Split('\n')[0]})" : null;
        }

        // Takes the page on screen to the route and reads what that page did itself: whether it became ready, whether its
        // circuit is alive, what it raised up to its check, and - read right before it is left - whether it lost its
        // connection at any point of its stay. What the page it left raised on the way out is that page's, handed back
        // apart.
        async Task<(List<string> Own, string? Leaving)> VisitAsync(string route, string left)
        {
            var own = new List<string>();

            await BeginLeavingAsync();

            try
            {
                await OpenAsync(route);
            }
            catch (Exception ex)
            {
                own.Add($"{route}: never became ready ({ex.Message.Split(Environment.NewLine)[0]})");
                return (own, CheckLeaving(left));
            }

            var leaving = CheckLeaving(left);

            if (await IsCircuitDeadAsync()) own.Add($"{route}: the circuit died");

            (int Console, int Page) now = (ConsoleErrors.Count, PageErrors.Count);
            var raised = ConsoleErrors.Skip(atCommit.Console).Take(now.Console - atCommit.Console)
                                      .Concat(PageErrors.Skip(atCommit.Page).Take(now.Page - atCommit.Page))
                                      .Where(error => IsTheConnectionGone(error) is false)
                                      .ToList();
            if (raised.Count > 0) own.Add($"{route}: raised an error ({raised[0].Split('\n')[0]})");

            checkedTo = now;

            if (await HasLostItsConnectionAsync()) own.Add($"{route}: lost its connection to the server");

            return (own, leaving);
        }

        var previous = "/";
        foreach (var route in routes)
        {
            var (own, leaving) = await VisitAsync(route, previous);
            if (leaving is not null) failures.Add(leaving);

            // A page whose own visit failed is visited once more, and only fails the test if it fails again: one page after
            // another for a quarter of an hour on a loaded runner, beside the test classes running with it, now and then a
            // navigation is aborted on the way or a circuit drops (Firefox), which says nothing about the page - and a page
            // that fails on its own does so on the second visit too. What the failed visit raised as it was left goes with
            // it; the second visit is left like any other.
            if (own.Count > 0)
            {
                (own, _) = await VisitAsync(route, route);
            }

            failures.AddRange(own);
            previous = route;
        }

        // The last page is left as well, so what it raises after its own check - and while it is left - is read like
        // every other page's.
        await BeginLeavingAsync();
        await OpenAsync("/");
        if (CheckLeaving(previous) is { } last) failures.Add(last);

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    // Whether the circuit of the page on screen has lost its connection at some point: Blazor adds its reconnect UI to
    // the page the first time the connection drops (blazor.web.js, DefaultReconnectDisplay.show) and only hides it once
    // the connection is back, so the element stays for as long as the page does. A page running in the browser has no
    // circuit to lose.
    private Task<bool> HasLostItsConnectionAsync()
    {
        return Page.EvaluateAsync<bool>("() => document.getElementById('components-reconnect-modal') !== null");
    }

    // A request of the page being left that the navigation cut off, which Firefox reports as failed with the status of a
    // load aborted (NS_BINDING_ABORTED, 0x804B0002) - a font it was still downloading, say - and WebKit as a fetch that
    // "cannot load ... due to access control checks" (the PDF viewer's sample it was still fetching). WebKit says the
    // same of a request a real CORS check refused, or one that failed for real (a refused connection, an aborted
    // response), so the WebKit one is only taken for one cut off when the request it names is one the browser saw fail
    // after the navigation away had started (cutOff).
    private static bool IsCutOffByLeaving(string error, IReadOnlySet<string> cutOff)
    {
        if (error.Contains("status=2152398850", StringComparison.Ordinal)) return true;

        var fetch = WebKitCutOffFetch().Match(error);

        return fetch.Success && cutOff.Contains(NormalizeUrl(fetch.Groups[1].Value));
    }

    // One spelling of an address, so the one in a console message and the one of a request compare equal: scheme and
    // host in lower case, the default port dropped.
    private static string NormalizeUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri.AbsoluteUri : url;
    }

    [System.Text.RegularExpressions.GeneratedRegex(@"Fetch API cannot load (\S+) due to access control checks")]
    private static partial System.Text.RegularExpressions.Regex WebKitCutOffFetch();
}
