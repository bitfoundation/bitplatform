using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class MessageChannelTests : ButilPageTest
{
    [TestMethod]
    public async Task Messages_Sent_Before_Start_Are_Queued_And_Arrive_In_Order()
    {
        await ClickAndExpectAsync("channel-queue", """channel:queue:{"queued":1}|{"queued":2}""");
    }

    [TestMethod]
    public async Task Bytes_Cross_A_Port_As_Binary()
    {
        await ClickAndExpectAsync("channel-binary", "channel:binary:True/[9,8,7]");
    }

    [TestMethod]
    public async Task A_Transferred_Port_Stops_Working_On_The_Sending_Side()
    {
        // Sent successfully, and then unusable here - which is the guarantee, not a bug.
        await ClickAndExpectAsync("channel-transfer", "channel:transfer:True/False");
    }
}
