using System.Text.Json;
using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.BevTools.Tests;

/// <summary>
/// The page's cookies, IndexedDB databases, caches, service workers and quota, read on request: structure and sizes,
/// never a cookie's value or a cached response.
/// </summary>
[TestClass]
public class AppStorageTests : BunitContext
{
    public AppStorageTests()
    {
        Services.AddBitBevToolsServices(o =>
        {
            o.Enabled = true;
            o.Ui.OpenByDefault = true;
        });
        JSInterop.Mode = JSRuntimeMode.Loose;
        this.UseRenderer("Server");
    }

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    private static BevAppStorage Sample() => new()
    {
        Cookies = [new BevCookieInfo { Name = ".AspNetCore.Culture", Size = 24 }, new BevCookieInfo { Name = "ai_session", Size = 60 }],
        IndexedDb = [new BevIndexedDbInfo { Name = "orders-cache", Version = 3 }],
        Caches = [new BevCacheInfo { Name = "bit-bswup-v2", Entries = 41 }],
        ServiceWorkers = [new BevServiceWorkerInfo { Scope = "https://shop.test/", Active = "https://shop.test/service-worker.js?v=1", ActiveState = "activated", Waiting = "https://shop.test/service-worker.js?v=2" }],
        Controller = "https://shop.test/service-worker.js?v=1",
        UsageBytes = 5 * 1024 * 1024,
        QuotaBytes = 100 * 1024 * 1024,
        UsageDetails = new() { ["caches"] = 4 * 1024 * 1024, ["indexedDB"] = 1024 * 1024 },
        Persisted = false,
        Errors = [],
    };

    private IRenderedComponent<BevToolsPanel> RenderPanel(BevAppStorage storage)
    {
        var module = JSInterop.SetupModule("./_content/Bit.BevTools/bit-bevtools.js");
        module.Setup<BevBrowserSnapshot>("attach", _ => true).SetResult(new BevBrowserSnapshot { Online = true });
        module.Setup<BevStorageEntry[]>("getStorage", _ => true).SetResult([]);
        module.Setup<BevAppStorage>("getAppStorage", _ => true).SetResult(storage);
        return Render<BevToolsPanel>();
    }

    [TestMethod]
    public void Reading_storage_shows_cookies_databases_caches_service_workers_and_the_quota()
    {
        var panel = RenderPanel(Sample());
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Browser", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-body button").First(b => b.TextContent.Contains("Read storage", StringComparison.Ordinal)).Click();

        var text = panel.Find(".bev-body").TextContent;
        StringAssert.Contains(text, "5 MB used of 100 MB (5 %)");
        StringAssert.Contains(text, "caches 4 MB");
        StringAssert.Contains(text, "best effort: the browser may clear it under storage pressure");
        StringAssert.Contains(text, "Cookies (2)");
        StringAssert.Contains(text, ".AspNetCore.Culture");
        StringAssert.Contains(text, "values are never read");
        StringAssert.Contains(text, "orders-cache");
        StringAssert.Contains(text, "bit-bswup-v2");
        StringAssert.Contains(text, "41 requests");
        StringAssert.Contains(text, "update waiting: https://shop.test/service-worker.js?v=2");
        StringAssert.Contains(text, "this page is controlled by https://shop.test/service-worker.js?v=1");
    }

    [TestMethod]
    public void A_waiting_service_worker_is_a_finding_and_exports_carry_the_structure()
    {
        var panel = RenderPanel(Sample());
        panel.FindAll(".bev-tab").First(t => t.TextContent.Trim().StartsWith("Browser", StringComparison.Ordinal)).Click();
        panel.FindAll(".bev-body button").First(b => b.TextContent.Contains("Read storage", StringComparison.Ordinal)).Click();

        Session.Diagnostics.Evaluate(force: true);
        var finding = Assert.ContainsSingle(f => f.Diagnostic.RuleId == "pwa.update-waiting", Session.Diagnostics.Findings).Diagnostic;
        Assert.AreEqual("An update is waiting for https://shop.test/", finding.Title);

        using var json = JsonDocument.Parse(BevSessionExporter.ToJson(Session));
        var storage = json.RootElement.GetProperty("browser").GetProperty("storage");
        Assert.AreEqual(".AspNetCore.Culture", storage.GetProperty("cookies")[0].GetProperty("name").GetString());
        Assert.IsFalse(storage.GetProperty("cookies")[0].TryGetProperty("value", out _));
        Assert.AreEqual(41, storage.GetProperty("caches")[0].GetProperty("entries").GetInt32());
        Assert.AreEqual("https://shop.test/service-worker.js?v=2", storage.GetProperty("serviceWorkers")[0].GetProperty("waiting").GetString());
    }

    [TestMethod]
    public void What_the_page_reported_is_bounded_and_redacted()
    {
        var token = "eyJhbGciOiJIUzI1NiJ9.eyJzdWIiOiIxMjMifQ.c2lnbmF0dXJlLXZhbHVl";
        var storage = BevToolsPanel.Clean(new BevAppStorage
        {
            Cookies = Enumerable.Range(0, 500).Select(i => new BevCookieInfo { Name = "c" + i, Size = -1 }).ToArray(),
            Caches = [new BevCacheInfo { Name = new string('x', 1000), Entries = -3 }],
            ServiceWorkers = [new BevServiceWorkerInfo { Scope = "https://shop.test/?token=" + token, Active = null }],
            Errors = [null!, "IndexedDB: timed out"],
        })!;

        Assert.HasCount(200, storage.Cookies);
        Assert.AreEqual(0, storage.Cookies[0].Size);
        Assert.AreEqual(200, storage.Caches![0].Name.Length);
        Assert.AreEqual(0, storage.Caches[0].Entries);
        Assert.DoesNotContain("eyJ", storage.ServiceWorkers![0].Scope, StringComparison.Ordinal);
        Assert.HasCount(2, storage.Errors);
        Assert.IsNull(BevToolsPanel.Clean(null));
    }
}
