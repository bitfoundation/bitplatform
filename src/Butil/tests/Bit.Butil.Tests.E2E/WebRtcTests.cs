using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Both peers live in the harness page and hand each other their ICE candidates directly, so the
/// whole handshake happens over loopback - no network, no signalling server, and nothing to prompt
/// for. Everything after the candidates is the same path a real connection takes.
/// </summary>
[TestClass]
public class WebRtcTests : ButilPageTest
{
    [TestMethod]
    public async Task Two_Peers_Connect_And_A_Message_Crosses_The_Data_Channel()
    {
        // connection state / channel state / what the other peer received / the remote channel's open
        await ClickAndExpectAsync("rtc-connect", "rtc:connect:connected/open/ping/remote-open", 30_000);
    }

    [TestMethod]
    public async Task A_Connected_Peer_Reports_Stats_Including_The_Candidate_Pair()
    {
        await ClickAndExpectAsync("rtc-connect", "rtc:connect:connected", 30_000);
        await ClickAndExpectAsync("rtc-stats", "rtc:stats:True/True");
    }

    [TestMethod]
    public async Task Closing_Tears_Both_Sides_Down()
    {
        await ClickAndExpectAsync("rtc-connect", "rtc:connect:connected", 30_000);
        await ClickAndExpectAsync("rtc-close", "rtc:closed");
    }
}
