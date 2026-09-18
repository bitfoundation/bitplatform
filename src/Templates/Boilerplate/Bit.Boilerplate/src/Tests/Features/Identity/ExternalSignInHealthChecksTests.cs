using System.Text;
using System.Security.Cryptography;
using AspNet.Security.OAuth.Apple;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Server.Api.Features.Identity.HealthChecks;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// entraId and appleSignIn, with the providers replaced. The answers they are judged by were observed live on
/// login.microsoftonline.com and appleid.apple.com.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ExternalSignInHealthChecksTests
{
    private const string TokenEndpoint = "https://login.microsoftonline.com/test-tenant/oauth2/v2.0/token";

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"token_type":"Bearer"}""", HealthStatus.Healthy, DisplayName = "Token issued")]
    [DataRow(HttpStatusCode.BadRequest, """{"error":"invalid_scope"}""", HealthStatus.Healthy, DisplayName = "Credentials accepted, scope not")]
    [DataRow(HttpStatusCode.Unauthorized, """{"error":"invalid_client"}""", HealthStatus.Degraded, DisplayName = "Wrong secret")]
    [DataRow(HttpStatusCode.BadRequest, """{"error":"unauthorized_client"}""", HealthStatus.Degraded, DisplayName = "Unknown client id")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "{}", HealthStatus.Degraded, DisplayName = "Outage")]
    [DataRow(HttpStatusCode.TooManyRequests, """{"error":"temporarily_unavailable"}""", HealthStatus.Degraded, DisplayName = "Throttled")]
    [DataRow(HttpStatusCode.BadRequest, """{"error":"something_new"}""", HealthStatus.Degraded, DisplayName = "Unknown error")]
    public async Task EntraIdCheck_Should_JudgeTheTokenEndpointsAnswer(HttpStatusCode tokenStatus, string tokenBody, HealthStatus expected)
    {
        var tokenForm = string.Empty;
        var handler = new RoutingHandler(async (request, cancellationToken) =>
        {
            if (request.RequestUri!.AbsoluteUri is "https://login.microsoftonline.com/test-tenant/v2.0/.well-known/openid-configuration")
                return Json(HttpStatusCode.OK, $$"""{"token_endpoint":"{{TokenEndpoint}}"}""");

            Assert.AreEqual(TokenEndpoint, request.RequestUri.AbsoluteUri);
            tokenForm = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(tokenStatus, tokenBody);
        });

        var result = await RunEntraIdCheck(handler, clientSecret: "test-secret");

        Assert.AreEqual(expected, result.Status, result.Exception?.Message);
        Assert.Contains("grant_type=client_credentials", tokenForm);
        Assert.Contains("client_id=test-client", tokenForm);
        Assert.Contains("scope=test-client%2F.default", tokenForm);
        if (result.Exception is not null)
        {
            Assert.DoesNotContain("test-secret", result.Exception.Message);
        }
    }

    [TestMethod]
    public async Task EntraIdCheck_Should_ReportDegraded_ForAnUnknownTenant()
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.BadRequest, """{"error":"invalid_tenant"}""")));

        var result = await RunEntraIdCheck(handler, clientSecret: "test-secret");

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.HasCount(1, handler.Requests, "The token endpoint must not be asked when the tenant is unknown.");
    }

    [TestMethod]
    [DataRow("https://evil.example/token", DisplayName = "Another origin")]
    [DataRow("http://login.microsoftonline.com/test-tenant/oauth2/v2.0/token", DisplayName = "Cleartext")]
    public async Task EntraIdCheck_Should_KeepTheSecret_OnTheAuthoritysOrigin(string tokenEndpoint)
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, $$"""{"token_endpoint":"{{tokenEndpoint}}"}""")));

        var result = await RunEntraIdCheck(handler, clientSecret: "test-secret");

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.HasCount(1, handler.Requests, "The secret must not be posted to the discovered token endpoint.");
    }

    [TestMethod]
    public async Task EntraIdCheck_Should_OnlyReadDiscovery_WithoutASecret()
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, $$"""{"token_endpoint":"{{TokenEndpoint}}"}""")));

        var result = await RunEntraIdCheck(handler, clientSecret: null);

        Assert.AreEqual(HealthStatus.Healthy, result.Status);
        Assert.HasCount(1, handler.Requests);
    }

    [TestMethod]
    public async Task EntraIdCheck_Should_UseThePolicyAuthority_ForB2C()
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}")));

        await RunEntraIdCheck(handler, clientSecret: null, configure: c =>
        {
            c["Authentication:AzureAD:Instance"] = "https://contoso.b2clogin.com/";
            c["Authentication:AzureAD:Domain"] = "contoso.onmicrosoft.com";
            c["Authentication:AzureAD:SignUpSignInPolicyId"] = "B2C_1_signin";
        });

        Assert.AreEqual("https://contoso.b2clogin.com/contoso.onmicrosoft.com/B2C_1_signin/v2.0/.well-known/openid-configuration", handler.Requests.Single().AbsoluteUri);
    }

    [TestMethod]
    public async Task AppleCheck_Should_SignTheClientSecret_AndReachApple()
    {
        var keyPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.p8");
        using (var key = ECDsa.Create(ECCurve.NamedCurves.nistP256))
        {
            await File.WriteAllTextAsync(keyPath, key.ExportPkcs8PrivateKeyPem(), TestContext.CancellationToken);
        }

        try
        {
            Version? requestVersion = null;
            var handler = new RoutingHandler((request, _) =>
            {
                requestVersion = request.Version;
                return Task.FromResult(Json(HttpStatusCode.OK, "{}"));
            });

            var result = await RunAppleCheck(handler, keyPath);

            Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Exception?.ToString());
            Assert.AreEqual("https://appleid.apple.com/.well-known/openid-configuration", handler.Requests.Single().AbsoluteUri);
            Assert.AreEqual(HttpVersion.Version11, requestVersion, "The check must go through sign in's own client, which stays on HTTP/1.1 for Apple.");
        }
        finally
        {
            File.Delete(keyPath);
        }
    }

    [TestMethod]
    public async Task AppleCheck_Should_ReportDegraded_WithoutTheKeyFile()
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}")));

        var result = await RunAppleCheck(handler, Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.p8"));

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
        Assert.IsEmpty(handler.Requests, "Apple must not be asked when the secret can't even be signed.");
    }

    [TestMethod]
    public async Task AppleCheck_Should_ReportDegraded_WhenAppleIsUnreachable()
    {
        var handler = new RoutingHandler((_, _) => throw new HttpRequestException("No route to host"));

        var result = await RunAppleCheck(handler, keyPath: null);

        Assert.AreEqual(HealthStatus.Degraded, result.Status);
    }

    private async Task<HealthCheckResult> RunEntraIdCheck(RoutingHandler handler, string? clientSecret, Action<IConfiguration>? configure = null)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:AzureAD:Instance"] = "https://login.microsoftonline.com/",
            ["Authentication:AzureAD:TenantId"] = "test-tenant",
            ["Authentication:AzureAD:ClientId"] = "test-client",
            ["Authentication:AzureAD:ClientSecret"] = clientSecret
        }).Build();
        configure?.Invoke(configuration);

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        AddHttpClient(services, handler, "AzureAD");

        await using var serviceProvider = services.BuildServiceProvider();

        return await Run(ActivatorUtilities.CreateInstance<EntraIdHealthCheck>(serviceProvider));
    }

    private async Task<HealthCheckResult> RunAppleCheck(RoutingHandler handler, string? keyPath)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureHttpClientFactoryForExternalIdentityProviders();
        AddHttpClient(services, handler, AppleAuthenticationDefaults.AuthenticationScheme);
        services.AddAuthentication().AddApple(options =>
        {
            options.ClientId = "com.example.test";
            options.KeyId = "TESTKEYID1";
            options.TeamId = "TESTTEAM01";
            if (keyPath is not null)
            {
                options.UsePrivateKey(_ => new PhysicalFileInfo(new FileInfo(keyPath)));
            }
        });

        await using var serviceProvider = services.BuildServiceProvider();

        return await Run(ActivatorUtilities.CreateInstance<AppleSignInHealthCheck>(serviceProvider));
    }

    private async Task<HealthCheckResult> Run(IHealthCheck healthCheck)
    {
        var registration = new HealthCheckRegistration("test", healthCheck, HealthStatus.Degraded, tags: null);

        return await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);
    }

    private static void AddHttpClient(IServiceCollection services, HttpMessageHandler handler, string name)
    {
        services.AddHttpClient(name).ConfigurePrimaryHttpMessageHandler(() => handler);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string body)
        => new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    internal sealed class RoutingHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler
    {
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri!);
            return respond(request, cancellationToken);
        }

        // The factory disposes the handler chain with the client; the tests still read Requests afterwards.
        protected override void Dispose(bool disposing) { }
    }
}
