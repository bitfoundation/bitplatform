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
}
