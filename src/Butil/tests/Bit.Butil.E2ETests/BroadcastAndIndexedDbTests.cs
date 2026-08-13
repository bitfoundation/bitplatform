using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class BroadcastAndIndexedDbTests : ButilObserversPageTest
{
    [Test]
    public async Task BroadcastChannel_Subscriber_Receives_A_Posted_Message()
    {
        await ClickAndExpectAsync("broadcast-subscribe", "broadcast:subscribed");
        await ClickAndExpectAsync("broadcast-post", "broadcast:received:pong");
    }

    [Test]
    public async Task IndexedDb_Open_Put_Get_Roundtrips()
    {
        await ClickAndExpectAsync("idb-roundtrip", "idb:get:stored");
    }

    // One assertion covering ranges, cursors, index queries, batched transactions and binary
    // values; the harness reports the first failed check by name, so a red test says which.
    [Test]
    public async Task IndexedDb_Ranges_Cursors_And_Transactions_Behave()
    {
        await ClickAndExpectAsync("idb-advanced", "idb:adv:ok");
    }

    [Test]
    public async Task IndexedDb_Upgrade_Applies_Schema_Changes()
    {
        await ClickAndExpectAsync("idb-migrate", "idb:migrate:ok");
    }
}
