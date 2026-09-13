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

    /// <summary>
    /// The connection callbacks, which JavaScript dispatches through the interop reference the handle hands
    /// it. That reference is held by a small relay object rather than by the handle itself - which is what
    /// keeps a trimmed app from downloading every IndexedDB module for one store read - so it is worth
    /// proving in a browser that the events still arrive.
    /// </summary>
    /// <remarks>
    /// The three values are: the open connection was told about the version change, the waiting open was
    /// <em>not</em> reported blocked, and it got its upgrade. The middle one is the point: Butil's
    /// connection closes itself as it reports the versionchange, so the other one is never held up.
    /// </remarks>
    [TestMethod]
    public async Task IndexedDb_Connection_Callbacks_Report_VersionChange_And_Yield_The_Database()
    {
        await ClickAndExpectAsync("idb-connection-callbacks", "idb:connection-callbacks:True/False/True");
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
