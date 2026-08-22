using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class CacheLocksAndPlatformTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task CacheStorage_PutText_Match_And_Delete_Roundtrips()
    {
        // Found=True, body="hello-cache", entry deleted=True.
        await ClickAndExpectAsync("cache-roundtrip", "cache:roundtrip:True/hello-cache/True");
    }

    [TestMethod]
    public async Task WebLocks_Run_Holds_The_Lock_While_Executing()
    {
        // The callback ran (True) and the lock manager reported the lock held (True) during it.
        await ClickAndExpectAsync("lock-run", "lock:run:True/True");
    }

    [TestMethod]
    public async Task ObjectUrls_Create_Returns_A_Blob_Url()
    {
        await ClickAndExpectAsync("objurl-create", "objurl:create:True");
    }

    [TestMethod]
    public async Task CookieStore_Set_Get_Delete_Roundtrips()
    {
        // CookieStore is Chromium-only; the E2E suite runs on Chromium so the value round-trips.
        await ClickAndExpectAsync("cookiestore-roundtrip", "cookiestore:get:csval");
    }

    [TestMethod]
    public async Task Performance_GetEntries_Reports_Then_ClearMarks_Empties()
    {
        // entries.Length>0 after the mark, and 0 after ClearMarks.
        await ClickAndExpectAsync("perf-entries", "perf:entries:True/True");
    }

    [TestMethod]
    public async Task Navigator_Reports_Language_Cores_And_Online()
    {
        await ClickAndExpectAsync("nav-info", "nav:info:True");
    }

    [TestMethod]
    public async Task UserAgent_Extract_Returns_The_Raw_User_Agent()
    {
        await ClickAndExpectAsync("ua-extract", "ua:extract:True");
    }

    [TestMethod]
    public async Task Screen_Reports_Positive_Metrics()
    {
        await ClickAndExpectAsync("screen-metrics", "screen:metrics:True");
    }
}
