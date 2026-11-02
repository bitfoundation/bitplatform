using Bit.Bcrdt.Tests.E2E.Infrastructure;
using Microsoft.Playwright;

namespace Bit.Bcrdt.Tests.E2E;

/// <summary>
/// The live page, the one that leaves the tab: two browser contexts stand for two people on two machines, each
/// with its own WebAssembly instance and its own replica, opening the same link. They meet through the demo
/// server's relay - the SignalR hub, or the WebSocket endpoint with <c>?relay=websocket</c> - behind which a
/// document host keeps one more replica of the document; each tab also keeps its copy in the browser's IndexedDB.
/// Each test uses a room of its own.
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

    [TestMethod]
    [DataRow(null)]
    [DataRow("websocket")]
    public async Task A_document_outlives_every_tab_that_had_it_open(string? relay)
    {
        var room = NewRoom();
        var author = await OpenTabAsync(room, relay);
        var witness = await OpenTabAsync(room, relay);
        await TypeAtEndAsync(Editor(author.Page), "Kept by the server.");

        // Every keystroke has been through the server: the relay hands the host each message before the room.
        await Assertions.Expect(Editor(witness.Page)).ToHaveValueAsync("Kept by the server.");

        // Both contexts - their browsers, IndexedDB and all - go away entirely.
        await author.Page.Context.CloseAsync();
        await witness.Page.Context.CloseAsync();

        // Somebody else, with nothing stored anywhere, opens the link to an empty room: the server's document
        // host hands them the document.
        var newcomer = await OpenTabAsync(room, relay);
        await Assertions.Expect(Editor(newcomer.Page)).ToHaveValueAsync("Kept by the server.");
    }

    [TestMethod]
    public async Task An_edit_made_offline_survives_a_reload_and_reaches_the_others_afterwards()
    {
        var room = NewRoom();
        var writer = await OpenTabAsync(room, relay: null);
        var reader = await OpenTabAsync(room, relay: null);

        await TypeAtEndAsync(Editor(writer.Page), "Online.");
        await Assertions.Expect(Editor(reader.Page)).ToHaveValueAsync("Online.");

        await GoOfflineAsync(writer.Page.Locator("section.panel"));
        int recordsBefore = await StoredRecordsAsync(writer.Page);
        await TypeAtEndAsync(Editor(writer.Page), " Offline.");

        // Nobody else has the offline sentence: it exists only in the writer's browser, once its writes have landed -
        // a few milliseconds after each keystroke, longer in WebKit, whose strictly durable writes wait for the disk.
        // A reload is only safe once the replica holds every keystroke and the store has stopped growing: the first
        // record to land may hold only the start of the sentence.
        await Assertions.Expect(writer.Page.Locator("section.panel .panel-foot .metric").First).ToHaveTextAsync($"{"Online. Offline.".Length} characters");
        await EventuallyAsync(async () => await StoredRecordsAsync(writer.Page) > recordsBefore, () => "the offline edit was stored in IndexedDB");
        await EventuallyAsync(async () =>
        {
            int records = await StoredRecordsAsync(writer.Page);
            await Task.Delay(500);
            return await StoredRecordsAsync(writer.Page) == records;
        }, () => "the offline edit's writes settled");
        await writer.Page.ReloadAsync();
        await WaitForInteractiveAsync(writer.Page);
        await WaitForReplicasAsync(writer.Page, 1);

        // Read back from IndexedDB, and - the page connects again on load - sent on to the others.
        await Assertions.Expect(Editor(writer.Page)).ToHaveValueAsync("Online. Offline.");
        await Assertions.Expect(Editor(reader.Page)).ToHaveValueAsync("Online. Offline.");
    }

    /// <summary>How many records the page's browser store holds, read straight from IndexedDB.</summary>
    private static Task<int> StoredRecordsAsync(IPage page) => page.EvaluateAsync<int>("""
        async () => {
            const db = await new Promise((resolve, reject) => {
                const request = indexedDB.open('bit-bcrdt-demo');
                request.onsuccess = () => resolve(request.result);
                request.onerror = () => reject(request.error);
            });
            try {
                if (!db.objectStoreNames.contains('updates')) return 0;
                return await new Promise(resolve => {
                    const count = db.transaction('updates').objectStore('updates').count();
                    count.onsuccess = () => resolve(count.result);
                });
            } finally {
                db.close();
            }
        }
        """);

    [TestMethod]
    [DataRow(null)]
    [DataRow("websocket")]
    public async Task A_read_only_tab_follows_the_document_but_cannot_change_it(string? relay)
    {
        var room = NewRoom();
        var writer = await OpenTabAsync(room, relay);
        var reader = await OpenTabAsync(room, relay, readOnly: true);

        // The relay granted the read-only link read access, and the editor turned read-only by itself.
        await Assertions.Expect(reader.Page.Locator(".live-readonly")).ToBeVisibleAsync();
        await Assertions.Expect(Editor(reader.Page)).Not.ToBeEditableAsync();
        await Assertions.Expect(writer.Page.Locator(".live-readonly")).ToHaveCountAsync(0);
        await Assertions.Expect(Editor(writer.Page)).ToBeEditableAsync();

        // It still receives every change, and still publishes its presence.
        await TypeAtEndAsync(Editor(writer.Page), "Written by the editor.");
        await Assertions.Expect(Editor(reader.Page)).ToHaveValueAsync("Written by the editor.");
        await Assertions.Expect(Presence(writer.Page)).ToContainTextAsync(await NameAsync(reader.Page));
    }

    /// <summary>A room no other test uses, in the alphabet the page accepts.</summary>
    private static string NewRoom() => "e2e-" + Guid.NewGuid().ToString("N")[..12];

    /// <summary>
    /// Opens the room in a browser context of its own - another person's browser - and waits until its replica is
    /// open and connected to the relay. Records the WebSockets the page opens, to tell which relay it used.
    /// </summary>
    private async Task<Tab> OpenTabAsync(string room, string? relay, bool readOnly = false)
    {
        var page = await NewPageAsync(await NewContextAsync());
        var sockets = new List<string>();
        page.WebSocket += (_, socket) => { lock (sockets) sockets.Add(socket.Url); };

        List<string> query = [];
        if (relay is not null) query.Add("relay=" + relay);
        if (readOnly) query.Add("access=read");
        await GotoAsync(page, $"/live/{room}" + (query.Count > 0 ? "?" + string.Join('&', query) : ""));
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
