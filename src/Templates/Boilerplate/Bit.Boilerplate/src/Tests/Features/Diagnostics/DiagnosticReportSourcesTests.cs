using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
using Boilerplate.Shared.Features.Diagnostic;
using Boilerplate.Shared.Infrastructure.Services;
using Boilerplate.Tests.Features.DevMcp;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// The report is reachable three ways - the anonymous http endpoint behind the /diagnostic page, the hub method, and
/// <c>GetDiagnosticReport</c> on /dev-mcp - and they exist to be compared: behind a proxy each is its own path to the
/// origin.
/// <para>
/// In process all three are loopback, so what this pins is that each really answers about a request of its own. The
/// hub is the one that can quietly fail to: SignalR carries no ambient HttpContext, and a report built without one
/// names no client rather than throwing.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class DiagnosticReportSourcesTests
{
    private static readonly Regex ClientIpLine = new(@"Client IP:\s*(\S+)", RegexOptions.Compiled);

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task EveryWayIn_Should_AnswerAboutItsOwnRequest()
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        // Only /dev-mcp needs this; the other two are anonymous, and the report is the caller's own either way.
        var (_, grant) = await DevMcpTestUtils.SignInAsGlobalAdmin(server, scope, TestContext.CancellationToken);
        await using var _ = grant;
        var accessToken = await DevMcpTestUtils.AccessToken(scope);

        // The typed client goes over http, which is the same call the /diagnostic page makes. Both ids are null, so
        // none of the endpoint's side effects run.
        var http = string.Join(Environment.NewLine, await scope.ServiceProvider.GetRequiredService<IDiagnosticController>()
            .PerformDiagnostic(signalRConnectionId: null, pushNotificationSubscriptionDeviceId: null, TestContext.CancellationToken));

        await using var hubConnection = new HubConnectionBuilder()
            .WithUrl(new Uri(server.WebAppServerAddress, "app-hub"), options =>
            {
                // The upgrade is the whole point: long polling would travel the same path as the http call above.
                options.Transports = HttpTransportType.WebSockets;
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();

        await hubConnection.StartAsync(TestContext.CancellationToken);

        var signalR = string.Join(Environment.NewLine,
            await hubConnection.InvokeAsync<string[]>(SharedAppMessages.GetDiagnosticReport, TestContext.CancellationToken));

        await using var mcp = await DevMcpTestUtils.Connect(server, accessToken, "dev-mcp", TestContext.CancellationToken);

        var devMcp = await DevMcpTestUtils.CallText(mcp, "GetDiagnosticReport", [], TestContext.CancellationToken);

        foreach (var (via, report) in new[] { ("Http", http), ("SignalR", signalR), ("DevMcp", devMcp) })
        {
            Assert.Contains($"Via: {via}", report,
                $"The report says which door it came in through, and this one is mislabelled - which makes three reports side by side unreadable. Report:{Environment.NewLine}{report}");

            Assert.DoesNotContain("No request is in flight", report,
                $"{via} built its report with no HttpContext, so it describes nothing. Report:{Environment.NewLine}{report}");

            var match = ClientIpLine.Match(report);

            Assert.IsTrue(match.Success, $"{via} answered without a 'Client IP' line. Report:{Environment.NewLine}{report}");

            Assert.IsTrue(IPAddress.TryParse(match.Groups[1].Value, out var clientIp) && IPAddress.IsLoopback(clientIp),
                $"{via} resolved '{match.Groups[1].Value}' as the caller, and everything here is on this machine.");

            Assert.Contains("Host:", report, $"{via} returned no request headers at all.");

            Assert.DoesNotContain(accessToken, report,
                $"{via} put the caller's access token in its report. The report is pasted into issues and, over /dev-mcp, handed to a model.");
        }

        // Enough to diagnose a wrong or missing credential without the credential itself: the assertion above is the
        // guarantee, this one says the header is still reported.
        Assert.Contains($"Authorization: Bearer {accessToken[..10]}...{accessToken[^10..]}", devMcp,
            "The scheme and both ends of the token have to survive, or 'no header at all', 'the wrong kind of header' and 'the wrong token' all read the same.");
    }
}
