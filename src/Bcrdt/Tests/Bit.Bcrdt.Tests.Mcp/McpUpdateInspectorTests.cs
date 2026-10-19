using Bit.Bcrdt.Demo.Server.Dtos;

namespace Bit.Bcrdt.Tests.Mcp;

/// <summary>
/// InspectBcrdtUpdate on what crosses a transport or sits in a store: a whole document, one keystroke's delta,
/// every kind of protocol message, and the payloads a replica refuses - with the refusal the library itself gives.
/// </summary>
[TestClass]
public class McpUpdateInspectorTests
{
    [TestMethod]
    public async Task Without_a_payload_it_decodes_a_sample_update()
    {
        var report = await InspectAsync(null);

        Assert.IsTrue(report.Sample);
        Assert.IsTrue(report.Valid, report.Error);
        Assert.AreEqual("update", report.Kind, "The sample is an update, even though its first bytes also read as a message.");
        Assert.IsFalse(string.IsNullOrEmpty(report.Payload));
        Assert.AreEqual(report.TotalBytes, report.BlockBytes + report.DeleteSetBytes);

        var client = report.Clients!.Single();
        Assert.AreEqual("1", client.Client);
        Assert.AreEqual(0, client.FromClock);
        Assert.AreEqual(client.ToClock, report.StateVector!["1"]);

        // The map entry names its root and key. The text's first block was deleted and collected, so it is the one
        // that names the root, with its characters dropped; what survives is anchored to it.
        Assert.IsTrue(client.Blocks.Any(block => block.Content == "Any" && block.Parent == "root 'meta'" && block.Key == "title" && block.Preview == "[\"Q3 planning\"]"));
        Assert.IsTrue(client.Blocks.Any(block => block.Content == "Deleted" && block.Parent == "root 'notes'"));
        Assert.IsTrue(client.Blocks.Any(block => block.Content == "String" && block.Origin is not null && block.Preview!.Contains("budget", StringComparison.Ordinal)));
        Assert.IsNotEmpty(report.DeleteSet!, "The sample deletes part of its text.");
        Assert.IsTrue(report.Notes.Any(note => note.Contains("sample", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task One_keystroke_is_a_delta_anchored_to_what_came_before()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 42 });
        var text = document.GetText("notes");
        text.Insert(0, "hello");

        byte[]? keystroke = null;
        document.Updated += args => keystroke = args.Update;
        text.Insert(5, "!");

        var report = await InspectAsync(Convert.ToBase64String(keystroke!), "update");

        Assert.IsTrue(report.Valid, report.Error);

        var block = report.Clients!.Single().Blocks.Single();

        Assert.AreEqual(5, block.Clock);
        Assert.AreEqual(1, block.Length);
        Assert.AreEqual("42@4", block.Origin, "A keystroke is anchored to the character before it.");
        Assert.AreEqual("\"!\"", block.Preview);
        Assert.IsEmpty(report.StateVector!, "A delta from clock 5 implies no unbroken history.");
        Assert.IsTrue(report.Notes.Any(note => note.Contains("delta", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_protocol_message_is_read_through_its_envelope()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 9 });
        document.GetMap("meta").Set("title", "x");

        var step2 = await InspectAsync(Convert.ToBase64String(BcrdtSyncProtocol.EncodeSyncStep2(document.EncodeStateAsUpdate())));
        var step1 = await InspectAsync(Convert.ToBase64String(BcrdtSyncExchange.CreateAnnouncement(document)));
        var auth = await InspectAsync(Convert.ToBase64String(BcrdtSyncProtocol.EncodeAuth(false, "read-only document")));

        Assert.AreEqual("SyncStep2", step2.Kind);
        Assert.AreEqual(1, step2.Version);
        Assert.AreEqual("9", step2.Clients!.Single().Client);
        Assert.IsTrue(step2.Notes.Any(note => note.StartsWith("A SyncStep2", StringComparison.Ordinal)));

        Assert.AreEqual("SyncStep1", step1.Kind);
        Assert.AreEqual(1L, step1.StateVector!["9"]);

        Assert.AreEqual("Auth", auth.Kind);
        Assert.AreEqual(false, auth.Permitted);
        Assert.AreEqual("read-only document", auth.Reason);
    }

    [TestMethod]
    public async Task Presence_entries_are_reported_with_their_state()
    {
        using var awareness = new BcrdtAwarenessStore(5);
        awareness.SetLocalState(new Dictionary<string, object?> { ["user"] = new Dictionary<string, object?> { ["name"] = "Ada" } });

        var report = await InspectAsync(Convert.ToBase64String(BcrdtSyncProtocol.EncodeAwarenessUpdate(awareness.EncodeAll())));

        Assert.AreEqual("AwarenessUpdate", report.Kind);

        var entry = report.Awareness!.Single();
        Assert.AreEqual("5", entry.Client);
        Assert.AreEqual("Ada", entry.State!["user"]!["name"]!.GetValue<string>());
    }

    [TestMethod]
    public async Task A_malformed_update_reports_the_exception_a_replica_would_throw()
    {
        using var document = new BcrdtDocument();
        document.GetText("notes").Insert(0, "hello world");
        var update = document.EncodeStateAsUpdate();
        var truncated = update[..^3];

        string expected;
        try
        {
            using var replica = new BcrdtDocument();
            replica.ApplyUpdate(truncated);
            Assert.Fail("The library accepted a truncated update.");
            return;
        }
        catch (BcrdtFormatException ex)
        {
            expected = ex.Message;
        }

        var report = await InspectAsync(Convert.ToBase64String(truncated), "update");

        Assert.IsFalse(report.Valid);
        Assert.AreEqual(expected, report.Error);
        Assert.IsNull(report.Clients);
        Assert.IsTrue(report.Notes.Any(note => note.Contains("all-or-nothing", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_message_from_a_newer_protocol_is_refused_as_the_library_refuses_it()
    {
        var report = await InspectAsync("0x0200", "message");

        Assert.IsFalse(report.Valid);
        Assert.Contains("Protocol version 2 is not supported", report.Error!);
    }

    [TestMethod]
    public async Task Hex_and_url_safe_base64_are_read_too()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 3 });
        document.GetText("notes").Insert(0, "???>>>");
        var update = document.EncodeStateAsUpdate();

        var base64 = await InspectAsync(Convert.ToBase64String(update), "update");
        var hex = await InspectAsync($"0x{Convert.ToHexString(update)}", "update");
        var urlSafe = await InspectAsync(Convert.ToBase64String(update).TrimEnd('=').Replace('+', '-').Replace('/', '_'), "update");

        Assert.IsTrue(base64.Valid);
        Assert.AreEqual(base64.Payload, hex.Payload);
        Assert.AreEqual(base64.Payload, urlSafe.Payload);
    }

    [TestMethod]
    public async Task Text_that_is_not_bytes_is_explained()
    {
        var notBase64 = await InspectAsync("not base64!");
        var notHex = await InspectAsync("0xZZ");

        Assert.IsFalse(notBase64.Valid);
        Assert.Contains("not base64", notBase64.Error!);
        Assert.IsFalse(notHex.Valid);
        Assert.Contains("not hex", notHex.Error!);
    }

    [TestMethod]
    public async Task The_empty_update_is_named_as_such()
    {
        var report = await InspectAsync(Convert.ToBase64String(new BcrdtDocument().EncodeStateAsUpdate()));

        Assert.IsTrue(report.Valid);
        Assert.AreEqual("update", report.Kind);
        Assert.AreEqual(2, report.TotalBytes);
        Assert.IsTrue(report.Notes.Any(note => note.StartsWith("An empty update", StringComparison.Ordinal)));
    }

    [TestMethod]
    public async Task A_long_update_is_listed_up_to_the_limit_and_measured_whole()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 8 });
        var text = document.GetText("notes");

        // Typing at the start every time leaves one block per character: nothing to run-length encode.
        for (var i = 0; i < 300; i++) text.Insert(0, "x");

        var update = document.EncodeStateAsUpdate();
        var report = await InspectAsync(Convert.ToBase64String(update), "update");

        var client = report.Clients!.Single();

        Assert.AreEqual(true, report.BlocksTruncated);
        Assert.HasCount(200, client.Blocks);
        Assert.AreEqual(300, client.BlockCount);
        Assert.AreEqual(update.Length, report.BlockBytes + report.DeleteSetBytes);
    }

    [TestMethod]
    public async Task Bytes_the_writers_would_not_produce_are_pointed_out()
    {
        using var document = new BcrdtDocument();
        document.GetText("notes").Insert(0, "hello");
        byte[] update = [.. document.EncodeStateAsUpdate(), 0xAA, 0xBB];

        var report = await InspectAsync(Convert.ToBase64String(update), "update");

        Assert.IsTrue(report.Valid, "The decoder stops where the delete set ends and accepts trailing bytes.");
        Assert.IsTrue(report.Notes.Any(note => note.Contains("2 byte(s) the library's writers would not have produced", StringComparison.Ordinal)), string.Join(" | ", report.Notes));
    }

    internal static Task<BcrdtUpdateInspectionDto> InspectAsync(string? payload, string? readAs = null)
    {
        var arguments = new Dictionary<string, object?>();
        if (payload is not null) arguments["payload"] = payload;
        if (readAs is not null) arguments["readAs"] = readAs;

        return McpCall.StructuredAsync<BcrdtUpdateInspectionDto>("InspectBcrdtUpdate", arguments);
    }
}
