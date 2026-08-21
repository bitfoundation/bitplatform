using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class BroadcastAndIndexedDbTests : ButilObserversPageTest
{
    [TestMethod]
    public async Task BroadcastChannel_Subscriber_Receives_A_Posted_Message()
    {
        await ClickAndExpectAsync("broadcast-subscribe", "broadcast:subscribed");
        await ClickAndExpectAsync("broadcast-post", "broadcast:received:pong");
    }

    [TestMethod]
    public async Task IndexedDb_Open_Put_Get_Roundtrips()
    {
        await ClickAndExpectAsync("idb-roundtrip", "idb:get:stored");
    }

    // One assertion covering ranges, cursors, index queries, batched transactions and binary
    // values; the harness reports the first failed check by name, so a red test says which.
    [TestMethod]
    public async Task IndexedDb_Ranges_Cursors_And_Transactions_Behave()
    {
        await ClickAndExpectAsync("idb-advanced", "idb:adv:ok");
    }

    [TestMethod]
    public async Task IndexedDb_Upgrade_Applies_Schema_Changes()
    {
        await ClickAndExpectAsync("idb-migrate", "idb:migrate:ok");
    }

    [TestMethod]
    public async Task CookieStore_Change_Event_Reports_A_Written_Cookie()
    {
        await ClickAndExpectAsync("cookiestore-change", "cookiestore:change:ok");
    }

    [TestMethod]
    public async Task Change_Subscriptions_Attach_And_Detach()
    {
        await ClickAndExpectAsync("subs-attach", "subs:ok");
    }

    [TestMethod]
    public async Task Platform_Extras_Report_Expected_Values()
    {
        await ClickAndExpectAsync("platform-extras", "extras:ok");
    }
}
