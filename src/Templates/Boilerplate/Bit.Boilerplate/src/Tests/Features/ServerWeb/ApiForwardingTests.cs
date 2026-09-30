using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// A standalone Server.Api runs on a host of its own, and Server.Web forwards <c>/api</c> and <c>/hangfire</c> to it
/// (See Server.Web's Program.Middlewares.cs): the Blazor WebAssembly calls that have to land on the page's own host go
/// that way, and so does the Hangfire dashboard. Every other test calls Server.Api directly, so the forwarding itself
/// is pinned here.
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ApiForwardingTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// An api call sent to the web app reaches Server.Api with its bearer token, and answers as the signed-in user.
    /// </summary>
    [TestMethod]
    public async Task AnApiCall_Should_ReachServerApi_ThroughTheWebApp()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestWebAppServices: services => services.AddBrowserlessClientServices()).Start(TestContext.CancellationToken);

        await using var scopeWebApp = server.WebApp.Services.CreateAsyncScope();
        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scopeWebApp, TestContext.CancellationToken);

        using var webAppClient = await CreateWebAppClient(server, scopeWebApp);

        var user = await webAppClient.GetFromJsonAsync<UserDto>($"api/v1/User/{nameof(IUserController.GetCurrentUser)}", TestContext.CancellationToken);

        Assert.IsNotNull(user);
        Assert.AreEqual(userId, user.Id, "Server.Web has to hand /api over to Server.Api, bearer token included.");
    }

    /// <summary>
    /// The Hangfire dashboard is Server.Api's. Server.Web serves no page of that name, so only the forwarding can answer
    /// <c>/hangfire</c> on the web app with it.
    /// </summary>
    [TestMethod]
    public async Task TheHangfireDashboard_Should_BeServedByServerApi_ThroughTheWebApp()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestWebAppServices: services => services.AddBrowserlessClientServices()).Start(TestContext.CancellationToken);

        await using var scopeWebApp = server.WebApp.Services.CreateAsyncScope();
        var (_, userId) = await TestAccountUtils.CreateAndSignIn(server, scopeWebApp, TestContext.CancellationToken);

        // The dashboard requires the Jobs_Manage feature (See HangfireDashboardAuthorizationFilter), which a global admin has.
        await using var _ = await TestAccountUtils.MakeGlobalAdmin(server, scopeWebApp, userId, TestContext.CancellationToken);

        using var webAppClient = await CreateWebAppClient(server, scopeWebApp);

        using var response = await webAppClient.GetAsync("hangfire", TestContext.CancellationToken);
        var html = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode,
            $"/hangfire on the web app has to be answered by Server.Api's dashboard. Location: '{response.Headers.Location}'.");
        Assert.Contains("Hangfire Dashboard", html);
    }

    /// <summary>A plain client on the web app that carries the access token the scope signed in with.</summary>
    private static async Task<HttpClient> CreateWebAppClient(AppTestServer server, AsyncServiceScope scopeWebApp)
    {
        var accessToken = await scopeWebApp.ServiceProvider.GetRequiredService<IStorageService>().GetItem("access_token");

        var httpClient = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = server.WebAppServerAddress };
        httpClient.DefaultRequestHeaders.Authorization = new("Bearer", accessToken);
        return httpClient;
    }
}
