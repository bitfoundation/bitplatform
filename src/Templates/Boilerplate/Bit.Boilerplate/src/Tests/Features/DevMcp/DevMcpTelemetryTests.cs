using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Boilerplate.Tests.Features.DevMcp;

[TestClass, TestCategory("IntegrationTest")]
public class DevMcpTelemetryTests
{
    private const string TraceId = "0af7651916cd43dd8448eb211c80319c";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task TelemetryTools_Should_ReadTheDashboardWithItsKey_AndShapeWhatItKeeps()
    {
        await using var dashboard = await FakeAspireDashboard.Start(TestContext.CancellationToken);
        await using var server = new AppTestServer();
        await server.Build(configureTestConfiguration: configuration =>
        {
            configuration["AspireDashboard:Url"] = dashboard.Url;
            configuration["AspireDashboard:ApiKey"] = FakeAspireDashboard.InitialApiKey;
        }).Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(client, TestContext.CancellationToken);
        await using var _ = grant;
        await using var mcp = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(client), "dev-mcp", TestContext.CancellationToken);

        var logs = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "ListLogs", new()
        {
            ["resource"] = "serverweb",
            ["severity"] = "warning",
            ["search"] = "failed",
            ["take"] = 500
        }, TestContext.CancellationToken))!;

        var logsRequest = dashboard.Requests.Last(request => request.Path is "/api/telemetry/logs");
        Assert.AreEqual(FakeAspireDashboard.InitialApiKey, logsRequest.ApiKey);
        Assert.AreEqual("Warning", logsRequest.Query["severity"]);
        Assert.AreEqual("resource:serverweb failed", logsRequest.Query["search"], "A service name reaches the dashboard as a qualifier, which matches every instance of it.");
        Assert.AreEqual("100", logsRequest.Query["limit"]);

        var entries = logs["logs"]!.AsArray();
        Assert.HasCount(2, entries);
        Assert.AreEqual("Hello info", entries[0]!["message"]!.GetValue<string>(), "The newest log comes first.");
        var error = entries[1]!;
        Assert.AreEqual("Error", error["severity"]!.GetValue<string>());
        Assert.AreEqual("serverweb", error["resource"]!.GetValue<string>());
        Assert.AreEqual("Boilerplate.Server.Api.Features.Orders", error["category"]!.GetValue<string>());
        Assert.AreEqual("Order 42 failed", error["message"]!.GetValue<string>());
        Assert.AreEqual(TraceId, error["traceId"]!.GetValue<string>());
        Assert.AreEqual("42", error["attributes"]!["OrderId"]!.GetValue<string>());
        Assert.IsNull(error["attributes"]!["exception.type"], "The exception attributes move into exception.");
        Assert.IsNull(error["attributes"]!["aspire.log_id"]);
        Assert.StartsWith("System.InvalidOperationException: Boom", error["exception"]!.GetValue<string>());
        Assert.AreEqual(DateTimeOffset.UnixEpoch.AddTicks(1794066769478509700 / 100), error["timestamp"]!.GetValue<DateTimeOffset>());
        Assert.AreEqual(2, logs["totalCount"]!.GetValue<int>());

        var traces = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "ListTraces", new() { ["hasError"] = true }, TestContext.CancellationToken))!;

        Assert.AreEqual("true", dashboard.Requests.Last(request => request.Path is "/api/telemetry/traces").Query["hasError"]);
        var trace = traces["traces"]!.AsArray().Single()!;
        Assert.AreEqual(TraceId, trace["traceId"]!.GetValue<string>());
        Assert.AreEqual("POST /api/Order/Create", trace["name"]!.GetValue<string>(), "A trace is named after its root span.");
        Assert.AreEqual(250d, trace["durationMs"]!.GetValue<double>());
        Assert.AreEqual(2, trace["spanCount"]!.GetValue<int>());
        Assert.IsTrue(trace["hasError"]!.GetValue<bool>());
        Assert.AreEqual("serverweb: POST /api/Order/Create (failed)", trace["failedSpans"]!.AsArray().Single()!.GetValue<string>());

        var details = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "GetTrace", new() { ["traceId"] = "0AF7651" }, TestContext.CancellationToken))!;

        Assert.IsTrue(details["found"]!.GetValue<bool>());
        Assert.AreEqual(TraceId, details["traceId"]!.GetValue<string>());
        Assert.AreEqual(TraceId, dashboard.Requests.Last(request => request.Path is "/api/telemetry/logs").Query["traceId"], "The trace's logs are asked for by its full id, not by the start of it.");
        var spans = details["spans"]!.AsArray();
        Assert.HasCount(2, spans);
        Assert.AreEqual("POST /api/Order/Create", spans[0]!["name"]!.GetValue<string>());
        Assert.AreEqual("Server", spans[0]!["kind"]!.GetValue<string>());
        Assert.AreEqual("Error", spans[0]!["status"]!.GetValue<string>());
        Assert.AreEqual("exception", spans[0]!["events"]![0]!["name"]!.GetValue<string>());
        Assert.AreEqual("b7ad6b7169203331", spans[1]!["parentSpanId"]!.GetValue<string>());
        Assert.AreEqual("Order 42 failed", details["logs"]!.AsArray().Single()!["message"]!.GetValue<string>());

        var missing = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "GetTrace", new() { ["traceId"] = "ffff" }, TestContext.CancellationToken))!;
        Assert.IsFalse(missing["found"]!.GetValue<bool>());

        var unknown = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "ListLogs", new() { ["resource"] = "nope" }, TestContext.CancellationToken))!;
        Assert.IsEmpty(unknown["logs"]!.AsArray());
        Assert.AreEqual("serverweb", unknown["knownServices"]!.AsArray().Single()!.GetValue<string>(), "A service name that matches nothing gets the names that would.");

        var requestCount = dashboard.Requests.Count;
        Assert.Contains("Information", await DevMcpTestUtils.CallText(mcp, "ListLogs", new() { ["severity"] = "loud" }, TestContext.CancellationToken));
        Assert.Contains("hexadecimal", await DevMcpTestUtils.CallText(mcp, "GetTrace", new() { ["traceId"] = "not-a-trace" }, TestContext.CancellationToken));
        Assert.Contains("one service name", await DevMcpTestUtils.CallText(mcp, "ListTraces", new() { ["resource"] = "serverweb severity:error" }, TestContext.CancellationToken));
        Assert.HasCount(requestCount, dashboard.Requests, "An argument the tool refuses never reaches the dashboard.");

        dashboard.ApiKey = "rotated";
        Assert.Contains("refused", await DevMcpTestUtils.CallText(mcp, "ListLogs", [], TestContext.CancellationToken));
    }

    [TestMethod]
    public async Task TelemetryTools_Should_SayNoDashboardIsConfigured()
    {
        await using var server = new AppTestServer();
        await server.Build().Start(TestContext.CancellationToken);
        await using var client = server.CreateAppClient();
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(client, TestContext.CancellationToken);
        await using var _ = grant;
        await using var mcp = await DevMcpTestUtils.Connect(server, await DevMcpTestUtils.AccessToken(client), "dev-mcp", TestContext.CancellationToken);

        Assert.Contains("AspireDashboard:Url", await DevMcpTestUtils.CallText(mcp, "ListLogs", [], TestContext.CancellationToken));
        Assert.Contains("AspireDashboard:Url", await DevMcpTestUtils.CallText(mcp, "GetTrace", new() { ["traceId"] = TraceId }, TestContext.CancellationToken));

        var deployment = JsonNode.Parse(await DevMcpTestUtils.CallText(mcp, "GetDeploymentInfo", [], TestContext.CancellationToken))!;
        Assert.IsFalse(deployment["capabilities"]!["aspireDashboard"]!.GetValue<bool>());
    }

    private sealed class FakeAspireDashboard : IAsyncDisposable
    {
        public const string InitialApiKey = "dashboard-key";

        private const string Resources = """[{"name":"serverweb","instanceId":"i1","displayName":"serverweb-i1","hasLogs":true,"hasTraces":true,"hasMetrics":false}]""";

        private const string NoLogs = """{"data":{"resourceLogs":[]},"totalCount":0,"returnedCount":0}""";

        private const string ErrorLog = """{"timeUnixNano":"1794066769478509700","severityNumber":17,"severityText":"Error","body":{"stringValue":"Order 42 failed"},"attributes":[{"key":"OrderId","value":{"stringValue":"42"}},{"key":"{OriginalFormat}","value":{"stringValue":"Order {OrderId} failed"}},{"key":"exception.type","value":{"stringValue":"System.InvalidOperationException"}},{"key":"exception.message","value":{"stringValue":"Boom"}},{"key":"exception.stacktrace","value":{"stringValue":"System.InvalidOperationException: Boom\n   at X.Y()"}},{"key":"aspire.log_id","value":{"stringValue":"1"}}],"traceId":"0af7651916cd43dd8448eb211c80319c","spanId":"b7ad6b7169203331"}""";

        private const string InfoLog = """{"timeUnixNano":"1794066769478510700","severityNumber":9,"severityText":"Information","body":{"stringValue":"Hello info"},"attributes":[{"key":"aspire.log_id","value":{"stringValue":"2"}}]}""";

        private const string Spans = """{"data":{"resourceSpans":[{"scopeSpans":[{"scope":{"name":"Microsoft.AspNetCore"},"spans":[{"traceId":"0af7651916cd43dd8448eb211c80319c","spanId":"b7ad6b7169203331","name":"POST /api/Order/Create","kind":2,"startTimeUnixNano":"1794066769478509700","endTimeUnixNano":"1794066769728509700","attributes":[{"key":"http.request.method","value":{"stringValue":"POST"}},{"key":"http.response.status_code","value":{"stringValue":"500"}}],"events":[{"timeUnixNano":"1794066769478510700","name":"exception","attributes":[{"key":"exception.type","value":{"stringValue":"System.InvalidOperationException"}}]}],"status":{"message":"failed","code":2}},{"traceId":"0af7651916cd43dd8448eb211c80319c","spanId":"c7ad6b7169203332","parentSpanId":"b7ad6b7169203331","name":"SELECT orders","kind":3,"startTimeUnixNano":"1794066769479509700","endTimeUnixNano":"1794066769483509700","attributes":[{"key":"db.system","value":{"stringValue":"postgresql"}}]}]}],"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"serverweb"}},{"key":"service.instance.id","value":{"stringValue":"i1"}}]}}]},"totalCount":1,"returnedCount":1}""";

        private readonly WebApplication app;

        private FakeAspireDashboard(WebApplication app) => this.app = app;

        public string Url => app.Urls.Single();

        public string ApiKey { get; set; } = InitialApiKey;

        public ConcurrentQueue<DashboardRequest> Requests { get; } = new();

        public static async Task<FakeAspireDashboard> Start(CancellationToken cancellationToken)
        {
            var builder = WebApplication.CreateSlimBuilder();
            builder.WebHost.UseUrls(AppTestServer.GenerateServerUrl("127.0.0.1"));
            var app = builder.Build();
            var dashboard = new FakeAspireDashboard(app);

            app.Use(async (context, next) =>
            {
                var apiKey = context.Request.Headers["X-API-Key"].ToString();
                dashboard.Requests.Enqueue(new(context.Request.Path, apiKey, context.Request.Query.ToDictionary(item => item.Key, item => item.Value.ToString())));

                if (apiKey != dashboard.ApiKey)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }

                await next(context);
            });

            app.MapGet("/api/telemetry/resources", () => Json(Resources));
            app.MapGet("/api/telemetry/logs", (string? traceId, string? search) => search?.Contains("resource:nope") is true
                ? Json(NoLogs)
                : Json(Logs(traceId is null ? [ErrorLog, InfoLog] : [ErrorLog])));
            app.MapGet("/api/telemetry/traces", () => Json(Spans));
            app.MapGet("/api/telemetry/traces/{traceId}", (string traceId) => TraceId.StartsWith(traceId, StringComparison.Ordinal) ? Json(Spans) : Results.NotFound());

            await app.StartAsync(cancellationToken);

            return dashboard;
        }

        private static string Logs(string[] records) => $$$"""{"data":{"resourceLogs":[{"scopeLogs":[{"scope":{"name":"Boilerplate.Server.Api.Features.Orders"},"logRecords":[{{{string.Join(',', records)}}}]}],"resource":{"attributes":[{"key":"service.name","value":{"stringValue":"serverweb"}},{"key":"service.instance.id","value":{"stringValue":"i1"}}]}}]},"totalCount":{{{records.Length}}},"returnedCount":{{{records.Length}}}}""";

        private static IResult Json(string json) => Results.Content(json, "application/json");

        public ValueTask DisposeAsync() => app.DisposeAsync();
    }

    private sealed record DashboardRequest(string Path, string ApiKey, Dictionary<string, string> Query);
}
