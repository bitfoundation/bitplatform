using System.Text;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Boilerplate.Tests.Features.DevMcp;

internal static class DevMcpTestUtils
{
    public static async Task<(string Email, TestAccountUtils.GlobalAdminGrant Grant)> SignInAsGlobalAdmin(
        AppTestServer server, AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, cancellationToken);
        var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, cancellationToken);
        return (email, grant);
    }

    public static async Task<string> AccessToken(AsyncServiceScope scope)
    {
        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
        Assert.IsNotNull(accessToken, "Sign-in should have stored an access token.");
        return accessToken;
    }

    public static async Task<McpClient> Connect(AppTestServer server, string accessToken, string path, CancellationToken cancellationToken)
    {
        var transport = new HttpClientTransport(new HttpClientTransportOptions
        {
            Endpoint = new Uri(server.WebAppServerAddress, path),
            TransportMode = HttpTransportMode.StreamableHttp,
            AdditionalHeaders = new Dictionary<string, string>
            {
                ["Authorization"] = $"Bearer {accessToken}"
            }
        });

        return await McpClient.CreateAsync(transport, cancellationToken: cancellationToken);
    }

    public static async Task<HttpStatusCode> ProbeInitialize(Uri baseAddress, string path, string? accessToken, CancellationToken cancellationToken)
    {
        using var httpClient = new HttpClient { BaseAddress = baseAddress };
        using var request = new HttpRequestMessage(HttpMethod.Post, path);
        request.Content = new StringContent("""{"jsonrpc":"2.0","id":1,"method":"initialize","params":{}}""",
            Encoding.UTF8, "application/json");
        request.Headers.Accept.ParseAdd("application/json");
        request.Headers.Accept.ParseAdd("text/event-stream");
        if (accessToken is not null)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        return response.StatusCode;
    }

    public static async Task<string> CallText(McpClient client, string tool, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        var result = await client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
        var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
        Assert.AreNotEqual(true, result.IsError, $"Tool '{tool}' returned an error. Result: '{text}'.");
        return text;
    }

    public static async Task<string> CallError(McpClient client, string tool, Dictionary<string, object?> arguments, CancellationToken cancellationToken)
    {
        try
        {
            var result = await client.CallToolAsync(tool, arguments, cancellationToken: cancellationToken);
            var text = result.Content.OfType<TextContentBlock>().FirstOrDefault()?.Text ?? "";
            if (result.IsError is not true)
                Assert.Fail($"Tool '{tool}' was expected to fail. Result: '{text}'.");
            return text;
        }
        catch (McpProtocolException exception)
        {
            return exception.Message;
        }
    }
}
