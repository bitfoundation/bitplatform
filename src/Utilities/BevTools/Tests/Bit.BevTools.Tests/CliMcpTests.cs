using System.IO.Pipelines;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Bit.BevTools.Tests;

/// <summary>
/// <c>bevtools mcp</c> driven by a real MCP client over an in-memory stream pair: the handshake, the tools' schemas and
/// annotations, and what each tool answers from real exports - bounded, and only what the files hold.
/// </summary>
[TestClass]
public class CliMcpTests : BunitContext
{
    private static readonly string[] Tools =
    [
        "list_sessions", "get_findings", "get_errors", "get_error_groups", "get_slowest_components", "get_component",
        "get_http", "get_http_endpoints", "get_timeline", "get_logs", "check", "compare",
    ];

    public CliMcpTests() => Services.AddBitBevToolsServices(o =>
    {
        o.Enabled = true;
        o.Extensions.Add(new CliPlantedFindings());
    });

    private BevToolsSession Session => Services.GetRequiredService<BevToolsSession>();

    [TestMethod]
    public async Task An_agent_lists_the_exports_and_follows_a_finding_to_its_component_and_errors()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-mcp-");
        try
        {
            var (baseline, current) = await RecordAsync(folder.FullName);
            var parent = Session.Components.All.Single(c => c.DisplayName == nameof(ParentComponent)).InstanceId;
            await using var served = await Served.StartAsync(baseline, current);

            Assert.Contains("never connects to a running app", served.Client.ServerInstructions!);
            Assert.Contains("redacted at capture", served.Client.ServerInstructions);
            var tools = await served.Client.ListToolsAsync(cancellationToken: TestContext.CancellationToken);
            Assert.AreSequenceEqual(Tools, tools.Select(t => t.Name).ToArray(), global::Microsoft.VisualStudio.TestTools.UnitTesting.SequenceOrder.InAnyOrder);
            foreach (var tool in tools)
            {
                var annotations = tool.ProtocolTool.Annotations!;
                Assert.IsTrue(annotations.ReadOnlyHint, tool.Name);
                Assert.IsFalse(annotations.DestructiveHint, tool.Name);
                Assert.IsFalse(annotations.OpenWorldHint, tool.Name);
                Assert.IsFalse(string.IsNullOrWhiteSpace(tool.Description), tool.Name);
            }

            using (var sessions = await served.CallAsync("list_sessions"))
            {
                var list = sessions.RootElement.GetProperty("sessions").EnumerateArray().ToList();
                Assert.HasCount(2, list);
                Assert.IsFalse(sessions.RootElement.GetProperty("truncated").GetBoolean());
                Assert.AreEqual(0, list[0].GetProperty("index").GetInt32());
                Assert.AreEqual(baseline, list[0].GetProperty("file").GetString());
                Assert.AreEqual(BevSessionExporter.Format, list[1].GetProperty("format").GetString());
                var sections = list[0].GetProperty("sections");
                Assert.AreEqual(JsonDocument.Parse(File.ReadAllText(baseline)).RootElement.GetProperty("timeline").GetArrayLength(), sections.GetProperty("timeline").GetInt32());
                Assert.IsGreaterThanOrEqualTo(2, sections.GetProperty("components").GetInt32());
                Assert.Contains(s => s.GetString() == "overhead", list[0].GetProperty("otherSections").EnumerateArray());
            }

            var (unnamedError, unnamed) = await served.CallTextAsync("get_findings");
            Assert.IsTrue(unnamedError, "two exports are loaded and none was named");
            Assert.Contains("2 exports are loaded", unnamed);

            using (var findings = await served.CallAsync("get_findings", new() { ["session"] = "0" }))
            {
                var items = findings.RootElement.GetProperty("findings").EnumerateArray().ToList();
                Assert.AreEqual(items.Count, findings.RootElement.GetProperty("total").GetInt32());
                Assert.IsFalse(findings.RootElement.GetProperty("truncated").GetBoolean());
                Assert.DoesNotContain(f => f.GetProperty("ruleId").GetString() == "cli.dismissed", items, "dismissed findings only when asked");
                var error = Assert.ContainsSingle(f => f.GetProperty("ruleId").GetString() == "cli.error", items);
                Assert.AreEqual("Total -5 for order 7", error.GetProperty("evidence").GetString());
                Assert.AreEqual(parent, error.GetProperty("componentInstanceId").GetInt64());
                Assert.AreEqual("Error", items[0].GetProperty("severity").GetString(), "most severe first");
            }

            using (var all = await served.CallAsync("get_findings", new() { ["session"] = "current.json", ["includeDismissed"] = true, ["minSeverity"] = "error" }))
            {
                Assert.AreEqual(1, all.RootElement.GetProperty("session").GetInt32(), "named by its file name");
                var items = all.RootElement.GetProperty("findings").EnumerateArray().ToList();
                Assert.IsTrue(items.All(f => f.GetProperty("severity").GetString() == "Error"));
                Assert.IsTrue(Assert.ContainsSingle(f => f.GetProperty("ruleId").GetString() == "cli.dismissed", items).GetProperty("dismissed").GetBoolean());
            }

            using (var hints = await served.CallAsync("get_findings", new() { ["session"] = "0", ["ruleId"] = "cli.hint" }))
            {
                Assert.AreEqual("cli.hint", Assert.ContainsSingle(hints.RootElement.GetProperty("findings").EnumerateArray()).GetProperty("ruleId").GetString());
            }

            Assert.IsTrue((await served.CallTextAsync("get_findings", new() { ["session"] = "0", ["minSeverity"] = "loud" })).IsError);
            Assert.IsTrue((await served.CallTextAsync("get_findings", new() { ["session"] = "7" })).IsError);

            using (var component = await served.CallAsync("get_component", new() { ["session"] = "0", ["instanceId"] = parent }))
            {
                Assert.AreEqual(nameof(ParentComponent), component.RootElement.GetProperty("component").GetProperty("displayName").GetString());
                Assert.Contains(c => c.GetProperty("displayName").GetString() == nameof(ChildComponent), component.RootElement.GetProperty("children").EnumerateArray());
                Assert.Contains(f => f.GetProperty("ruleId").GetString() == "cli.error", component.RootElement.GetProperty("findings").EnumerateArray());
            }

            var (missingError, missing) = await served.CallTextAsync("get_component", new() { ["session"] = "0", ["instanceId"] = 987654321 });
            Assert.IsTrue(missingError);
            Assert.Contains("No component #987654321", missing);

            using (var slowest = await served.CallAsync("get_slowest_components", new() { ["session"] = "1", ["by"] = "renders", ["limit"] = 1 }))
            {
                Assert.AreEqual("renders", slowest.RootElement.GetProperty("rankedBy").GetString());
                var top = Assert.ContainsSingle(slowest.RootElement.GetProperty("components").EnumerateArray());
                Assert.AreEqual(Session.Components.All.Where(c => !c.IsBevTools).Max(c => c.RenderCount), top.GetProperty("renders").GetInt32());
                Assert.IsTrue(slowest.RootElement.GetProperty("truncated").GetBoolean());
            }

            using (var errors = await served.CallAsync("get_errors", new() { ["session"] = "0", ["text"] = "7001" }))
            {
                Assert.AreEqual(1, errors.RootElement.GetProperty("total").GetInt32());
                Assert.Contains("Order 7001", Assert.ContainsSingle(errors.RootElement.GetProperty("errors").EnumerateArray()).GetProperty("message").GetString()!);
            }

            using (var groups = await served.CallAsync("get_error_groups", new() { ["session"] = "0" }))
            {
                var group = Assert.ContainsSingle(g => g.GetProperty("exceptionType").GetString()!.EndsWith(nameof(InvalidOperationException), StringComparison.Ordinal), groups.RootElement.GetProperty("errorGroups").EnumerateArray());
                Assert.AreEqual(2, group.GetProperty("occurrences").GetInt32(), "one problem, two messages");
                Assert.AreEqual(2, group.GetProperty("distinctMessages").GetInt32());
            }

            using (var failed = await served.CallAsync("get_http", new() { ["session"] = "0", ["failed"] = true }))
            {
                var request = Assert.ContainsSingle(failed.RootElement.GetProperty("requests").EnumerateArray());
                Assert.AreEqual(500, request.GetProperty("statusCode").GetInt32());
                Assert.Contains("/api/orders/7", request.GetProperty("url").GetString()!);
            }

            using (var byEndpoint = await served.CallAsync("get_http", new() { ["session"] = "0", ["endpoint"] = "/API/CUSTOMERS" }))
            {
                Assert.AreEqual(1, byEndpoint.RootElement.GetProperty("total").GetInt32(), "the endpoint filter ignores case");
            }

            using (var endpoints = await served.CallAsync("get_http_endpoints", new() { ["session"] = "0" }))
            {
                Assert.IsGreaterThanOrEqualTo(1, endpoints.RootElement.GetProperty("total").GetInt32());
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task The_timeline_is_paged_within_its_bounds_and_long_text_is_cut_and_said_to_be()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-mcp-");
        try
        {
            var (baseline, _) = await RecordAsync(folder.FullName);
            await using var served = await Served.StartAsync(baseline);

            long next;
            using (var page = await served.CallAsync("get_timeline"))
            {
                var root = page.RootElement;
                Assert.AreEqual(100, root.GetProperty("limit").GetInt32(), "100 by default");
                Assert.AreEqual(100, root.GetProperty("returned").GetInt32());
                Assert.IsGreaterThan(100, root.GetProperty("total").GetInt32());
                Assert.IsTrue(root.GetProperty("truncated").GetBoolean());
                var events = root.GetProperty("events").EnumerateArray().ToList();
                next = root.GetProperty("nextAfterEventId").GetInt64();
                Assert.AreEqual(events[^1].GetProperty("id").GetInt64(), next);
                Assert.AreSequenceEqual(events.Select(e => e.GetProperty("id").GetInt64()).Order().ToArray(), events.Select(e => e.GetProperty("id").GetInt64()).ToArray(), "in order");
            }

            using (var following = await served.CallAsync("get_timeline", new() { ["afterEventId"] = next, ["limit"] = 5 }))
            {
                Assert.IsTrue(following.RootElement.GetProperty("events").EnumerateArray().All(e => e.GetProperty("id").GetInt64() > next));
            }

            using (var capped = await served.CallAsync("get_timeline", new() { ["limit"] = 100_000 }))
            {
                Assert.AreEqual(500, capped.RootElement.GetProperty("limit").GetInt32(), "at most 500, whatever is asked");
                Assert.IsLessThanOrEqualTo(500, capped.RootElement.GetProperty("returned").GetInt32());
            }

            using (var custom = await served.CallAsync("get_timeline", new() { ["kind"] = "custom", ["limit"] = 500 }))
            {
                var events = custom.RootElement.GetProperty("events").EnumerateArray().ToList();
                Assert.IsTrue(events.All(e => e.GetProperty("kind").GetString() == "Custom"));
                var longOne = Assert.ContainsSingle(e => e.GetProperty("title").GetString() == "long detail", events);
                Assert.IsLessThanOrEqualTo(1001, longOne.GetProperty("detail").GetString()!.Length);
                Assert.EndsWith("…", longOne.GetProperty("detail").GetString());
                Assert.AreEqual("detail", Assert.ContainsSingle(longOne.GetProperty("truncatedFields").EnumerateArray()).GetString());
                Assert.IsFalse(events.First(e => e.GetProperty("title").GetString() != "long detail").TryGetProperty("truncatedFields", out _), "only what was cut is named");
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task Logs_are_read_only_from_a_logs_section_and_an_export_without_one_says_so()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-mcp-");
        try
        {
            var (baseline, _) = await RecordAsync(folder.FullName);
            Assert.IsTrue(JsonDocument.Parse(File.ReadAllText(baseline)).RootElement.TryGetProperty("logs", out _), "exports carry the logs BevTools captured");
            // An export written before BevTools captured logs has no such section.
            var older = JsonNode.Parse(File.ReadAllText(baseline))!.AsObject();
            older.Remove("logs");
            older.Remove("logCounts");
            var olderFile = Path.Combine(folder.FullName, "older.json");
            File.WriteAllText(olderFile, older.ToJsonString());
            await using (var served = await Served.StartAsync(olderFile))
            {
                using var absent = await served.CallAsync("get_logs");
                Assert.IsFalse(absent.RootElement.GetProperty("present").GetBoolean());
                Assert.AreEqual("no logs section in this export", absent.RootElement.GetProperty("message").GetString());
            }

            // The same export with a logs section, as an export that captures logs would carry one.
            var withLogs = JsonNode.Parse(File.ReadAllText(baseline))!.AsObject();
            withLogs["logs"] = new JsonArray(
                new JsonObject { ["level"] = "Information", ["category"] = "Microsoft.AspNetCore.Components.Routing", ["message"] = "Navigating to /orders" },
                new JsonObject { ["level"] = "Warning", ["category"] = "Microsoft.AspNetCore.Components.Server.Circuits", ["message"] = "Circuit lost" },
                new JsonObject { ["level"] = "Error", ["category"] = "Shop.Orders", ["message"] = "Order 7001 failed" },
                // Value access None keeps the message unread: the template stands for it.
                new JsonObject { ["level"] = "Information", ["category"] = "Shop.Cart", ["messageTemplate"] = "Added {Sku} to the cart" });
            var logged = Path.Combine(folder.FullName, "logged.json");
            File.WriteAllText(logged, withLogs.ToJsonString());
            await using (var served = await Served.StartAsync(logged))
            {
                using (var warnings = await served.CallAsync("get_logs", new() { ["minLevel"] = "warning" }))
                {
                    Assert.AreSequenceEqual(new[] { "Order 7001 failed", "Circuit lost" }, warnings.RootElement.GetProperty("logs").EnumerateArray().Select(l => l.GetProperty("message").GetString()).ToArray(), "most recent first");
                }

                using (var framework = await served.CallAsync("get_logs", new() { ["categoryPrefix"] = "Microsoft.AspNetCore.Components" }))
                {
                    Assert.AreEqual(2, framework.RootElement.GetProperty("total").GetInt32());
                }

                using (var text = await served.CallAsync("get_logs", new() { ["text"] = "ORDER 7001", ["limit"] = 0 }))
                {
                    Assert.AreEqual(1, text.RootElement.GetProperty("returned").GetInt32(), "a limit under 1 still returns one");
                }

                using (var template = await served.CallAsync("get_logs", new() { ["text"] = "to the cart" }))
                {
                    Assert.AreEqual(1, template.RootElement.GetProperty("total").GetInt32(), "an entry without its message is found by its template");
                }

                Assert.IsTrue((await served.CallTextAsync("get_logs", new() { ["minLevel"] = "loud" })).IsError);
            }
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public async Task Check_and_compare_answer_as_the_tool_does_on_the_loaded_exports()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-mcp-");
        try
        {
            var (baseline, current) = await RecordAsync(folder.FullName);
            await using var served = await Served.StartAsync(baseline, current);

            using (var check = await served.CallAsync("check", new() { ["session"] = "0" }))
            {
                var expected = BevSessionCheck.Check(File.ReadAllText(baseline));
                Assert.IsFalse(check.RootElement.GetProperty("passed").GetBoolean());
                Assert.AreEqual(baseline, check.RootElement.GetProperty("file").GetString());
                Assert.HasCount(expected.Findings.Count + expected.Dismissed.Count, check.RootElement.GetProperty("findings").EnumerateArray());
                Assert.IsTrue(check.RootElement.GetProperty("findings").EnumerateArray().Single(f => f.GetProperty("ruleId").GetString() == "cli.dismissed").GetProperty("dismissed").GetBoolean());
            }

            using (var lenient = await served.CallAsync("check", new() { ["session"] = "0", ["failOn"] = "none" }))
            {
                Assert.IsTrue(lenient.RootElement.GetProperty("passed").GetBoolean());
            }

            using (var ignoring = await served.CallAsync("check", new() { ["session"] = "0", ["ignore"] = new[] { "cli.error", "cli.hostile" } }))
            {
                Assert.IsTrue(ignoring.RootElement.GetProperty("passed").GetBoolean());
            }

            Assert.IsTrue((await served.CallTextAsync("check", new() { ["session"] = "0", ["failOn"] = "loud" })).IsError);

            using (var compare = await served.CallAsync("compare", new() { ["baseline"] = "0", ["current"] = "current.json" }))
            {
                var root = compare.RootElement;
                Assert.IsFalse(root.GetProperty("passed").GetBoolean());
                Assert.AreEqual(baseline, root.GetProperty("baseline").GetString());
                Assert.AreEqual(current, root.GetProperty("current").GetString());
                var parent = Assert.ContainsSingle(c => c.GetProperty("displayName").GetString() == nameof(ParentComponent), root.GetProperty("changes").EnumerateArray());
                Assert.AreEqual("Regressed", parent.GetProperty("kind").GetString());
                Assert.StartsWith("renders +2", parent.GetProperty("reason").GetString());
            }

            using (var same = await served.CallAsync("compare", new() { ["baseline"] = "1", ["current"] = "1" }))
            {
                Assert.IsTrue(same.RootElement.GetProperty("passed").GetBoolean());
            }

            using (var tolerant = await served.CallAsync("compare", new() { ["baseline"] = "0", ["current"] = "1", ["minRenderChange"] = 1000 }))
            {
                Assert.IsTrue(tolerant.RootElement.GetProperty("passed").GetBoolean(), "the thresholds are the tool's");
            }

            using (var allocations = await served.CallAsync("compare", new() { ["baseline"] = "0", ["current"] = "1", ["minAllocationChangeBytes"] = 4096L * 1024 * 1024, ["allocationChangeRatio"] = 2.5 }))
            {
                var options = allocations.RootElement.GetProperty("options");
                Assert.AreEqual(4096L * 1024 * 1024, options.GetProperty("minAllocationChangeBytes").GetInt64(), "past what an int holds");
                Assert.AreEqual(2.5, options.GetProperty("allocationChangeRatio").GetDouble());
            }

            var (unknownError, unknown) = await served.CallTextAsync("compare", new() { ["baseline"] = "0", ["current"] = "7" });
            Assert.IsTrue(unknownError);
            Assert.Contains("No export has index 7", unknown);
            Assert.IsTrue((await served.CallTextAsync("compare", new() { ["baseline"] = "0", ["current"] = "1", ["timeChangeRatio"] = -1 })).IsError);
            Assert.IsTrue((await served.CallTextAsync("compare", new() { ["baseline"] = "0", ["current"] = "1", ["minAllocationChangeBytes"] = -1 })).IsError);
            Assert.IsTrue((await served.CallTextAsync("compare", new() { ["baseline"] = "0", ["current"] = "1", ["allocationChangeRatio"] = -0.5 })).IsError);
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    [TestMethod]
    public void A_page_stops_before_the_item_that_would_take_it_past_its_size_bound_and_pages_on_from_there()
    {
        var events = Enumerable.Range(1, 300).Select(i => new { id = i, kind = "Custom", title = "event " + i, detail = new string('x', 990) }).ToArray();
        using var exports = BevMcpExports.Parse([("big.json", JsonSerializer.Serialize(new { format = "bit-bevtools/1", timeline = events }))]);
        var tools = new BevMcpTools(exports);

        var answer = tools.GetTimeline(limit: 500);
        Assert.IsLessThanOrEqualTo(64 * 1024, System.Text.Encoding.UTF8.GetByteCount(answer), "within the bound, not one item past it");
        using var page = JsonDocument.Parse(answer);
        var root = page.RootElement;
        var returned = root.GetProperty("returned").GetInt32();
        Assert.IsTrue(returned is > 0 and < 300, "returned: " + returned);
        Assert.StartsWith("size", root.GetProperty("truncatedBy").GetString());
        var next = root.GetProperty("nextAfterEventId").GetInt64();
        Assert.AreEqual(returned, next);

        using var following = JsonDocument.Parse(tools.GetTimeline(afterEventId: next, limit: 500));
        Assert.AreEqual(next + 1, following.RootElement.GetProperty("events")[0].GetProperty("id").GetInt64(), "nothing skipped");
    }

    [TestMethod]
    public void The_list_of_sessions_stops_before_the_export_that_would_take_it_past_its_size_bound()
    {
        const string json = """{"format":"bit-bevtools/1"}""";
        var folder = new string('f', 1000);
        using var exports = BevMcpExports.Parse(Enumerable.Range(0, 200).Select(i => ($"{folder}/{i}.json", json)).ToList());
        var tools = new BevMcpTools(exports);

        var answer = tools.ListSessions();
        Assert.IsLessThanOrEqualTo(64 * 1024, System.Text.Encoding.UTF8.GetByteCount(answer), "within the bound, not one export past it");
        using var list = JsonDocument.Parse(answer);
        var root = list.RootElement;
        var sessions = root.GetProperty("sessions").EnumerateArray().ToList();
        Assert.IsTrue(sessions.Count is > 0 and < 200, "returned: " + sessions.Count);
        Assert.AreEqual(sessions.Count, root.GetProperty("returned").GetInt32());
        Assert.AreEqual(200, root.GetProperty("total").GetInt32());
        Assert.IsTrue(root.GetProperty("truncated").GetBoolean());
        Assert.StartsWith("size", root.GetProperty("truncatedBy").GetString());
        Assert.AreEqual(sessions.Count - 1, sessions[^1].GetProperty("index").GetInt32(), "in order, nothing skipped");
    }

    [TestMethod]
    public void An_export_is_named_exactly_first_and_regardless_of_case_after()
    {
        const string json = """{"format":"bit-bevtools/1"}""";
        using var exports = BevMcpExports.Parse([("run/a.json", json), ("run/A.json", json)]);
        Assert.AreEqual(0, exports.Get("a.json").Index);
        Assert.AreEqual(1, exports.Get("A.json").Index);
        Assert.AreEqual(1, exports.Get("run/A.json").Index);
        var ambiguous = Assert.ThrowsExactly<ModelContextProtocol.McpException>(() => exports.Get("A.JSON"));
        Assert.Contains("Several loaded exports", ambiguous.Message);
    }

    [TestMethod]
    public async Task The_server_refuses_what_it_cannot_serve_at_startup_and_ends_with_its_client()
    {
        var folder = Directory.CreateTempSubdirectory("bevtools-mcp-");
        try
        {
            var (baseline, _) = await RecordAsync(folder.FullName);
            var notAnExport = Path.Combine(folder.FullName, "other.json");
            File.WriteAllText(notAnExport, "{}");

            var (none, _, noneError) = Run("mcp");
            Assert.AreEqual(BevToolsCli.Unusable, none);
            Assert.Contains("Expected: bevtools mcp <export.json>", noneError);
            var (missing, _, missingError) = Run("mcp", baseline, Path.Combine(folder.FullName, "missing.json"));
            Assert.AreEqual(BevToolsCli.Unusable, missing, "every file is read before anything is served");
            Assert.Contains("No such file", missingError);
            var (other, _, otherError) = Run("mcp", notAnExport);
            Assert.AreEqual(BevToolsCli.Unusable, other);
            Assert.Contains("not a BevTools session export", otherError);
            Assert.AreEqual(BevToolsCli.Unusable, Run("mcp", baseline, "--port", "8080").Code, "no options: it serves stdio only");

            // Served is disposed by closing the client's side; its DisposeAsync fails the test if the server outlives it.
            await using var served = await Served.StartAsync(baseline);
            using var sessions = await served.CallAsync("list_sessions");
            Assert.AreEqual(1, sessions.RootElement.GetProperty("sessions").GetArrayLength());

            // An agent may call several tools at once; each still gets the whole, same answer.
            var answers = await Task.WhenAll(Enumerable.Range(0, 16).Select(i => served.CallTextAsync(i % 2 == 0 ? "get_findings" : "get_timeline")));
            Assert.IsTrue(answers.All(a => !a.IsError));
            Assert.ContainsSingle(answers.Where((_, i) => i % 2 == 0).Select(a => a.Text).Distinct());
            Assert.ContainsSingle(answers.Where((_, i) => i % 2 == 1).Select(a => a.Text).Distinct());
        }
        finally
        {
            folder.Delete(recursive: true);
        }
    }

    /// <summary>
    /// Two exports of one session: rendered, with an error twice, two HTTP requests (one failing), 150 custom events (one
    /// with a long detail) and the planted findings (one dismissed); then two more renders of the parent.
    /// </summary>
    private async Task<(string Baseline, string Current)> RecordAsync(string folder)
    {
        var cut = Render<ParentComponent>(ps => ps.Add(p => p.Value, 1));
        await cut.InvokeAsync(() => cut.Instance.Increment());
        using (var client = new HttpClient(new BevToolsHttpMessageHandler(Session) { InnerHandler = new StatusHandler() }))
        {
            await client.GetAsync("https://shop.test/api/orders/7", TestContext.CancellationToken);
            await client.GetAsync("https://shop.test/api/customers/3", TestContext.CancellationToken);
        }

        Session.Errors.Record(new InvalidOperationException("Order 7001 was not found"), "blazor");
        Session.Errors.Record(new InvalidOperationException("Order 9002 was not found"), "blazor");
        foreach (var i in Enumerable.Range(0, 150))
        {
            Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Custom, Category = "app", Title = "step " + i });
        }

        Session.Timeline.Record(new BevToolsEvent { Kind = BevToolsEventKind.Custom, Category = "app", Title = "long detail", Detail = string.Concat(Enumerable.Repeat("a long detail ", 400)) });
        Session.Diagnostics.Evaluate(force: true);
        Session.Diagnostics.Dismiss("cli.dismissed");
        var baseline = Path.Combine(folder, "baseline.json");
        File.WriteAllText(baseline, BevSessionExporter.ToJson(Session));

        await cut.InvokeAsync(() => cut.Instance.Increment());
        await cut.InvokeAsync(() => cut.Instance.Increment());
        var current = Path.Combine(folder, "current.json");
        File.WriteAllText(current, BevSessionExporter.ToJson(Session));
        return (baseline, current);
    }

    private static (int Code, string Output, string Error) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var code = BevToolsCli.Run(args, output, error);
        return (code, output.ToString(), error.ToString());
    }

    private sealed class StatusHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(request.RequestUri!.AbsolutePath.StartsWith("/api/orders", StringComparison.Ordinal) ? HttpStatusCode.InternalServerError : HttpStatusCode.OK) { Content = new StringContent("{}") });
    }

    /// <summary>The server on one end of two in-memory pipes and a real MCP client on the other: the protocol, not the methods.</summary>
    private sealed class Served : IAsyncDisposable
    {
        private readonly Pipe _toServer = new();
        private readonly Pipe _toClient = new();
        private readonly CancellationTokenSource _timeout = new(TimeSpan.FromMinutes(2));
        private BevMcpExports _exports = null!;
        private Task _server = Task.CompletedTask;

        public McpClient Client { get; private set; } = null!;

        public static async Task<Served> StartAsync(params string[] files)
        {
            var served = new Served { _exports = BevMcpExports.Parse(files.Select(f => (f, File.ReadAllText(f))).ToList()) };
            served._server = BevToolsMcp.RunAsync(served._exports, served._toServer.Reader.AsStream(), served._toClient.Writer.AsStream(), served._timeout.Token);
            served.Client = await McpClient.CreateAsync(new StreamClientTransport(served._toServer.Writer.AsStream(), served._toClient.Reader.AsStream()), cancellationToken: served._timeout.Token);
            return served;
        }

        /// <summary>A tool's answer, which must be a success, as JSON.</summary>
        public async Task<JsonDocument> CallAsync(string tool, Dictionary<string, object?>? arguments = null)
        {
            var (isError, text) = await CallTextAsync(tool, arguments);
            Assert.IsFalse(isError, tool + ": " + text);
            return JsonDocument.Parse(text);
        }

        public async Task<(bool IsError, string Text)> CallTextAsync(string tool, Dictionary<string, object?>? arguments = null)
        {
            var result = await Client.CallToolAsync(tool, arguments, cancellationToken: _timeout.Token);
            return (result.IsError == true, Assert.IsInstanceOfType<TextContentBlock>(Assert.ContainsSingle(result.Content)).Text);
        }

        public async ValueTask DisposeAsync()
        {
            await Client.DisposeAsync();
            // The end of its input is the end of the session, as a closed stdin is for the real tool.
            await _toServer.Writer.CompleteAsync();
            await _server.WaitAsync(TimeSpan.FromSeconds(30));
            _exports.Dispose();
            _timeout.Dispose();
        }
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}
