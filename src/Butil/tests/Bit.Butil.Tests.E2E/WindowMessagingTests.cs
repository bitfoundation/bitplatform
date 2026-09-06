using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Drives the harness against the document in
/// <c>Bit.Butil.Samples.Web/wwwroot/frames/e2e-frame.html</c> - cross-document messaging needs a
/// second document, so the harness has to have one.
/// </summary>
[TestClass]
public class WindowMessagingTests : ButilPageTest
{
    [TestMethod]
    public async Task A_Message_Round_Trips_Through_An_Iframe_With_The_Senders_Origin()
    {
        await ClickAndExpectAsync("wm-frame-post", """wm:frame:True/{"op":"echo","payload":{"text":"ping"}}""");
    }

    [TestMethod]
    public async Task Bytes_Reach_The_Frame_As_Binary()
    {
        // The frame sums them, so the answer is derived from the payload rather than echoed.
        await ClickAndExpectAsync("wm-frame-bytes", """wm:frame-bytes:{"op":"sum","length":3,"sum":60}""");
    }

    [TestMethod]
    public async Task A_Port_Can_Be_Transferred_To_The_Frame_And_Answered_On()
    {
        await ClickAndExpectAsync("wm-frame-port", """wm:frame-port:True/{"op":"fromFrame","echoOf":{"over":"the port"}}""");
    }

    [TestMethod]
    public async Task A_Top_Level_Page_Is_Its_Own_Parent()
    {
        await ClickAndExpectAsync("wm-self", """wm:self:True/{"up":"parent"}""");
    }

    [TestMethod]
    public async Task A_Message_From_An_Origin_Not_On_The_List_Never_Reaches_The_Callback()
    {
        // The same self-post as above, with a listener that trusts a different origin.
        await ClickAndExpectAsync("wm-origin-filter", "wm:origin-filter:True");
    }

    [TestMethod]
    public async Task A_Transferred_Port_Goes_To_One_Listener_Only()
    {
        // Two listeners accept the message; the first one registered gets the port and the second
        // gets none, so there is never more than one handle over a port. The echo proves the one
        // that was handed over is live.
        await ClickAndExpectAsync("wm-port-owner", """wm:port-owner:1/0/{"over":"the port"}""");
    }
}
