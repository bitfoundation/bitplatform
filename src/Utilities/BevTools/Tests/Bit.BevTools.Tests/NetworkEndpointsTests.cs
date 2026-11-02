using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// Requests per endpoint: the ids in a path make every request look different, while what a developer asks is how one
/// endpoint behaves - how often it is called, how slow it is at the 95th percentile, and whether a list calls it once
/// per row.
/// </summary>
[TestClass]
public class NetworkEndpointsTests : BunitContext
{
    public NetworkEndpointsTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
            o.Ui.RefreshIntervalMs = 600_000;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    [DataRow("GET", "https://shop.test/api/orders/1001", "GET https://shop.test/api/orders/{id}")]
    [DataRow("GET", "/api/orders/1001/lines/7", "GET /api/orders/{id}/lines/{id}")]
    [DataRow("DELETE", "/api/carts/3f2504e0-4f89-11d3-9a0c-0305e82c3301", "DELETE /api/carts/{id}")]
    [DataRow("GET", "/api/files/9f86d081884c7d659a2feaa0c55ad015", "GET /api/files/{id}")]
    [DataRow("GET", "/api/orders/recent?page=2&size=20", "GET /api/orders/recent?page&size")]
    [DataRow("GET", "/api/v2/orders/cafe", "GET /api/v2/orders/cafe")]
    [DataRow("GET", "https://shop.test/", "GET https://shop.test/")]
    [DataRow("GET", "https://shop.test/api/orders/12#top", "GET https://shop.test/api/orders/{id}")]
    public void Ids_in_a_path_are_one_endpoint_and_words_are_not(string method, string url, string template)
    {
        Assert.AreEqual(template, BevHttpEndpoints.Template(method, url));
    }

    [TestMethod]
    public void An_endpoint_summary_counts_failures_apart_from_cancellations_and_times_only_completed_requests()
    {
        BevHttpRecord[] requests =
        [
            Request("/api/orders/1", 200, 10),
            Request("/api/orders/2", 200, 20),
            Request("/api/orders/3", 500, 30),
            Request("/api/orders/4", null, null),
            Request("/api/orders/5", null, 900, cancelled: true),
            Request("/api/products", 200, 5),
        ];

        var orders = BevHttpEndpoints.Summarize(requests).Single(e => e.Endpoint == "GET /api/orders/{id}");

        Assert.AreEqual(5, orders.Requests);
        Assert.AreEqual(5, orders.DistinctUrls);
        Assert.AreEqual(1, orders.Failed);
        Assert.AreEqual(1, orders.Cancelled);
        Assert.AreEqual(1, orders.Pending);
        Assert.AreEqual(20, orders.P50Ms, "the median of 10, 20, 30: pending and cancelled requests have no duration to count");
        Assert.AreEqual(30, orders.P95Ms);
        Assert.AreEqual(30, orders.MaxMs);
        Assert.IsNull(orders.ResponseBytes, "no response reported a size: unknown, not 0 B");
    }

    [TestMethod]
    public void A_request_per_row_is_reported_once_per_endpoint_and_not_as_a_burst()
    {
        var (session, scope) = TestHelpers.CreateSession(o => o.Diagnostics.HttpPerItemCount = 5);
        using (scope)
        {
            var start = DateTimeOffset.UtcNow.AddSeconds(-2);
            for (var i = 0; i < 25; i++)
            {
                session.Http.Add(Request("/api/orders/" + (1001 + i), 200, 50, start.AddMilliseconds(i * 4), session.Http.NextId()));
            }

            // The same order five times is a duplicate, not one request per item.
            for (var i = 0; i < 5; i++)
            {
                session.Http.Add(Request("/api/customers/7", 200, 5, start.AddMilliseconds(i * 10), session.Http.NextId()));
            }

            // Four ids are fewer than the threshold.
            for (var i = 0; i < 4; i++)
            {
                session.Http.Add(Request("/api/invoices/" + i, 200, 5, start.AddMilliseconds(i * 10), session.Http.NextId()));
            }

            session.Diagnostics.Evaluate(force: true);

            var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "http.per-item", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("GET /api/orders/{id}: one request per item (25 ids)", finding.Title);
            StringAssert.Contains(finding.Evidence, "25 requests for 25 different ids within 96 ms");
            StringAssert.Contains(finding.Evidence, "not inside a UI event");
            Assert.AreEqual(BevDiagnosticSeverity.Warning, finding.Severity);
            Assert.DoesNotContain(f => f.Diagnostic.RuleId == "http.burst", session.Diagnostics.Findings);
        }
    }

    [TestMethod]
    public void The_network_panel_groups_by_endpoint_drills_into_one_and_sorts_by_keyboard_operable_headers()
    {
        var panel = RenderPanel();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        for (var i = 0; i < 6; i++)
        {
            Session.Http.Add(Request("https://shop.test/api/orders/" + (1001 + i), 200, 10 * (i + 1), start.AddMilliseconds(i * 10), Session.Http.NextId()));
        }

        Session.Http.Add(Request("https://shop.test/api/products", 500, 3, start.AddMilliseconds(100), Session.Http.NextId()));
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Network", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Endpoints").Click();
        Assert.AreEqual("true", panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Endpoints").GetAttribute("aria-pressed"));
        var rows = panel.FindAll("tbody tr.bev-row");
        Assert.HasCount(2, rows);
        var orders = rows[0].QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToArray();
        Assert.AreEqual("GET /api/orders/{id}", orders[0], "the busiest first");
        Assert.AreEqual("6", orders[1]);
        Assert.AreEqual(BevFmt.Ms(30), orders[3], "p50 of 10…60 ms");
        Assert.AreEqual(BevFmt.Ms(60), orders[4]);
        Assert.Contains("bev-err", rows[1].QuerySelectorAll("td")[2].ClassName!, StringComparison.Ordinal);

        // Sorting is a button in the header: once by failures, largest first; again, smallest first.
        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Failed", StringComparison.Ordinal)).Click();
        Assert.StartsWith("GET /api/products", panel.FindAll("tbody tr.bev-row")[0].QuerySelector("td")!.TextContent.Trim());
        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Failed", StringComparison.Ordinal)).Click();
        Assert.AreEqual("ascending", panel.FindAll("thead th[aria-sort]").Single(h => h.TextContent.StartsWith("Failed", StringComparison.Ordinal)).GetAttribute("aria-sort"));
        Assert.StartsWith("GET /api/orders/{id}", panel.FindAll("tbody tr.bev-row")[0].QuerySelector("td")!.TextContent.Trim());

        // An endpoint opens its requests; the chip's ✕ shows every request again.
        panel.FindAll("tbody tr.bev-row button").First(b => b.TextContent.Contains("/api/orders/{id}", StringComparison.Ordinal)).Click();
        Assert.AreEqual("true", panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Requests").GetAttribute("aria-pressed"));
        Assert.Contains("GET https://shop.test/api/orders/{id}", panel.Find(".bev-chip.bev-on").TextContent, StringComparison.Ordinal);
        Assert.HasCount(6, panel.FindAll("tbody tr.bev-row"));

        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Duration", StringComparison.Ordinal)).Click();
        Assert.AreEqual(BevFmt.Ms(60), panel.FindAll("tbody tr.bev-row")[0].QuerySelectorAll("td")[4].TextContent.Trim());
        Assert.AreEqual("descending", panel.FindAll("thead th[aria-sort]").Single(h => h.TextContent.StartsWith("Duration", StringComparison.Ordinal)).GetAttribute("aria-sort"));

        panel.Find("button[aria-label='Show the requests to every endpoint']").Click();
        Assert.HasCount(7, panel.FindAll("tbody tr.bev-row"));
    }

    [TestMethod]
    public void Bytes_received_are_a_total_only_when_every_response_reported_its_size()
    {
        static BevHttpRecord Sized(string url, long? bytes, int? status = 200, string? error = null) => new()
        {
            Id = 1, StartedAt = DateTimeOffset.UtcNow, Method = "GET", Url = url, StatusCode = status, DurationMs = 5, ResponseBytes = bytes, Error = error,
        };

        Assert.AreEqual(300, BevHttpEndpoints.Summarize([Sized("/a/1", 100), Sized("/a/2", 200)]).Single().ResponseBytes);
        Assert.IsNull(BevHttpEndpoints.Summarize([Sized("/a/1", 0), Sized("/a/2", null)]).Single().ResponseBytes, "one chunked response: a sum would read as the whole");
        Assert.AreEqual(100, BevHttpEndpoints.Summarize([Sized("/a/1", 100), Sized("/a/2", null, status: null, error: "connection refused")]).Single().ResponseBytes, "no response, nothing received");
    }

    [TestMethod]
    public void A_lookup_typed_into_a_box_is_a_burst_and_rows_loading_theirs_are_per_item()
    {
        var (session, scope) = TestHelpers.CreateSession(o =>
        {
            o.Diagnostics.HttpPerItemCount = 5;
            o.Diagnostics.HttpBurstCount = 4;
        });
        using (scope)
        {
            var start = DateTimeOffset.UtcNow.AddSeconds(-3);
            var typed = new[] { "1", "12", "123", "1234", "12345" };
            for (var i = 0; i < typed.Length; i++)
            {
                session.Http.Add(new BevHttpRecord
                {
                    Id = session.Http.NextId(), StartedAt = start.AddMilliseconds(i * 150), Method = "GET", Url = "/api/orders/" + typed[i], StatusCode = 200, DurationMs = 40,
                    TriggerEventId = 100 + i, TriggerDescription = "oninput → OrderLookup.OnInput",
                });
            }

            for (var i = 0; i < 6; i++)
            {
                session.Http.Add(new BevHttpRecord
                {
                    Id = session.Http.NextId(), StartedAt = start.AddMilliseconds(i * 5), Method = "GET", Url = "/api/customers/" + (500 + i), StatusCode = 200, DurationMs = 40,
                    TriggerEventId = 900, TriggerDescription = "onclick → Customers.Load (lambda)",
                });
            }

            session.Diagnostics.Evaluate(force: true);

            var burst = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "http.burst", session.Diagnostics.Findings).Diagnostic;
            Assert.StartsWith("GET /api/orders/{id}: 5 requests", burst.Title);
            StringAssert.Contains(burst.Evidence, "for different ids, each from its own UI event");
            var perItem = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "http.per-item", session.Diagnostics.Findings).Diagnostic;
            Assert.AreEqual("GET /api/customers/{id}: one request per item (6 ids)", perItem.Title);
        }
    }

    [TestMethod]
    public void The_request_rules_cost_one_pass_over_a_storm()
    {
        var (session, scope) = TestHelpers.CreateSession();
        using (scope)
        {
            var start = DateTimeOffset.UtcNow.AddSeconds(-2);
            for (var i = 0; i < 250; i++)
            {
                session.Http.Add(Request("/api/orders/7", 200, 5, start.AddMilliseconds(i * 3), session.Http.NextId()));
                session.Http.Add(Request("/api/items/" + (i % 4), 200, 5, start.AddMilliseconds(i * 3), session.Http.NextId()));
            }

            session.Diagnostics.Evaluate(force: true);
            var started = System.Diagnostics.Stopwatch.GetTimestamp();
            session.Diagnostics.Evaluate(force: true);
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(started);

            // Before the sliding window this took 75-150 ms (Debug): every window start re-read every request after it.
            Console.WriteLine($"500 requests evaluated in {elapsed.TotalMilliseconds:0.00} ms");
            Assert.IsLessThan(40, elapsed.TotalMilliseconds, $"evaluating 500 requests took {elapsed.TotalMilliseconds:0.0} ms");
        }
    }

    [TestMethod]
    public void A_request_without_a_response_sorts_above_every_status_and_Clear_drops_the_endpoint_filter()
    {
        var panel = RenderPanel();
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        Session.Http.Add(Request("https://shop.test/api/orders/1", 500, 10, start, Session.Http.NextId()));
        Session.Http.Add(new BevHttpRecord { Id = Session.Http.NextId(), StartedAt = start.AddMilliseconds(5), Method = "GET", Url = "https://shop.test/api/orders/2", Error = "connection refused", DurationMs = 3 });
        Session.Http.Add(Request("https://shop.test/api/orders/3", 200, 10, start.AddMilliseconds(10), Session.Http.NextId()));
        panel.FindAll(".bev-tab").First(t => t.TextContent.Contains("Network", StringComparison.Ordinal)).Click();
        panel.Find("button[aria-label='Refresh captured data']").Click();

        panel.FindAll("thead th[aria-sort] button").Single(b => b.TextContent.StartsWith("Status", StringComparison.Ordinal)).Click();
        CollectionAssert.AreEqual(new[] { "ERR", "500", "200" }, panel.FindAll("tbody tr.bev-row").Select(r => r.QuerySelectorAll("td")[3].TextContent.Trim()).ToArray());

        panel.FindAll(".bev-segmented button").Single(b => b.TextContent == "Endpoints").Click();
        panel.FindAll("tbody tr.bev-row button").First().Click();
        Assert.IsNotNull(Session.Ui.Network.Endpoint);
        panel.FindAll("button").Single(b => b.TextContent == "Clear").Click();
        Assert.IsNull(Session.Ui.Network.Endpoint);
        Session.Http.Add(Request("https://shop.test/api/products", 200, 4, DateTimeOffset.UtcNow, Session.Http.NextId()));
        panel.Find("button[aria-label='Refresh captured data']").Click();
        Assert.HasCount(1, panel.FindAll("tbody tr.bev-row"), "new requests show, not \"No request matches\"");
    }

    [TestMethod]
    public void An_address_without_a_path_keeps_its_query_when_its_origin_is_stripped()
    {
        Assert.AreEqual("/?q", BevToolsHttpMessageHandler.StripOrigin("https://shop.test?q"));
        Assert.AreEqual("/", BevToolsHttpMessageHandler.StripOrigin("https://shop.test"));
        Assert.AreEqual("/api/x?y=1", BevToolsHttpMessageHandler.StripOrigin("https://shop.test/api/x?y=1"));
    }

    [TestMethod]
    public void Exports_carry_the_endpoints_and_the_report_lists_the_busiest()
    {
        var start = DateTimeOffset.UtcNow.AddSeconds(-5);
        for (var i = 0; i < 3; i++)
        {
            Session.Http.Add(Request("/api/orders/" + i, 200, 10 * (i + 1), start.AddMilliseconds(i), Session.Http.NextId()));
        }

        using var json = JsonDocument.Parse(BevSessionExporter.ToJson(Session, new BevSessionExportOptions { Sections = new HashSet<string> { "http" } }));
        var endpoint = Assert.ContainsSingle(json.RootElement.GetProperty("httpEndpoints").EnumerateArray().ToList());
        Assert.AreEqual("GET /api/orders/{id}", endpoint.GetProperty("endpoint").GetString());
        Assert.AreEqual(3, endpoint.GetProperty("requests").GetInt32());
        Assert.AreEqual(20, endpoint.GetProperty("p50Ms").GetDouble());

        var report = BevSessionExporter.ToMarkdownReport(Session);
        Assert.Contains("## Busiest endpoints", report, StringComparison.Ordinal);
        Assert.Contains("| `GET /api/orders/{id}` | 3 | 0 |", report, StringComparison.Ordinal);
    }

    private static BevHttpRecord Request(string url, int? status, double? durationMs, DateTimeOffset? at = null, long id = 1, bool cancelled = false) => new()
    {
        Id = id,
        StartedAt = at ?? DateTimeOffset.UtcNow,
        Method = "GET",
        Url = url,
        StatusCode = status,
        DurationMs = durationMs,
        IsCancelled = cancelled,
    };

    private IRenderedComponent<BevToolsPanel> RenderPanel()
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<string?>("loadPrefs", _ => true).SetResult(null);
        module.SetupVoid("savePrefs", _ => true).SetVoidResult();
        return Render<BevToolsPanel>();
    }
}
