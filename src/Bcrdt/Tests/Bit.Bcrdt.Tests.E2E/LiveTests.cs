using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The live page, the one that leaves the tab: two browser contexts stand for two people on two machines, each
/// with its own WebAssembly instance and its own replica, opening the same link. They meet only through the demo
/// server's relay - the SignalR hub, or the WebSocket endpoint with <c>?relay=websocket</c> - which forwards their
/// messages and holds no copy of the document. Each test uses a room of its own.
/// </summary>
[TestClass]
public sealed class LiveTests : BcrdtTest
{
    [TestMethod]
    [DataRow(null, "/hub/bcrdt")]
    [DataRow("websocket", "/ws/bcrdt/live-{room}-ws")]
    public async Task Two_tabs_on_the_same_link_edit_together_and_see_each_other(string? relay, string socketPath)
    {
        var room = NewRoom();
        var first = await OpenTabAsync(room, relay);
        var second = await OpenTabAsync(room, relay);

        // Both tabs went through the relay the link names: the page's own socket to the demo server.
        foreach (var tab in new[] { first, second })
        {
            string[] sockets;
            lock (tab.Sockets) sockets = [.. tab.Sockets];
            var expected = socketPath.Replace("{room}", room, StringComparison.Ordinal);
            Assert.IsTrue(sockets.Any(url => new Uri(url).AbsolutePath.StartsWith(expected, StringComparison.Ordinal)),
                $"the tab opened no WebSocket to {expected}; it opened {Quote(sockets)}");
        }

        // Each tab lists the other in its presence list, by the name the other tab gave itself.
        await Assertions.Expect(Presence(first.Page)).ToContainTextAsync(await NameAsync(second.Page));
        await Assertions.Expect(Presence(second.Page)).ToContainTextAsync(await NameAsync(first.Page));
        await Assertions.Expect(first.Page.Locator(".panel-foot")).ToContainTextAsync("1 other tab on this document");

        await TypeAtEndAsync(Editor(first.Page), "Typed in the first tab.");
        await Assertions.Expect(Editor(second.Page)).ToHaveValueAsync("Typed in the first tab.");

        await TypeAtEndAsync(Editor(second.Page), " Answered in the second.");
        await Assertions.Expect(Editor(first.Page)).ToHaveValueAsync("Typed in the first tab. Answered in the second.");
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("websocket")]
    public async Task A_tab_taken_offline_converges_with_the_other_when_it_comes_back(string? relay)
    {
        var room = NewRoom();
        var first = await OpenTabAsync(room, relay);
        var second = await OpenTabAsync(room, relay);
        ILocator[] editors = [Editor(first.Page), Editor(second.Page)];

        await TypeAtEndAsync(Editor(first.Page), "Shared start.");
        await ExpectConvergedAsync(editors, "Shared start.");

        await GoOfflineAsync(first.Page.Locator("section.panel"));

        // Disconnecting withdraws the tab's presence, so the other tab sees it leave at once.
        await Assertions.Expect(second.Page.Locator(".panel-foot")).ToContainTextAsync("0 other tabs on this document");

        await TypeAtEndAsync(Editor(first.Page), " Written offline.");
        await TypeAtEndAsync(Editor(second.Page), " Written online.");
        await Assertions.Expect(Editor(first.Page)).ToHaveValueAsync("Shared start. Written offline.");
        await Assertions.Expect(Editor(second.Page)).ToHaveValueAsync("Shared start. Written online.");

        await GoOnlineAsync(first.Page.Locator("section.panel"));

        // The handshake runs in both directions: both paragraphs survive, in the same order in both tabs.
        var merged = await ExpectConvergedAsync(editors, "Shared start.", " Written offline.", " Written online.");
        Assert.AreEqual("Shared start. Written offline. Written online.".Length, merged.Length, $"characters were lost or duplicated: \"{merged}\"");
        await Assertions.Expect(Presence(second.Page)).ToContainTextAsync(await NameAsync(first.Page));
    }

    /// <summary>A room no other test uses, in the alphabet the page accepts.</summary>
    private static string NewRoom() => "e2e-" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Opens the room in a browser context of its own - another person's browser - and waits until its replica is
    /// open and connected to the relay. Records the WebSockets the page opens, to tell which relay it used.
    /// </summary>
    private async Task<Tab> OpenTabAsync(string room, string? relay)
    {
        var page = await NewPageAsync(await NewContextAsync());
        var sockets = new List<string>();
        page.WebSocket += (_, socket) => { lock (sockets) sockets.Add(socket.Url); };

        await GotoAsync(page, $"/live/{room}" + (relay is null ? "" : "?relay=" + relay));
        await WaitForInteractiveAsync(page);
        await WaitForReplicasAsync(page, 1);
        await Assertions.Expect(page.Locator(".live-relay-active")).ToHaveTextAsync(relay is null ? "SignalR hub" : "WebSocket endpoint");

        return new Tab(page, sockets);
    }

    private static ILocator Editor(IPage page) => page.Locator("section.panel textarea");

    private static ILocator Presence(IPage page) => page.Locator("section.panel .presence-list");

    /// <summary>The guest name the tab gave itself, from its "You are Guest 123" header.</summary>
    private static async Task<string> NameAsync(IPage page) =>
        (await page.Locator("section.panel .panel-name").InnerTextAsync()).Replace("You are", "", StringComparison.Ordinal).Trim();

    private sealed record Tab(IPage Page, List<string> Sockets);
}
