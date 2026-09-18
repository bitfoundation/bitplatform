using System.Text;
using OtpNet;
using Microsoft.AspNetCore.Identity;
using ModelContextProtocol;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Boilerplate.Tests.Features.DevMcp;

internal static class DevMcpTestUtils
{
    /// <summary>
    /// /dev-mcp needs the System.DevMcp feature AND <see cref="AuthPolicies.TFA_ENABLED"/>, so the account has to hold
    /// both. TFA_ENABLED is the amr=mfa claim, which only a real two-factor sign-in produces - so this enrols an
    /// authenticator and signs in through it rather than flipping a column.
    /// </summary>
    public static async Task<(string Email, TestAccountUtils.GlobalAdminGrant Grant)> SignInAsGlobalAdmin(
        AppTestServer server, AsyncServiceScope scope, CancellationToken cancellationToken)
    {
        var (email, userId) = await TestAccountUtils.CreateAndSignIn(server, scope, cancellationToken);
        // Two factor first, so MakeGlobalAdmin's refresh is also what proves amr=mfa survives one.
        await EnableTwoFactorAndSignInWithIt(server, scope, email, userId, cancellationToken);
        var grant = await TestAccountUtils.MakeGlobalAdmin(server, scope, userId, cancellationToken);
        return (email, grant);
    }

    /// <summary>Enrols an authenticator, then signs in through it so the session really carries amr=mfa.</summary>
    public static async Task EnableTwoFactorAndSignInWithIt(
        AppTestServer server, AsyncServiceScope scope, string email, Guid userId, CancellationToken cancellationToken)
    {
        const string password = "P@ssw0rdP@ssw0rd";

        // The per-run account is created through the magic link, so it has no password to sign in with a second time.
        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var account = await dbContext.Users.IgnoreQueryFilters().SingleAsync(item => item.Id == userId, cancellationToken);
            account.PasswordHash = new PasswordHasher<User>().HashPassword(account, password);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var userController = scope.ServiceProvider.GetRequiredService<IUserController>();
        var enrolment = await userController.TwoFactorAuth(new(), cancellationToken);
        var sharedKey = enrolment.SharedKey!.Replace(" ", "");

        var enabled = await userController.TwoFactorAuth(
            new() { Enable = true, TwoFactorCode = ComputeTotp(sharedKey) }, cancellationToken);
        Assert.IsTrue(enabled.IsTwoFactorEnabled, "Two factor should be on after enabling it with a valid code.");

        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        Assert.IsTrue(await authManager.SignIn(new() { Email = email, Password = password }, cancellationToken),
            "An account with two factor on must be challenged for the second factor.");

        await authManager.SignIn(new()
        {
            Email = email,
            Password = password,
            TwoFactorCode = ComputeTotp(sharedKey)
        }, cancellationToken);
    }

    public static string ComputeTotp(string base32Secret) => new Totp(Base32Encoding.ToBytes(base32Secret)).ComputeTotp();

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
