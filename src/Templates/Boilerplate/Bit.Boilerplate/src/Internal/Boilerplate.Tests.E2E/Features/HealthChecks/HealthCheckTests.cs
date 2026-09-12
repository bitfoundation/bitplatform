namespace Boilerplate.Tests.E2E.Features.HealthChecks;

/// <summary>
/// MapAppHealthChecks (Server.Shared) on every deployment: /health runs every check, /alive only the "live" ones, and
/// both answer with the bare status - the names and details stay behind /healthz, mapped in Development only, and the
/// Dev MCP's GetHealth, which is global-admin only.
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public partial class HealthCheckTests
{
    /// <summary>What a public health endpoint may say. Degraded still means "in rotation" (See AddServerApiHealthChecks).</summary>
    private static readonly string[] publicStatuses = ["Healthy", "Degraded"];

    /// <summary>What AdminPanelApi registers: the shared disk check, then Server.Api's own (See AddServerApiHealthChecks).</summary>
    private static readonly string[] requiredChecks = ["binStorage", "AppDbContext", "hangfire", "userProfileImages", "sms"];

    /// <summary>The checks that fail /health rather than degrade it - an instance without them should leave rotation.</summary>
    private static readonly string[] criticalChecks = ["binStorage", "AppDbContext", "hangfire"];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, "health", DisplayName = "AdminPanel /health")]
    [DataRow(DeployedApps.AdminPanel, "alive", DisplayName = "AdminPanel /alive")]
    [DataRow(DeployedApps.Todo, "health", DisplayName = "Todo /health")]
    [DataRow(DeployedApps.Todo, "alive", DisplayName = "Todo /alive")]
    [DataRow(DeployedApps.Sales, "health", DisplayName = "Sales (integrated API) /health")]
    [DataRow(DeployedApps.Sales, "alive", DisplayName = "Sales (integrated API) /alive")]
    [DataRow(DeployedApps.AdminPanelApi, "health", DisplayName = "AdminPanelApi /health")]
    [DataRow(DeployedApps.AdminPanelApi, "alive", DisplayName = "AdminPanelApi /alive")]
    [DataRow(DeployedApps.TodoApi, "health", DisplayName = "TodoApi /health")]
    [DataRow(DeployedApps.TodoApi, "alive", DisplayName = "TodoApi /alive")]
    public async Task PublicEndpoint_Should_SayOnlyTheStatus(string host, string path)
    {
        using var response = await Send(host, path);
        var body = (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Trim();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"{host}{path} answered {(int)response.StatusCode}: {body}");
        Assert.IsNotNull(response.Content.Headers.ContentType, $"{host}{path} answered with no Content-Type.");
        Assert.AreEqual("text/plain", response.Content.Headers.ContentType.MediaType);
        // One word, not a report: which checks exist, and why one fails, is not for the public.
        Assert.Contains(body, publicStatuses, $"{host}{path} said '{body}'.");
    }

    [TestMethod]
    [DataRow(DeployedApps.AdminPanel, DisplayName = "AdminPanel")]
    [DataRow(DeployedApps.Todo, DisplayName = "Todo")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    public async Task DetailedEndpoint_Should_NotBeServedInProduction(string host)
    {
        using var response = await Send(host, "healthz");

        // A 404 from an API, a redirect to NotFoundPage from a web app - anything but the report.
        Assert.AreNotEqual(HttpStatusCode.OK, response.StatusCode, $"{host}healthz is served in production.");
    }

    /// <summary>
    /// The Dev MCP on AdminPanel's API, through the run's global admin session: the full report, every check by name.
    /// Failure messages name a check and its description only - a check's exception can carry a connection string.
    /// </summary>
    [TestMethod]
    public async Task DevMcp_Should_ReportEveryCheckTheApiRuns()
    {
        await SkipWithoutGlobalAdminCredentials();

        var globalApiClient = await DeployedApiClientProvider.GetGlobalApiClient(TestContext.CancellationToken);
        var json = await globalApiClient.McpClient!.CallText("GetHealth", arguments: null, TestContext.CancellationToken);
        var report = JsonSerializer.Deserialize<HealthReport>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        var summary = string.Join(", ", report.Checks.Select(check => $"{check.Name}={check.Status}"));

        foreach (var name in requiredChecks)
        {
            Assert.Contains(check => string.Equals(check.Name, name, StringComparison.OrdinalIgnoreCase), report.Checks,
                $"GetHealth has no '{name}' check. It reported: {summary}");
        }

        foreach (var name in criticalChecks)
        {
            var check = report.Checks.Single(check => string.Equals(check.Name, name, StringComparison.OrdinalIgnoreCase));
            Assert.AreEqual("Healthy", check.Status, $"'{name}' is {check.Status}: {check.Description}");
        }

        // /alive runs only these: the disk the app runs from, nothing remote.
        Assert.Contains("live", report.Checks.Single(check => string.Equals(check.Name, "binStorage", StringComparison.OrdinalIgnoreCase)).Tags ?? []);

        // The public endpoint says the same thing in one word (its answer is output cached for 10 seconds).
        using var response = await Send(DeployedApps.AdminPanelApi, "health");
        var publicStatus = (await response.Content.ReadAsStringAsync(TestContext.CancellationToken)).Trim();
        Assert.AreEqual(report.Status, publicStatus, $"GetHealth says {report.Status} ({summary}), /health says {publicStatus}.");
    }

    private async Task<HttpResponseMessage> Send(string host, string path)
    {
        using var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromMinutes(2) };

        return await httpClient.GetAsync(new Uri(new Uri(host), path), TestContext.CancellationToken);
    }

    /// <summary>As AppTestBase's: the Dev MCP session is the global admin's.</summary>
    private static async Task SkipWithoutGlobalAdminCredentials()
    {
        await using var apiClient = DeployedApiClientProvider.CreateApiClientFor(DeployedApps.AdminPanelApi);
        var configuration = apiClient.Services.GetRequiredService<IConfiguration>();

        if (string.IsNullOrWhiteSpace(configuration["GlobalAdminEmail"]) || string.IsNullOrWhiteSpace(configuration["GlobalAdminPassword"]))
            Assert.Inconclusive("'GlobalAdminEmail' / 'GlobalAdminPassword' are not in this project's user secrets, and the Dev MCP is reached through the global admin.");
    }

    /// <summary>The part of DevMcpDiagnosticTools.GetHealth's answer this test reads; Exception is left out on purpose.</summary>
    private sealed record HealthReport(string Status, HealthCheck[] Checks);

    private sealed record HealthCheck(string Name, string Status, string? Description, string[]? Tags);
}
