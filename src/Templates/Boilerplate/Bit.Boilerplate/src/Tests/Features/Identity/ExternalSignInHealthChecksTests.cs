using System.Text;
using System.Security.Cryptography;
using AspNet.Security.OAuth.Apple;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Server.Api.Features.Identity.HealthChecks;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// entraId, appleSignIn and the Google, GitHub, X and Facebook sign in checks, with the providers replaced. The answers they
/// are judged by were observed live on login.microsoftonline.com, appleid.apple.com, oauth2.googleapis.com, github.com,
/// api.x.com and graph.facebook.com.
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
            // The handler sits on sign in's own client, so a request reaching it shows the check went through that client.
            var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}")));

            var result = await RunAppleCheck(handler, keyPath);

            Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Exception?.ToString());
            Assert.AreEqual("https://appleid.apple.com/.well-known/openid-configuration", handler.Requests.Single().AbsoluteUri);
        }
        finally
        {
            File.Delete(keyPath);
        }
    }

    /// <summary>
    /// A deployed server gets the key from Authentication:Apple:PrivateKey, which a CD secret carries on one line.
    /// </summary>
    [TestMethod]
    [DataRow(false, DisplayName = "The whole .p8")]
    [DataRow(true, DisplayName = "Its body on one line")]
    public async Task AppleCheck_Should_SignWithTheKeyFromConfiguration(bool bodyOnly)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var pem = key.ExportPkcs8PrivateKeyPem().ReplaceLineEndings("\r\n");
        var body = AppleP8Key.Body(pem);
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, "{}")));

        var result = await RunAppleCheck(handler, keyPath: null, privateKey: bodyOnly ? body : pem);

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Exception?.ToString());
        Assert.MatchesRegex(new Regex("^[A-Za-z0-9+/=]+$"), body);
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

    [TestMethod]
    [DataRow(HttpStatusCode.BadRequest, """{"error":"invalid_grant","error_description":"Malformed auth code."}""", HealthStatus.Healthy, DisplayName = "Credentials accepted")]
    [DataRow(HttpStatusCode.Unauthorized, """{"error":"invalid_client","error_description":"The provided client secret is invalid."}""", HealthStatus.Degraded, DisplayName = "Wrong secret")]
    [DataRow(HttpStatusCode.ServiceUnavailable, "<html>Unavailable</html>", HealthStatus.Degraded, DisplayName = "Outage")]
    public async Task GoogleCheck_Should_JudgeTheTokenEndpointsAnswer(HttpStatusCode status, string body, HealthStatus expected)
    {
        var form = string.Empty;
        var handler = new RoutingHandler(async (request, cancellationToken) =>
        {
            Assert.AreEqual(GoogleDefaults.TokenEndpoint, request.RequestUri!.AbsoluteUri);
            form = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(status, body);
        });

        var result = await RunSignInCheck<GoogleSignInHealthCheck>(handler, "Google", "Google", ("ClientId", "test-client"), ("ClientSecret", "test-secret"));

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.Contains("grant_type=authorization_code", form);
        Assert.Contains("client_id=test-client", form);
        Assert.DoesNotContain("test-secret", result.Description!);
    }

    [TestMethod]
    [DataRow("""{"error":"bad_verification_code","error_description":"The code passed is incorrect or expired."}""", HealthStatus.Healthy, DisplayName = "Credentials accepted")]
    [DataRow("""{"error":"incorrect_client_credentials","error_description":"The client_id and/or client_secret passed are incorrect."}""", HealthStatus.Degraded, DisplayName = "Wrong secret")]
    public async Task GitHubCheck_Should_JudgeTheTokenEndpointsAnswer(string body, HealthStatus expected)
    {
        HttpRequestMessage? sent = null;
        var handler = new RoutingHandler((request, _) =>
        {
            sent = request;
            return Task.FromResult(Json(HttpStatusCode.OK, body));
        });

        var result = await RunSignInCheck<GitHubSignInHealthCheck>(handler, "GitHub", "GitHub", ("ClientId", "test-client"), ("ClientSecret", "test-secret"));

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual("https://github.com/login/oauth/access_token", sent!.RequestUri!.AbsoluteUri);
        Assert.Contains("application/json", sent.Headers.Accept.ToString(), "GitHub answers a form unless asked for JSON.");
    }

    [TestMethod]
    public async Task GitHubCheck_Should_AskTheEnterpriseServer_WhenOneIsSet()
    {
        var handler = new RoutingHandler((_, _) => Task.FromResult(Json(HttpStatusCode.OK, """{"error":"bad_verification_code"}""")));

        var result = await RunSignInCheck<GitHubSignInHealthCheck>(handler, "GitHub", "GitHub", ("ClientId", "test-client"), ("ClientSecret", "test-secret"), ("EnterpriseDomain", "github.contoso.com"));

        Assert.AreEqual(HealthStatus.Healthy, result.Status, result.Description);
        Assert.AreEqual("https://github.contoso.com/login/oauth/access_token", handler.Requests.Single().AbsoluteUri);
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"token_type":"bearer","access_token":"AAAA"}""", HealthStatus.Healthy, DisplayName = "Token issued")]
    [DataRow(HttpStatusCode.Forbidden, """{"errors":[{"code":99,"message":"Unable to verify your credentials","label":"authenticity_token_error"}]}""", HealthStatus.Degraded, DisplayName = "Wrong secret")]
    public async Task TwitterCheck_Should_JudgeTheTokenEndpointsAnswer(HttpStatusCode status, string body, HealthStatus expected)
    {
        HttpRequestMessage? sent = null;
        var handler = new RoutingHandler((request, _) =>
        {
            sent = request;
            return Task.FromResult(Json(status, body));
        });

        var result = await RunSignInCheck<TwitterSignInHealthCheck>(handler, "Twitter", "Twitter", ("ConsumerKey", "test key"), ("ConsumerSecret", "test-secret"));

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.AreEqual("https://api.x.com/oauth2/token", sent!.RequestUri!.AbsoluteUri);
        Assert.AreEqual("Basic", sent.Headers.Authorization!.Scheme);
        Assert.AreEqual("test%20key:test-secret", Encoding.UTF8.GetString(Convert.FromBase64String(sent.Headers.Authorization.Parameter!)), "X wants each part url-encoded.");
        if (expected is HealthStatus.Degraded)
        {
            Assert.Contains("Unable to verify your credentials", result.Description!);
        }
    }

    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"access_token":"1234|AAAA","token_type":"bearer"}""", HealthStatus.Healthy, DisplayName = "Token issued")]
    [DataRow(HttpStatusCode.BadRequest, """{"error":{"message":"Error validating client secret.","type":"OAuthException","code":1}}""", HealthStatus.Degraded, DisplayName = "Wrong secret")]
    [DataRow(HttpStatusCode.BadRequest, """{"error":{"message":"Invalid Client ID","type":"OAuthException","code":101}}""", HealthStatus.Degraded, DisplayName = "Unknown app")]
    public async Task FacebookCheck_Should_JudgeTheTokenEndpointsAnswer(HttpStatusCode status, string body, HealthStatus expected)
    {
        var form = string.Empty;
        var handler = new RoutingHandler(async (request, cancellationToken) =>
        {
            Assert.AreEqual(FacebookDefaults.TokenEndpoint, request.RequestUri!.AbsoluteUri);
            Assert.AreEqual(HttpMethod.Post, request.Method, "The secret goes in the body, not in a url a proxy may log.");
            form = await request.Content!.ReadAsStringAsync(cancellationToken);
            return Json(status, body);
        });

        var result = await RunSignInCheck<FacebookSignInHealthCheck>(handler, "Facebook", "Facebook", ("AppId", "1234"), ("AppSecret", "test-secret"));

        Assert.AreEqual(expected, result.Status, result.Description);
        Assert.Contains("grant_type=client_credentials", form);
        Assert.DoesNotContain("test-secret", result.Description!);
    }

    [TestMethod]
    public async Task SignInChecks_Should_ReportDegraded_WhenTheProviderIsUnreachable()
    {
        var handler = new RoutingHandler((_, _) => throw new HttpRequestException("No route to host"));

        Assert.AreEqual(HealthStatus.Degraded, (await RunSignInCheck<GoogleSignInHealthCheck>(handler, "Google", "Google", ("ClientId", "test-client"))).Status);
        Assert.AreEqual(HealthStatus.Degraded, (await RunSignInCheck<GitHubSignInHealthCheck>(handler, "GitHub", "GitHub", ("ClientId", "test-client"))).Status);
        Assert.AreEqual(HealthStatus.Degraded, (await RunSignInCheck<TwitterSignInHealthCheck>(handler, "Twitter", "Twitter", ("ConsumerKey", "test-key"))).Status);
        Assert.AreEqual(HealthStatus.Degraded, (await RunSignInCheck<FacebookSignInHealthCheck>(handler, "Facebook", "Facebook", ("AppId", "1234"))).Status);
    }

    private async Task<HealthCheckResult> RunSignInCheck<THealthCheck>(RoutingHandler handler, string clientName, string section, params (string Key, string Value)[] settings)
        where THealthCheck : IHealthCheck
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.ToDictionary(setting => $"Authentication:{section}:{setting.Key}", setting => (string?)setting.Value))
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        AddHttpClient(services, handler, clientName);

        await using var serviceProvider = services.BuildServiceProvider();

        return await Run(ActivatorUtilities.CreateInstance<THealthCheck>(serviceProvider));
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

    private async Task<HealthCheckResult> RunAppleCheck(RoutingHandler handler, string? keyPath, string? privateKey = null)
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
            if (privateKey is not null)
            {
                options.UsePrivateKey(privateKey, new NullFileProvider());
            }
            else if (keyPath is not null)
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
