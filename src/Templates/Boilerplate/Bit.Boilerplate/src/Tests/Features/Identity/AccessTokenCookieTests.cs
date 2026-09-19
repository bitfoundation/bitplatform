using Microsoft.Net.Http.Headers;
using Boilerplate.Shared.Features.Identity.Dtos;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;
using SameSiteMode = Microsoft.Net.Http.Headers.SameSiteMode;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// The access_token cookie pre-rendering reads (See UserController.BuildAccessTokenCookieOptions), and the server-side
/// plumbing that carries a user's token to the api on the user's behalf.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class AccessTokenCookieTests
{
    // The typed IUserController returns nothing, and the cookie is in the response headers.
    private const string UpdateSessionUri = $"api/v1/User/{nameof(IUserController.UpdateSession)}";
    private const string SignOutUri = $"api/v1/User/{nameof(IUserController.SignOut)}";

    /// <summary>
    /// Host-only, so the browser keeps it for exactly the host that answered, and Delete has to name the attributes
    /// Append used, or the browser keeps the cookie after sign out.
    /// </summary>
    [TestMethod]
    public async Task UpdateSession_Should_WriteAHostOnlyCookie_ThatSignOutDeletes()
    {
        await using var server = await StartServer(services => services.AddIntegrationApiOnlyTestsServices());
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        var accessToken = await scope.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");
        var tokenExpiry = DateTimeOffset.FromUnixTimeSeconds(IAuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: false).GetClaimValue<long>("exp"));

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        using var updated = await httpClient.PostAsJsonAsync(UpdateSessionUri, new UpdateUserSessionRequestDto(), TestContext.CancellationToken);
        var written = AccessTokenCookie(updated);

        Assert.AreEqual(accessToken, written.Value.ToString());
        Assert.AreEqual<DateTimeOffset?>(tokenExpiry, written.Expires, "The cookie must not outlive the token it carries.");
        Assert.IsTrue(written.HttpOnly);
        Assert.AreEqual(SameSiteMode.Strict, written.SameSite);
        Assert.AreEqual("/", written.Path.ToString());
        Assert.IsFalse(written.Domain.HasValue, "A Domain is dropped by the browser when the host that answered is not under it, and reaches every subdomain when it is.");

        using var signedOut = await httpClient.PostAsync(SignOutUri, content: null, TestContext.CancellationToken);
        var deleted = AccessTokenCookie(signedOut);

        Assert.IsLessThan(DateTimeOffset.UtcNow, deleted.Expires!.Value, "SignOut has to expire the cookie.");
        Assert.AreEqual(written.Path, deleted.Path);
        Assert.AreEqual(written.Domain, deleted.Domain);
        Assert.AreEqual(written.HttpOnly, deleted.HttpOnly);
        Assert.AreEqual(written.SameSite, deleted.SameSite);
    }

    [TestMethod]
    public async Task ANonWebClient_Should_GetNoAccessTokenCookie()
    {
        await using var server = await StartServer(services => services.AddIntegrationApiOnlyTestsServices());
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(scope);

        using var request = new HttpRequestMessage(HttpMethod.Post, UpdateSessionUri)
        {
            Content = JsonContent.Create(new UpdateUserSessionRequestDto { PlatformType = AppPlatformType.Android })
        };
        // RequestHeadersDelegatingHandler keeps a platform the request already names.
        request.Headers.Add("X-App-Platform", nameof(AppPlatformType.Android));

        using var response = await scope.ServiceProvider.GetRequiredService<HttpClient>().SendAsync(request, TestContext.CancellationToken);

        Assert.IsFalse(response.Headers.Contains(HeaderNames.SetCookie), "Only a browser pre-renders, so only a web client gets the cookie.");
    }

    /// <summary>
    /// Server.Web builds every pre-rendering and Blazor Server HttpClient on one <see cref="SocketsHttpHandler"/>. With
    /// cookies on, the access_token cookie UpdateSession answers a signed-in session with went out with every later call
    /// of every other user, and the api authenticated an anonymous visitor's call as that user.
    /// </summary>
    [TestMethod]
    public async Task TheSharedServerSideHandler_Should_NotCarryOneUsersCookieIntoAnothersCall()
    {
        await using var server = await StartServer(services => services.AddIntegrationApiOnlyTestsServices());
        var sharedHandler = server.WebApp.Services.GetRequiredService<SocketsHttpHandler>();

        await using var signedInScope = server.WebApp.Services.CreateAsyncScope();
        await SignIn(signedInScope);

        using (var signedInClient = CreateServerSideClient(signedInScope, server, sharedHandler))
        {
            using var updated = await signedInClient.PostAsJsonAsync(UpdateSessionUri, new UpdateUserSessionRequestDto(), TestContext.CancellationToken);

            Assert.IsNotNull(AccessTokenCookie(updated), "Precondition: the api answered the signed-in session with its cookie.");
        }

        await using var anonymousScope = server.WebApp.Services.CreateAsyncScope();
        using var anonymousClient = CreateServerSideClient(anonymousScope, server, sharedHandler);

        await Assert.ThrowsExactlyAsync<UnauthorizedException>(
            () => anonymousClient.GetAsync($"api/v1/User/{nameof(IUserController.GetCurrentUser)}", TestContext.CancellationToken),
            "A call with no token of its own must stay anonymous, whatever an earlier response on the same handler set.");
    }


    private async Task<AppTestServer> StartServer(Action<IServiceCollection>? configureTestServices = null)
    {
        var server = new AppTestServer();
        await server.Build(configureTestServices).Start(TestContext.CancellationToken);
        return server;
    }

    private Task SignIn(AsyncServiceScope scope)
    {
        return scope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);
    }

    /// <summary>
    /// What Server.Web's own HttpClient is: the app's handler chain on the shared transport. Neither is disposed here,
    /// the transport belongs to the server.
    /// </summary>
    private static HttpClient CreateServerSideClient(AsyncServiceScope scope, AppTestServer server, SocketsHttpHandler sharedHandler)
    {
        var handlerFactory = scope.ServiceProvider.GetRequiredService<HttpMessageHandlersChainFactory>();

        return new HttpClient(handlerFactory.Invoke(sharedHandler), disposeHandler: false)
        {
            BaseAddress = server.WebAppServerAddress
        };
    }

    private static SetCookieHeaderValue AccessTokenCookie(HttpResponseMessage response)
    {
        Assert.IsTrue(response.Headers.TryGetValues(HeaderNames.SetCookie, out var setCookies), "The response carries no Set-Cookie.");

        return SetCookieHeaderValue.ParseList([.. setCookies]).Single(cookie => cookie.Name == "access_token");
    }

    public TestContext TestContext { get; set; } = default!;
}
