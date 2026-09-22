//+:cnd:noEmit
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Shared.Features.Diagnostic;
//#if (cloudflare == true)
using System.Text;
using System.Text.Json.Nodes;
using Boilerplate.Server.Api.Infrastructure.Services;
//#endif
//#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection.Extensions;
//#endif

namespace Boilerplate.Tests.Features.Diagnostics;

[TestClass, TestCategory("IntegrationTest")]
public partial class HealthCheckIntegrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Verifies the application exposes a working liveness health-check endpoint. <c>MapAppHealthChecks</c>
    /// (See <c>WebApplicationExtensions.MapAppHealthChecks</c>) maps <c>GET /alive</c> with a predicate that runs
    /// only the checks tagged <c>"live"</c> - which, per <c>AddDefaultHealthChecks</c>, is exactly the single
    /// <c>binStorage</c> disk-storage check. That check is deterministically healthy in any test/CI environment
    /// (it only requires free disk space), so the endpoint returns HTTP 200 with the default plain-text body
    /// <c>"Healthy"</c>. <c>/alive</c> is deliberately preferred over <c>/health</c> here: <c>/health</c> also runs
    /// the Hangfire check (which is racy right after the host starts, before the Hangfire server has written its
    /// first heartbeat) plus the DbContext and blob-storage checks, none of which are deterministic at this instant.
    /// The endpoint is anonymous, so no sign-in is required; the request is issued through the DI <see cref="HttpClient"/>
    /// resolved from a request scope (BaseAddress is the test server address).
    /// </summary>
    [TestMethod]
    public async Task Health_Should_ReportHealthy()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        using var response = await httpClient.GetAsync("alive", TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // The default MapHealthChecks response writer emits the overall HealthStatus name as plain text.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Unexpected status. Body: '{body}'.");
        Assert.AreEqual("Healthy", body.Trim());
    }

    /// <summary>
    /// <c>/healthz</c> needs <see cref="AppFeatures.System.Operations_View"/>, is never cached, and answers 200 even
    /// when a check is Unhealthy.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_NeedTheFeature_AndDescribeEveryCheck()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddHealthChecks().AddCheck("failing", () => HealthCheckResult.Unhealthy("Down on purpose", new InvalidOperationException("The test broke it")));
        }).Start(TestContext.CancellationToken);

        using (var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress })
        using (var anonymousResponse = await anonymousClient.GetAsync("healthz", TestContext.CancellationToken))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode, "An anonymous caller must not read the report.");
        }

        await using (var userScope = server.WebApp.Services.CreateAsyncScope())
        {
            await TestAccountUtils.CreateAndSignIn(server, userScope, TestContext.CancellationToken);

            await Assert.ThrowsExactlyAsync<ForbiddenException>(
                () => userScope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync("healthz", TestContext.CancellationToken),
                "A signed-in user without the feature must not read the report.");
        }

        await using var adminScope = server.WebApp.Services.CreateAsyncScope();
        await adminScope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await adminScope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync("healthz", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, "An Unhealthy report still answers 200.");
        Assert.IsNotNull(response.Headers.CacheControl);
        Assert.IsTrue(response.Headers.CacheControl.NoStore, "The report must not be cached anywhere.");

        var report = await response.Content.ReadFromJsonAsync(
            adminScope.ServiceProvider.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>(), TestContext.CancellationToken);

        Assert.IsNotNull(report);
        Assert.AreEqual(HealthCheckStatus.Unhealthy, report.Status);

        var failing = report.Entries["failing"];
        Assert.AreEqual(HealthCheckStatus.Unhealthy, failing.Status);
        Assert.AreEqual("Down on purpose", failing.Description);
        Assert.Contains("The test broke it", failing.Exception!);

        Assert.AreEqual(HealthCheckStatus.Unhealthy, report.Entries["AppDbContext"].FailureStatus, "The database takes the instance out of rotation.");
        Assert.Contains("live", report.Entries["binStorage"].Tags);

        var smtp = report.Entries["smtp"];
        Assert.AreEqual(HealthCheckStatus.Degraded, smtp.FailureStatus);
        Assert.AreEqual(TimeSpan.FromSeconds(10), smtp.Timeout);
        Assert.IsTrue(smtp.Data.ContainsKey("CheckedAt"), "A cached check says when it really ran.");
    }

    /// <summary>
    /// <c>/healthz/v1</c> is the same report under the api version the controllers carry, behind the same feature.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_AnswerUnderTheApiVersionToo()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using (var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress })
        using (var anonymousResponse = await anonymousClient.GetAsync("healthz/v1", TestContext.CancellationToken))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode, "The versioned path must not be a way around the feature.");
        }

        await using var adminScope = server.WebApp.Services.CreateAsyncScope();
        await adminScope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await adminScope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync("healthz/v1", TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var report = await response.Content.ReadFromJsonAsync(
            adminScope.ServiceProvider.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>(), TestContext.CancellationToken);

        Assert.IsNotNull(report);
        Assert.Contains("live", report.Entries["binStorage"].Tags);
    }

    /// <summary>
    /// A remote dependency must neither hang <c>/health</c> nor make it answer 503, so every check outside
    /// <c>localChecks</c> needs a timeout, reports Degraded and stays out of <c>/alive</c>. The optional ones are
    /// configured below, as development only registers them then (Sms aside: it would initialize the static Twilio client).
    /// </summary>
    [TestMethod]
    public async Task ExternalHealthChecks_Should_BeBoundedAndNonFatal()
    {
        await using var server = new AppTestServer();

        await server.Build(s => s.AddIntegrationApiOnlyTestsServices(), configuration =>
        {
            configuration["Authentication:Keycloak:KeycloakUrl"] = "http://keycloak.invalid/";
            configuration["Authentication:Keycloak:Realm"] = "dev";
            configuration["Authentication:AzureAD:ClientId"] = "not-a-real-client";
            configuration["Authentication:Apple:ClientId"] = "not-a-real-client";
            //#if (cloudflare == true)
            configuration["Cloudflare:ApiToken"] = "not-a-real-token";
            configuration["Cloudflare:ZoneIds:0"] = "not-a-real-zone";
            //#endif
            //#if (notification == true)
            configuration["AdsPushFirebase:PrivateKey"] = "not-a-real-key";
            configuration["AdsPushAPNS:P8PrivateKey"] = "not-a-real-key";
            //#endif
            //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
            configuration["AI:OpenAI:ChatApiKey"] = "not-a-real-key";
            configuration["AI:OpenAI:EmbeddingApiKey"] = "not-a-real-key";
            //#endif
            //#if (signalR == true)
            configuration["AI:OpenAI:SpeechToTextApiKey"] = "not-a-real-key";
            configuration["AI:OpenAI:TextToSpeechApiKey"] = "not-a-real-key";
            configuration["AI:OpenAI:RealtimeApiKey"] = "not-a-real-key";
            //#endif
        }).Start(TestContext.CancellationToken);

        var registrations = server.WebApp.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        string[] localChecks = ["binStorage", "AppDbContext", "hangfire", "appCertificate", "StackExchange.Redis_redis-persistent"];

        string[] expectedExternalChecks =
        [
            "userProfileImages", "smtp", "keycloakIdentity", "entraId", "appleSignIn",
            //#if (captcha == "reCaptcha")
            "reCaptcha",
            //#endif
            //#if (cloudflare == true)
            "cloudflare",
            //#endif
            //#if (redis == true)
            "StackExchange.Redis_redis-cache",
            //#endif
            //#if (notification == true)
            "firebase", "apns",
            //#endif
            //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
            "aiChat", "aiEmbedding",
            //#endif
            //#if (signalR == true)
            "aiSpeechToText", "aiTextToSpeech", "aiRealtime",
            //#endif
        ];

        foreach (var name in expectedExternalChecks)
        {
            Assert.Contains(r => r.Name == name, registrations, $"No '{name}' health check is registered. See AddServerApiHealthChecks.");
        }

        foreach (var registration in registrations.Where(r => localChecks.Contains(r.Name) is false))
        {
            var name = registration.Name;

            Assert.AreNotEqual(Timeout.InfiniteTimeSpan, registration.Timeout,
                $"The '{name}' check calls out to a third party, so it needs a timeout - without one a hung socket hangs /health with it.");

            Assert.AreEqual(HealthStatus.Degraded, registration.FailureStatus,
                $"A failing '{name}' check must not make /health answer 503 and drain the instance from rotation.");

            Assert.DoesNotContain("live", registration.Tags,
                $"The '{name}' check must stay out of /alive, which is the liveness probe and must not depend on anything remote.");
        }

        // The control: the one check that *is* meant to gate liveness still does.
        Assert.Contains("live", registrations.Single(r => r.Name == "binStorage").Tags);
    }

    /// <summary>
    /// Outside development, an unconfigured dependency reports Degraded and names its settings. Only the services are
    /// built, which leaves <c>AppEnvironment</c>, shared by the parallel tests, untouched.
    /// </summary>
    [TestMethod]
    public async Task UnconfiguredDependencies_Should_ReportDegraded_OutsideDevelopment()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = typeof(Server.Web.Program).Assembly.GetName().Name
        });

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        string[] expectedChecks =
        [
            "sms", "keycloakIdentity", "entraId", "appleSignIn", "googleSignIn", "gitHubSignIn", "twitterSignIn", "facebookSignIn",
            //#if (cloudflare == true)
            "cloudflare",
            //#endif
            //#if (signalR == true)
            "azureSignalR",
            //#endif
            //#if (notification == true)
            "firebase", "apns",
            //#endif
            //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
            "aiChat", "aiEmbedding",
            //#endif
            //#if (signalR == true)
            "aiSpeechToText", "aiTextToSpeech", "aiRealtime",
            //#endif
        ];

        // Blank, whatever the machine's environment variables hold.
        foreach (var key in new[]
        {
            "Sms:TwilioAccountSid", "KEYCLOAK_HTTP", "Authentication:Keycloak:KeycloakUrl", "Authentication:AzureAD:ClientId",
            "Authentication:Google:ClientId", "Authentication:GitHub:ClientId", "Authentication:Twitter:ConsumerKey", "Authentication:Facebook:AppId",
            "Authentication:Apple:ClientId", "Cloudflare:ApiToken", "Azure:SignalR:ConnectionString", "AdsPushFirebase:PrivateKey",
            "AdsPushAPNS:P8PrivateKey", "AI:OpenAI:ChatApiKey", "AI:OpenAI:EmbeddingApiKey", "AI:HuggingFace:EmbeddingEndpoint",
            "AI:OpenAI:SpeechToTextApiKey", "AI:OpenAI:TextToSpeechApiKey", "AI:OpenAI:RealtimeApiKey"
        })
        {
            builder.Configuration[key] = "";
        }

        builder.AddServerWebProjectServices();

        await using var serviceProvider = builder.Services.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        foreach (var name in expectedChecks)
        {
            var registration = registrations.SingleOrDefault(r => r.Name == name);
            Assert.IsNotNull(registration, $"No '{name}' health check is registered outside development. See AddServerApiHealthChecks.");

            var result = await registration.Factory(serviceProvider).CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);

            Assert.AreEqual(HealthStatus.Degraded, result.Status, $"'{name}': {result.Description}");
            Assert.Contains("is not configured", result.Description!, $"'{name}' must say what is missing.");
        }
    }

    /// <summary>
    /// A failing check must report its registration's FailureStatus. <c>TwilioClient.Init</c> is never called in
    /// tests, so the SDK throws without a network request.
    /// </summary>
    [TestMethod]
    public async Task TwilioHealthCheck_Should_ReportTheRegisteredFailureStatus()
    {
        var healthCheck = new TwilioHealthCheck();

        var registration = new HealthCheckRegistration("sms", healthCheck, failureStatus: HealthStatus.Degraded, tags: null);

        var result = await healthCheck.CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);

        Assert.AreEqual(HealthStatus.Degraded, result.Status,
            $"The check must honour its registration's FailureStatus. Description: '{result.Description}'.");
    }
    //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")

    /// <summary>
    /// <c>aiChat</c> gets a real, short answer, reused while healthy and asked again on every probe while failing.
    /// </summary>
    [TestMethod]
    public async Task AIChatHealthCheck_Should_GetAnAnswer_AndOnlyRepeatItAfterAFailure()
    {
        var chatClient = new ScriptedChatClient();

        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient));
        },
        configuration => configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-this-test").Start(TestContext.CancellationToken);

        var healthCheckService = server.WebApp.Services.GetRequiredService<HealthCheckService>();

        async Task<HealthReportEntry> CheckAIChat() => (await healthCheckService.CheckHealthAsync(r => r.Name is "aiChat", TestContext.CancellationToken)).Entries["aiChat"];

        Assert.AreEqual(HealthStatus.Healthy, (await CheckAIChat()).Status);
        Assert.AreEqual(HealthStatus.Healthy, (await CheckAIChat()).Status);
        Assert.AreEqual(1, chatClient.Calls, "A healthy answer must be reused within the cache period instead of paying for another one.");
        Assert.IsNotNull(chatClient.LastOptions);
        Assert.AreEqual(AIChatAgentHealthCheck.MaxOutputTokens, chatClient.LastOptions.MaxOutputTokens);
        Assert.IsNotNull(chatClient.LastMessages);
        Assert.Contains(m => m.Role == ChatRole.User, chatClient.LastMessages, "The agent must actually send a message.");

        chatClient.Failure = new HttpRequestException("insufficient_quota");
        server.WebApp.Services.GetRequiredService<CachedHealthCheck.ResultsStore>().Clear(); // As if the cache period had passed.

        Assert.AreEqual(HealthStatus.Degraded, (await CheckAIChat()).Status);
        Assert.AreEqual(HealthStatus.Degraded, (await CheckAIChat()).Status);
        Assert.AreEqual(3, chatClient.Calls, "A failing answer must never be reused.");

        chatClient.Failure = null;

        Assert.AreEqual(HealthStatus.Healthy, (await CheckAIChat()).Status, "A recovery must show up on the very next probe.");
    }

    /// <summary>
    /// <c>aiEmbedding</c> embeds one word with the DI generator, whichever provider built it, and needs a vector back.
    /// </summary>
    [TestMethod]
    [DataRow(3, HealthStatus.Healthy, DisplayName = "A vector")]
    [DataRow(0, HealthStatus.Degraded, DisplayName = "An empty vector")]
    [DataRow(-1, HealthStatus.Degraded, DisplayName = "No embedding at all")]
    public async Task AIEmbeddingHealthCheck_Should_NeedAVector(int dimensions, HealthStatus expectedStatus)
    {
        IEnumerable<string>? embeddedValues = null;
        var embeddingGenerator = A.Fake<IEmbeddingGenerator<string, Embedding<float>>>();
        A.CallTo(() => embeddingGenerator.GenerateAsync(A<IEnumerable<string>>._, A<EmbeddingGenerationOptions?>._, A<CancellationToken>._))
            .ReturnsLazily((IEnumerable<string> values, EmbeddingGenerationOptions? _, CancellationToken _) =>
            {
                embeddedValues = [.. values];
                return Task.FromResult(dimensions < 0
                    ? new GeneratedEmbeddings<Embedding<float>>()
                    : new GeneratedEmbeddings<Embedding<float>>([new Embedding<float>(new float[dimensions])]));
            });

        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.Replace(ServiceDescriptor.Singleton(embeddingGenerator));
        },
        configuration => configuration["AI:OpenAI:EmbeddingApiKey"] = "fake-key-never-used-by-this-test").Start(TestContext.CancellationToken);

        var report = await server.WebApp.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(r => r.Name is "aiEmbedding", TestContext.CancellationToken);

        Assert.AreEqual(expectedStatus, report.Entries["aiEmbedding"].Status);
        Assert.IsNotNull(embeddedValues);
        Assert.HasCount(1, embeddedValues);
    }

    private sealed class ScriptedChatClient : IChatClient
    {
        public int Calls { get; private set; }

        public Exception? Failure { get; set; }

        public ChatOptions? LastOptions { get; private set; }

        public ChatMessage[]? LastMessages { get; private set; }

        public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastOptions = options;
            LastMessages = [.. messages];

            return Failure is null
                ? Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "OK")))
                : Task.FromException<ChatResponse>(Failure);
        }

        public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public object? GetService(Type serviceType, object? serviceKey = null) => null;

        public void Dispose() { }
    }
    //#endif
    //#if (cloudflare == true)

    /// <summary>
    /// <c>cloudflare</c> purges a tag nothing carries, and only a confirmed purge is healthy.
    /// </summary>
    [TestMethod]
    [DataRow(HttpStatusCode.OK, """{"success":true}""", HealthStatus.Healthy, DisplayName = "Purged")]
    [DataRow(HttpStatusCode.OK, """{"success":false,"errors":[{"code":10000,"message":"Authentication error"}]}""", HealthStatus.Degraded, DisplayName = "Rejected")]
    [DataRow(HttpStatusCode.Forbidden, "{}", HealthStatus.Degraded, DisplayName = "Forbidden")]
    public async Task CloudflareHealthCheck_Should_PurgeATagNothingCarries(HttpStatusCode status, string body, HealthStatus expectedStatus)
    {
        var purgedTags = new List<string>();
        var handler = new Identity.ExternalSignInHealthChecksTests.RoutingHandler(async (request, cancellationToken) =>
        {
            Assert.AreEqual("https://api.cloudflare.com/client/v4/zones/test-zone/purge_cache", request.RequestUri!.AbsoluteUri);
            purgedTags.AddRange(JsonNode.Parse(await request.Content!.ReadAsStringAsync(cancellationToken))!["tags"]!.AsArray().Select(tag => tag!.GetValue<string>()));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        });

        await using var server = new AppTestServer();
        await server.Build(services =>
        {
            services.AddIntegrationApiOnlyTestsServices();
            services.AddHttpClient<ResponseCacheService>().ConfigurePrimaryHttpMessageHandler(() => handler);
        }, configuration =>
        {
            configuration["Cloudflare:ApiToken"] = "not-a-real-token";
            configuration["Cloudflare:ZoneIds:0"] = "test-zone";
        }).Start(TestContext.CancellationToken);

        var report = await server.WebApp.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(r => r.Name is "cloudflare", TestContext.CancellationToken);

        Assert.AreEqual(expectedStatus, report.Entries["cloudflare"].Status, report.Entries["cloudflare"].Exception?.Message);
        Assert.IsNotEmpty(purgedTags);
        Assert.IsTrue(purgedTags.All(tag => tag.StartsWith("health-check-", StringComparison.Ordinal)));
    }
    //#endif
}
