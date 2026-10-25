using System.Net;
using System.Text.RegularExpressions;
using ButilTests.Hosting.Infrastructure;
using Microsoft.Extensions.Logging;
using Modes = ButilTests.Harness.Web.HarnessRenderModes;

namespace ButilTests.Hosting;

/// <summary>
/// Every public member of every Butil service, called while a page renders statically (see PrerenderSweep in the
/// harness host). Bit.Butil promises that nothing throws and nothing waits during prerendering and static SSR -
/// reads answer with safe defaults, writes and subscriptions are inert - and that promise is kept one call site at
/// a time, so this checks all of them rather than a sample.
/// </summary>
[TestClass]
public class PrerenderSweepTests
{
    // The library has well over a hundred services; a sweep that found a handful has lost the scan, not the
    // services, and would otherwise pass by checking almost nothing.
    private const int MinimumServices = 100;
    private const int MinimumCompletedMembers = 1000;

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(Modes.Ssr)]
    [DataRow(Modes.Server)]
    [DataRow(Modes.WebAssembly)]
    public async Task Every_member_of_every_service_completes_during_a_static_render(string mode)
    {
        await using var factory = new HarnessHostFactory(mode);
        using var client = factory.CreateClient();
        using var response = await client.GetAsync("/prerender-sweep");

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        // Both of the runtimes a static render can hand out are ones Butil has to recognise as unusable: the
        // placeholder a host without the Server runtime registers, and a Server circuit's runtime that has not
        // been initialized yet - which is what a host that registers both hands to a static render.
        var runtime = RenderedHtml.TextOf(html, "sweep-runtime");
        Assert.IsTrue(runtime is "UnsupportedJavaScriptRuntime" or "RemoteJSRuntime", $"The static render ran with {runtime}.");

        var entries = Entries(html);
        foreach (var skipped in entries.Where(e => e.Outcome is "Skipped" or "Rejected"))
        {
            TestContext.WriteLine($"{skipped.Outcome}: {skipped.Service}.{skipped.Member} - {skipped.Detail}");
        }

        var broken = entries.Where(e => e.Outcome is "Failed" or "Hung").ToArray();
        Assert.AreEqual(0, broken.Length,
            $"{broken.Length} member(s) broke the prerendering contract on {runtime}:{Environment.NewLine}" +
            string.Join(Environment.NewLine, broken.Select(e => $"  {e.Outcome}: {e.Service}.{e.Member} - {e.Detail}")));

        Assert.IsGreaterThanOrEqualTo(MinimumServices, entries.Select(e => e.Service).Distinct().Count(), "The sweep found too few services.");
        Assert.IsGreaterThanOrEqualTo(MinimumCompletedMembers, entries.Count(e => e.Outcome is "Completed"), "The sweep completed too few members.");

        // What the calls handed back was disposed inside the sweep, and the services themselves when the request's
        // scope ended - after the response. Either failing is logged, not thrown, so the log is where it shows.
        var errors = factory.Logs.Entries.Where(e => e.Level >= LogLevel.Error).ToArray();
        Assert.AreEqual(0, errors.Length, string.Join(Environment.NewLine, errors.Select(e => e.ToString())));
    }

    private static List<(string Outcome, string Service, string Member, string Detail)> Entries(string html) =>
        [.. Regex.Matches(html, "<li data-outcome=\"([^\"]*)\" data-service=\"([^\"]*)\" data-member=\"([^\"]*)\">([^<]*)</li>")
            .Select(m => (m.Groups[1].Value, m.Groups[2].Value, WebUtility.HtmlDecode(m.Groups[3].Value), WebUtility.HtmlDecode(m.Groups[4].Value)))];
}
