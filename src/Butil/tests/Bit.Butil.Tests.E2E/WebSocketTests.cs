using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// Drives the harness against the echo endpoint <see cref="WebSocketEchoFixture"/> hosts for the
/// run - handed to the page through the query string, since the sample under test is a standalone
/// WebAssembly host with no server side of its own.
/// </summary>
[TestClass]
public class WebSocketTests : ButilHarnessTestBase
{
    protected override string HarnessRoute => $"/e2e?ws={Uri.EscapeDataString(WebSocketEchoFixture.Url)}";

    [TestMethod]
    public async Task Open_Negotiates_The_Offered_SubProtocol()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
    }

    [TestMethod]
    public async Task SendText_Round_Trips()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
        await ClickAndExpectAsync("ws-text", "ws:text:True/echo: ping");
    }

    [TestMethod]
    public async Task SendBytes_Round_Trips_As_Binary()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
        // The endpoint increments every byte, so this is a real round trip rather than the page
        // reporting the array it sent.
        await ClickAndExpectAsync("ws-binary", "ws:binary:True/[2,3,4,5]");
    }

    [TestMethod]
    public async Task An_Open_Socket_Reports_Its_State_Url_And_Buffer()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
        // Open, a url ending in /echo, nothing queued, and the negotiated protocol.
        await ClickAndExpectAsync("ws-state", "ws:state:Open/True/0/butil-echo");
    }

    [TestMethod]
    public async Task Close_Reports_The_Code_Back_Through_OnClose()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
        await ClickAndExpectAsync("ws-close", "ws:close:4000/True");
    }

    [TestMethod]
    public async Task A_Close_Started_By_The_Server_Carries_Its_Code_And_Reason()
    {
        await ClickAndExpectAsync("ws-open", "ws:open:butil-echo");
        await ClickAndExpectAsync("ws-server-close", "ws:server-close:4001/True/closed by the server");
    }

    [TestMethod]
    public async Task A_Socket_Closing_After_Its_Replacement_Opened_Leaves_The_Replacement_Usable()
    {
        // The first socket's close (4000) arrives while the second is already the current one, and
        // the second still round-trips afterwards.
        await ClickAndExpectAsync("ws-replace", "ws:replace:4000/True/echo: after");
    }

    [TestMethod]
    public async Task A_Url_With_A_Refused_Scheme_Returns_Null_Rather_Than_Throwing()
    {
        await ClickAndExpectAsync("ws-bad-url", "ws:bad-url:True");
    }
}
