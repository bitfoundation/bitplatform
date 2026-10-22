using System.Net;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Bit.Bcrdt.Demo.Server.Dtos;
using Bit.Bcrdt.Demo.Server.Services;

namespace Bit.Bcrdt.Tests.Mcp;

/// <summary>
/// The server's claims put to the things they describe: the merge lab against the engine it runs, the
/// update inspector against the encoder that made the bytes, the API index against the four assemblies,
/// the guide index against the README, and the HTTP mirror against the protocol it mirrors. No expected
/// answers are written down here - the library, its files and the protocol are the expectations.
/// </summary>
[TestClass]
public partial class McpOracleTests
{
    private static readonly string[] _words = ["alpha", "beta", "gamma", "delta", "x", "y", "z", "draft", "final"];

    [TestMethod]
    public async Task The_merge_lab_agrees_with_the_engine()
    {
        var random = new Random(20261019);

        for (var round = 0; round < 20; round++)
        {
            var replicas = random.Next(2, 4);
            var labels = Enumerable.Range(0, replicas).Select(i => ((char)('A' + i)).ToString()).ToArray();

            // A shared start the replicas can all safely edit, then a few edits each that are valid on their own.
            var lines = new List<string> { "base: text notes insert 0 \"start\"", "base: array todo push \"a\" \"b\"", "base: set tags add \"t\"", "base: map meta set owner \"base\"" };

            foreach (var label in labels)
            {
                for (var i = random.Next(1, 4); i > 0; i--)
                {
                    lines.Add(random.Next(5) switch
                    {
                        0 => $"{label}: map meta set {_words[random.Next(3)]} \"{_words[random.Next(_words.Length)]}\"",
                        1 => $"{label}: text notes insert {random.Next(0, 6)} \"{_words[random.Next(_words.Length)]}\"",
                        2 => $"{label}: array todo push {random.Next(100)}",
                        3 => $"{label}: set tags {(random.Next(2) == 0 ? "add" : "remove")} \"{_words[random.Next(3)]}\"",
                        _ => $"{label}: map meta delete {_words[random.Next(3)]}"
                    });
                }
            }

            // The replicas' lines interleaved arbitrarily, each replica's own staying in its order: which replica
            // a line belongs to is what the lab goes by, not where the line is.
            var queues = labels.Select(label => new Queue<string>(lines.Where(line => line.StartsWith($"{label}:", StringComparison.Ordinal)))).ToList();
            var interleaved = lines.Take(4).ToList();

            while (queues.Count > 0)
            {
                var queue = queues[random.Next(queues.Count)];
                interleaved.Add(queue.Dequeue());
                if (queue.Count == 0) queues.Remove(queue);
            }

            var operations = string.Join('\n', interleaved);
            var context = $"round {round}:\n{operations}";

            var report = await McpMergeLabTests.InspectAsync(operations);

            Assert.IsNull(report.Error, context);
            Assert.IsTrue(report.Converged, context);

            // The same thing, done directly with the engine: the same client ids, the same transactions, and the
            // roots asked for by kind, as an application asks for them.
            using var origin = Declare(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 1000 }));
            Run(origin, lines.Where(line => line.StartsWith("base:", StringComparison.Ordinal)));
            var start = origin.EncodeStateAsUpdate();

            // The lab numbers the replicas in the order the operations first name them; the engine is given the same ids.
            ulong ClientIdOf(string label) => report.Replicas!.Single(replica => replica.Label == label).ClientId;

            var documents = labels.Select(label =>
            {
                var document = Declare(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = ClientIdOf(label) }));
                document.ApplyUpdate(start);
                Run(document, lines.Where(line => line.StartsWith($"{label}:", StringComparison.Ordinal)));
                return document;
            }).ToArray();

            try
            {
                foreach (var (document, label) in documents.Zip(labels))
                {
                    Assert.AreEqual(Json(Project(document)), Json(report.Replicas!.Single(replica => replica.Label == label).Before), $"{context}\n{label} before the merge");
                }

                var full = documents.Select(document => document.EncodeStateAsUpdate()).ToArray();
                foreach (var document in documents) foreach (var update in full) document.ApplyUpdate(update);

                foreach (var document in documents)
                {
                    Assert.AreEqual(Json(Project(document)), Json(report.Merged), $"{context}\nafter the merge");
                }

                // Each update's size is the size of what that replica's transaction really produced.
                foreach (var label in labels)
                {
                    using var replica = Declare(new BcrdtDocument(new BcrdtDocumentOptions { ClientId = ClientIdOf(label) }));
                    replica.ApplyUpdate(start);

                    var sizes = new List<int>();
                    replica.Updated += args => sizes.Add(args.Update.Length);

                    foreach (var line in lines.Where(line => line.StartsWith($"{label}:", StringComparison.Ordinal)))
                    {
                        var before = sizes.Count;
                        Run(replica, [line]);
                        if (sizes.Count == before) sizes.Add(0);
                    }

                    CollectionAssert.AreEqual(sizes, report.Replicas!.Single(r => r.Label == label).Updates.Select(update => update.Bytes).ToArray(), $"{context}\n{label}'s update sizes");
                }
            }
            finally
            {
                foreach (var document in documents) document.Dispose();
            }
        }

        static void Run(BcrdtDocument document, IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                var words = line[(line.IndexOf(':') + 2)..].Split(' ', 4);
                var argument = words.Length > 3 ? words[3] : string.Empty;

                switch (words[0], words[2])
                {
                    case ("text", "insert"):
                        var (index, text) = (int.Parse(argument.Split(' ')[0]), JsonSerializer.Deserialize<string>(argument[(argument.IndexOf(' ') + 1)..])!);
                        document.GetText(words[1]).Insert(Math.Min(index, document.GetText(words[1]).Count), text);
                        break;
                    case ("array", "push"):
                        document.GetArray(words[1]).Push([.. argument.Split(' ').Select(value => JsonSerializer.Deserialize<JsonElement>(value)).Select(Value)]);
                        break;
                    case ("set", "add"):
                        document.GetSet(words[1]).Add(JsonSerializer.Deserialize<string>(argument));
                        break;
                    case ("set", "remove"):
                        document.GetSet(words[1]).Remove(JsonSerializer.Deserialize<string>(argument));
                        break;
                    case ("map", "set"):
                        document.GetMap(words[1]).Set(argument.Split(' ')[0], JsonSerializer.Deserialize<string>(argument[(argument.IndexOf(' ') + 1)..]));
                        break;
                    case ("map", "delete"):
                        document.GetMap(words[1]).Delete(argument);
                        break;
                }
            }
        }

        static object? Value(JsonElement element) => element.ValueKind == JsonValueKind.Number ? element.GetInt64() : element.GetString();

        static BcrdtDocument Declare(BcrdtDocument document)
        {
            document.GetText("notes");
            document.GetArray("todo");
            document.GetSet("tags");
            document.GetMap("meta");
            return document;
        }
    }

    [TestMethod]
    public async Task The_update_inspector_agrees_with_the_decoders()
    {
        var random = new Random(20261020);

        for (var round = 0; round < 15; round++)
        {
            // A few replicas edit a document and merge, so the update carries several clients, runs, deletions
            // and the occasional nested type.
            var documents = Enumerable.Range(0, random.Next(1, 4)).Select(_ => new BcrdtDocument()).ToArray();

            try
            {
                foreach (var document in documents)
                {
                    var text = document.GetText("notes");
                    for (var i = random.Next(1, 6); i > 0; i--) text.Insert(random.Next(text.Count + 1), _words[random.Next(_words.Length)]);
                    if (text.Count > 2 && random.Next(2) == 0) text.Delete(random.Next(text.Count - 1), 1);
                    if (random.Next(2) == 0) document.GetMap("meta").EnsureText("description").Insert(0, "nested");
                    document.GetArray("todo").Push(random.Next(1000), "item");
                }

                foreach (var document in documents) foreach (var other in documents) document.ApplyUpdate(other.EncodeStateAsUpdate());

                var update = documents[0].EncodeStateAsUpdate();
                var context = $"round {round}: {Convert.ToBase64String(update)}";

                var report = await McpUpdateInspectorTests.InspectAsync(Convert.ToBase64String(update), "update");

                Assert.IsTrue(report.Valid, context);
                Assert.AreEqual(update.Length, report.TotalBytes, context);
                Assert.AreEqual(update.Length, report.BlockBytes + report.DeleteSetBytes, $"{context}: the parts should re-encode to exactly the bytes the encoder wrote.");

                // The clients, their clock ranges and their blocks are the ones the library decodes.
                var blocks = BcrdtUpdateOperations.DecodeBlocks(update);

                Assert.HasCount(blocks.Count, report.Clients!, context);

                foreach (var client in report.Clients!)
                {
                    var structs = blocks[ulong.Parse(client.Client)];

                    Assert.AreEqual(structs.Count, client.BlockCount, context);
                    Assert.AreEqual(structs[0].Id.Clock, client.FromClock, context);
                    Assert.AreEqual(structs.Sum(block => (long)block.Length), client.ToClock - client.FromClock, context);
                    CollectionAssert.AreEqual(structs.Select(block => block.Id.Clock).ToArray(), client.Blocks.Select(block => block.Clock).ToArray(), context);
                }

                // A whole document's update implies its whole state vector.
                var vector = documents[0].GetStateVector();
                CollectionAssert.AreEquivalent(vector.Select(entry => $"{entry.Key}={entry.Value}").ToArray(), report.StateVector!.Select(entry => $"{entry.Key}={entry.Value}").ToArray(), context);

                var deleted = BcrdtUpdateOperations.DecodeDeleteSet(update);
                CollectionAssert.AreEquivalent(
                    deleted.Clients.SelectMany(client => client.Value.Select(range => $"{client.Key}:{range.Clock}+{range.Length}")).ToArray(),
                    report.DeleteSet!.Select(range => $"{range.Client}:{range.Clock}+{range.Length}").ToArray(), context);
            }
            finally
            {
                foreach (var document in documents) document.Dispose();
            }
        }
    }

    [TestMethod]
    public async Task The_update_inspector_reads_every_message_the_protocol_writes()
    {
        using var document = new BcrdtDocument(new BcrdtDocumentOptions { ClientId = 7 });
        document.GetText("notes").Insert(0, "hello");

        using var awareness = new BcrdtAwarenessStore(7);
        awareness.SetLocalState(new Dictionary<string, object?> { ["name"] = "Ada" });

        (byte[] Message, string Kind)[] messages =
        [
            (BcrdtSyncExchange.CreateAnnouncement(document), "SyncStep1"),
            (BcrdtSyncProtocol.EncodeSyncStep2(document.EncodeStateAsUpdate()), "SyncStep2"),
            (BcrdtSyncProtocol.EncodeUpdate(document.EncodeStateAsUpdate()), "Update"),
            (BcrdtSyncProtocol.EncodeSyncStep1Reply(document.GetStateVector(), BcrdtSyncExchange.ComputeDeletionDigest(document)), "SyncStep1Reply"),
            (BcrdtSyncProtocol.EncodeAwarenessUpdate(awareness.EncodeAll()), "AwarenessUpdate"),
            (BcrdtSyncProtocol.EncodeAwarenessQuery(), "AwarenessQuery"),
            (BcrdtSyncProtocol.EncodeAuth(false, "no access"), "Auth"),
            (BcrdtSyncProtocol.EncodePing(), "Ping"),
            (BcrdtSyncProtocol.EncodePong(), "Pong"),
        ];

        CollectionAssert.AreEquivalent(Enum.GetNames<BcrdtSyncMessageKind>(), messages.Select(message => message.Kind).ToArray(), "Every message kind should be read here.");

        foreach (var (message, kind) in messages)
        {
            var report = await McpUpdateInspectorTests.InspectAsync(Convert.ToBase64String(message));

            Assert.IsTrue(report.Valid, $"{kind}: {report.Error}");
            Assert.AreEqual(kind, report.Kind, $"Auto-detection read {kind} as {report.Kind}.");
            Assert.AreEqual(message.Length - 2, report.PayloadBytes, kind);
        }
    }

    [TestMethod]
    public void Every_exported_type_is_in_the_api_index()
    {
        var exported = BcrdtApiCatalog.Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsNested is false && BcrdtApiCatalog.IsRazorImports(type) is false)
            .Select(BcrdtApiCatalog.FriendlyName)
            .ToHashSet(StringComparer.Ordinal);

        var indexed = BcrdtApiCatalog.Types.Select(type => type.Name).ToHashSet(StringComparer.Ordinal);

        CollectionAssert.AreEquivalent(exported.ToArray(), indexed.ToArray());
        CollectionAssert.AreEquivalent(new[] { "Bit.Bcrdt", "Bit.Bcrdt.SignalR", "Bit.Bcrdt.Server", "Bit.Bcrdt.Testing" },
            BcrdtApiCatalog.Types.Select(type => type.Package).Distinct().ToArray());
    }

    [TestMethod]
    public void Every_public_member_of_the_engine_is_in_its_reference()
    {
        foreach (var type in new[] { typeof(BcrdtDocument), typeof(BcrdtCollaborativeDocument), typeof(BcrdtText), typeof(BcrdtMap), typeof(BcrdtArray), typeof(BcrdtSet), typeof(BcrdtUpdateOperations), typeof(BcrdtSyncProtocol) })
        {
            var reference = BcrdtApiCatalog.RenderType(type.Name)!;

            // Indexers are left out, as the reference leaves them: map["key"] and list[0] are Get and the list itself.
            var members = type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                .Where(member => member is PropertyInfo { } property && property.GetIndexParameters().Length == 0 || member is EventInfo or FieldInfo
                                 || member is MethodInfo { IsSpecialName: false } method && method.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute)) is false)
                .Where(member => member.DeclaringType != typeof(object))
                .Select(member => member.Name)
                .Distinct();

            foreach (var member in members) Assert.Contains($"**{member}", reference, $"{type.Name}.{member}");
        }
    }

    [TestMethod]
    public void The_xml_documentation_reaches_the_server()
    {
        // The XML documentation files have to reach the server's output folder for any of this to work;
        // a build that stops copying them leaves every summary empty without failing anything else. Every
        // public type of the four packages carries one, so a type that has none is a gap in what an agent
        // is told, and is named here.
        var withoutSummary = BcrdtApiCatalog.Types.Where(type => type.Summary is null).Select(type => $"{type.Package}: {type.Name}").ToArray();

        Assert.IsEmpty(withoutSummary, $"Public types without an XML summary: {string.Join(", ", withoutSummary)}.");
        Assert.IsGreaterThan(1000, BcrdtXmlDocs.Count);
    }

    [TestMethod]
    public void The_guide_sections_cover_every_heading_of_the_readme()
    {
        var readme = BcrdtSourceCatalog.Readme.Replace("\r\n", "\n", StringComparison.Ordinal);

        Assert.IsFalse(string.IsNullOrEmpty(readme), "The README is not embedded.");

        var fenced = false;
        var headings = new List<string>();

        foreach (var line in readme.Split('\n'))
        {
            if (line.TrimStart().StartsWith("```", StringComparison.Ordinal)) { fenced = fenced is false; continue; }
            if (fenced is false && HeadingRegex().Match(line) is { Success: true } match) headings.Add(match.Groups["text"].Value.Trim());
        }

        CollectionAssert.AreEqual(headings, BcrdtSourceCatalog.GuideSections.Select(section => section.Heading).ToArray());
        Assert.HasCount(BcrdtSourceCatalog.GuideSections.Length, BcrdtSourceCatalog.GuideSections.Select(section => section.Key).Distinct(StringComparer.Ordinal), "Every section needs a key of its own.");
    }

    [TestMethod]
    public void Every_demo_source_file_is_embedded()
    {
        // The pages, the shared collaboration code and the hosting are what the index promises; a new page
        // that the .csproj's patterns miss would silently be absent from every answer. The site chrome - the
        // shared Layout/ components and the DocsCatalog.cs they are driven by - is left out on purpose, with
        // the MCP and not-found pages.
        var client = FindDemoDirectory("Client");
        if (client is null) Assert.Inconclusive("The demo's sources are not next to this build.");

        var expected = Directory.EnumerateFiles(client, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".razor", StringComparison.Ordinal) || path.EndsWith(".cs", StringComparison.Ordinal))
            .Select(path => $"Demo/Client/{Path.GetRelativePath(client, path).Replace('\\', '/')}")
            .Where(path => path.Contains("/bin/", StringComparison.Ordinal) is false && path.Contains("/obj/", StringComparison.Ordinal) is false)
            .Where(path => path.StartsWith("Demo/Client/Layout/", StringComparison.Ordinal) is false)
            .Where(path => path is not "Demo/Client/Pages/McpServer.razor" and not "Demo/Client/Pages/NotFound.razor" and not "Demo/Client/DocsCatalog.cs")
            .ToArray();

        var embedded = BcrdtSourceCatalog.SourceFiles.Select(file => file.Path).Where(path => path.StartsWith("Demo/Client/", StringComparison.Ordinal)).ToArray();

        CollectionAssert.AreEquivalent(expected, embedded);
    }

    [TestMethod]
    public async Task Every_tool_is_reachable_as_a_plain_get_with_the_same_answer()
    {
        using var http = McpTestHost.CreateHttpClient();

        (string Tool, string Query, Dictionary<string, object?>? Arguments)[] calls =
        [
            ("SearchBcrdt", "?query=presence&limit=3", new() { ["query"] = "presence", ["limit"] = 3 }),
            ("GetBcrdtSetupGuide", "?hostingModel=server", new() { ["hostingModel"] = "server" }),
            ("GetBcrdtGuideSection", "?heading=Undo", new() { ["heading"] = "Undo" }),
            ("GetBcrdtApi", "?typeName=BcrdtText", new() { ["typeName"] = "BcrdtText" }),
            ("InspectBcrdtMerge", "", null),
            ("InspectBcrdtUpdate", "", null),
            ("GetBcrdtSourceFile", "?path=Demo/Client/Pages/Live.razor", new() { ["path"] = "Demo/Client/Pages/Live.razor" }),
        ];

        CollectionAssert.AreEquivalent(McpSurfaceTests.ExpectedTools, calls.Select(call => call.Tool).ToArray(),
            "The HTTP mirror is being exercised for a different set of tools than the server publishes.");

        foreach (var (tool, query, arguments) in calls)
        {
            using var response = await http.GetAsync($"/api/mcp/{tool}{query}");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, tool);

            var body = await response.Content.ReadAsStringAsync();

            if (tool is "SearchBcrdt" or "InspectBcrdtMerge" or "InspectBcrdtUpdate")
            {
                Assert.StartsWith("{", body, tool);
                continue;
            }

            Assert.AreEqual(await McpCall.TextAsync(tool, arguments), body, tool);
        }
    }

    [TestMethod]
    public async Task The_labs_answer_the_same_over_http_and_over_the_protocol()
    {
        using var http = McpTestHost.CreateHttpClient();
        var json = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        const string operations = "A: map meta set title \"x\"; B: map meta set title \"y\"";

        var mergeOverHttp = JsonSerializer.Deserialize<BcrdtMergeReportDto>(await http.GetStringAsync($"/api/mcp/InspectBcrdtMerge?operations={Uri.EscapeDataString(operations)}"), json)!;
        var mergeOverMcp = await McpMergeLabTests.InspectAsync(operations);

        Assert.AreEqual(Json(mergeOverMcp.Merged), Json(mergeOverHttp.Merged));
        CollectionAssert.AreEqual(mergeOverMcp.Notes, mergeOverHttp.Notes);

        var updateOverHttp = JsonSerializer.Deserialize<BcrdtUpdateInspectionDto>(await http.GetStringAsync("/api/mcp/InspectBcrdtUpdate"), json)!;
        var updateOverMcp = await McpUpdateInspectorTests.InspectAsync(null);

        Assert.AreEqual(updateOverMcp.Payload, updateOverHttp.Payload);
        CollectionAssert.AreEqual(updateOverMcp.Notes, updateOverHttp.Notes);
    }

    [TestMethod]
    public async Task The_demos_mcp_page_lists_this_server_and_its_links_answer()
    {
        // The page that tells people what this server offers is read from the repository, not
        // embedded: it is excluded from the source files on purpose. Out of the repository it has
        // nothing to be held to.
        var page = McpSurfaceTests.FindRepositoryFile("Demo", "Client", "Pages", "McpServer.razor");
        if (page is null) Assert.Inconclusive("The demo's McpServer.razor is not next to this build.");

        var markup = await File.ReadAllTextAsync(page);

        foreach (var tool in McpSurfaceTests.ExpectedTools) Assert.Contains($"<code>{tool}</code>", markup, tool);
        foreach (var prompt in new[] { "add-bcrdt-to-app", "implement-bcrdt-feature", "debug-bcrdt-sync" }) Assert.Contains(prompt, markup, prompt);
        foreach (var endpoint in new[] { "https://bcrdt.bitplatform.dev/mcp", "http://localhost:5560/mcp" }) Assert.Contains(endpoint, markup, endpoint);

        using var http = McpTestHost.CreateHttpClient();

        var links = PageLinkRegex().Matches(markup).Select(match => WebUtility.HtmlDecode(match.Groups["href"].Value)).Distinct().ToArray();

        Assert.IsNotEmpty(links);

        foreach (var link in links)
        {
            using var response = await http.GetAsync(link);

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, link);

            var body = await response.Content.ReadAsStringAsync();

            Assert.DoesNotContain("has no public type called", body, link);
            Assert.DoesNotContain("No source file at", body, link);
        }
    }

    [TestMethod]
    public void The_demos_navigation_links_the_mcp_page()
    {
        // The header's primary links and the docs nav panel are both built from the demo's catalog.
        var catalog = McpSurfaceTests.FindRepositoryFile("Demo", "Client", "DocsCatalog.cs");
        if (catalog is null) Assert.Inconclusive("The demo's DocsCatalog.cs is not next to this build.");

        var source = File.ReadAllText(catalog);

        Assert.Contains("new(\"/mcp-server\", \"MCP server\")", source, "the header does not link the MCP page");
        Assert.Contains("typeof(McpServer)", source, "the docs nav panel does not list the MCP page");
    }

    private static string? FindDemoDirectory(string name)
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            var candidate = Path.Combine(directory.FullName, "Demo", name);
            if (Directory.Exists(candidate)) return candidate;
        }

        return null;
    }

    /// <summary>The document as the lab projects it: what ToJson says, in JSON.</summary>
    private static JsonNode? Project(BcrdtDocument document) => JsonSerializer.SerializeToNode(document.ToJson());

    private static string Json(JsonNode? node) => node?.ToJsonString() ?? "null";

    [GeneratedRegex(@"^#{2,4} (?<text>.+)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"href=""(?<href>/api/mcp/[^""]+)""")]
    private static partial Regex PageLinkRegex();
}
