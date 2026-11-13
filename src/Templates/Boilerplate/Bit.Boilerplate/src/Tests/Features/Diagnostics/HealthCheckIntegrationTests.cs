//+:cnd:noEmit
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Infrastructure.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Boilerplate.Shared.Features.Diagnostic;
using Boilerplate.Client.Core.Infrastructure.Services.HttpMessageHandlers;
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
    /// <c>binStorage</c> disk-storage check. That check only needs 2 GB free at the root of the current directory's
    /// path, which on Linux is <c>/</c> even when the workspace sits on another mount. <c>ci.yml</c> makes sure of it
    /// with its <c>Free disk space</c> step, so the endpoint returns HTTP 200 with the
    /// default plain-text body <c>"Healthy"</c>. <c>/alive</c> is deliberately preferred over <c>/health</c> here:
    /// <c>/health</c> also runs
    /// the Hangfire check (which is racy right after the host starts, before the Hangfire server has written its
    /// first heartbeat) plus the DbContext and blob-storage checks, none of which are deterministic at this instant.
    /// The endpoint is anonymous, so no sign-in is required; the request is issued through the DI <see cref="HttpClient"/>
    /// resolved from a request scope (BaseAddress is the test server address).
    /// </summary>
    [TestMethod]
    public async Task Health_Should_ReportHealthy()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var client = server.CreateAppClient();
        var httpClient = client.HttpClient;

        using var response = await httpClient.GetAsync("alive", TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // The default MapHealthChecks response writer emits the overall HealthStatus name as plain text.
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, $"Unexpected status. Body: '{body}'.");
        Assert.AreEqual("Healthy", body.Trim());
    }

    /// <summary>
    /// <c>/healthz</c> needs <see cref="AppFeatures.System.Operations_View"/>, is never cached, and answers 503 when a
    /// check is Unhealthy, with the report in the body all the same.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_NeedTheFeature_AndDescribeEveryCheck()
    {
        await using var server = new AppTestServer();

        // The api's health report is the one under test.
        await server.Build(
            configureTestApiAppServices: services => services.AddHealthChecks().AddCheck("failing", () => HealthCheckResult.Unhealthy("Down on purpose", new InvalidOperationException("The test broke it"))))
            .Start(TestContext.CancellationToken);

        var healthzUrl = new Uri(server.ApiAppAddress, "healthz");

        using (var anonymousClient = server.CreateRawHttpClient())
        using (var anonymousResponse = await anonymousClient.GetAsync(healthzUrl, TestContext.CancellationToken))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode, "An anonymous caller must not read the report.");
        }

        await using (var userClient = server.CreateAppClient())
        {
            await TestAccountUtils.CreateAndSignIn(userClient, TestContext.CancellationToken);

            await Assert.ThrowsExactlyAsync<ForbiddenException>(
                () => userClient.HttpClient.GetAsync(healthzUrl, TestContext.CancellationToken),
                "A signed-in user without the feature must not read the report.");
        }

        await using var adminClient = server.CreateAppClient();
        await adminClient.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await GetReport(adminClient.HttpClient, healthzUrl);

        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode, "An Unhealthy report answers 503, the way /health does.");
        Assert.IsNotNull(response.Headers.CacheControl);
        Assert.IsTrue(response.Headers.CacheControl.NoStore, "The report must not be cached anywhere.");

        var report = await response.Content.ReadFromJsonAsync(
            adminClient.Services.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>(), TestContext.CancellationToken);

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
        Assert.IsNotNull(smtp.VerifiedAt, "A cached check says when it really ran.");
        Assert.AreEqual(TimeSpan.FromMinutes(5), smtp.CacheDuration);
        // The Operations page of app versions in users' hands reads the run from the data, by its older names.
        Assert.AreEqual(smtp.Duration, TimeSpan.Parse(smtp.Data["Duration"]!, CultureInfo.InvariantCulture));
        Assert.AreEqual(smtp.CacheDuration, TimeSpan.Parse(smtp.Data["CacheDuration"]!, CultureInfo.InvariantCulture));
        // The field is written to the millisecond, the data in full.
        Assert.IsLessThan(TimeSpan.FromMilliseconds(1), (smtp.VerifiedAt!.Value - DateTimeOffset.Parse(smtp.Data["CheckedAt"]!, CultureInfo.InvariantCulture)).Duration());
        Assert.IsFalse(smtp.Data.ContainsKey("VerifiedAt"));
    }

    private async Task<HttpResponseMessage> GetReport(HttpClient httpClient, Uri url)
    {
        using HttpRequestMessage request = new(HttpMethod.Get, url);
        request.Options.Set(new HttpRequestOptionsKey<HttpStatusCode[]>(RequestOptionNames.AllowedStatusCodes), [HttpStatusCode.ServiceUnavailable]);

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    /// <summary>
    /// <c>/healthz/v1</c> is the same report under the api version the controllers carry, behind the same feature.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_AnswerUnderTheApiVersionToo()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        var healthzUrl = new Uri(server.ApiAppAddress, "healthz/v1");

        using (var anonymousClient = server.CreateRawHttpClient())
        using (var anonymousResponse = await anonymousClient.GetAsync(healthzUrl, TestContext.CancellationToken))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode, "The versioned path must not be a way around the feature.");
        }

        await using var adminClient = server.CreateAppClient();
        await adminClient.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await GetReport(adminClient.HttpClient, healthzUrl);

        var report = await response.Content.ReadFromJsonAsync(
            adminClient.Services.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>(), TestContext.CancellationToken);

        Assert.IsNotNull(report);
        Assert.Contains("live", report.Entries["binStorage"].Tags);
    }

    /// <summary>
    /// The report says which build answered, on which instance, since when: a monitor polling it marks a deployment
    /// where <c>applicationVersion</c> (which ends with the commit in a Release build) changes, and a restart where it
    /// stays and <c>startedAt</c> moves. The JSON names are what such a monitor codes against.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_SayWhichBuildAnswered_AndSinceWhen()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        await using var adminClient = server.CreateAppClient();
        await adminClient.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await GetReport(adminClient.HttpClient, new Uri(server.ApiAppAddress, "healthz"));

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        // The test host is the entry assembly here, as the app's own host is in a deployment.
        var entryAssembly = Assembly.GetEntryAssembly();
        Assert.IsNotNull(entryAssembly);
        var informationalVersion = entryAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        Assert.IsNotNull(informationalVersion);

        Assert.AreEqual(informationalVersion.InformationalVersion, root.GetProperty("applicationVersion").GetString());
        Assert.AreEqual(Environment.MachineName, root.GetProperty("instanceName").GetString());
        Assert.IsTrue(root.TryGetProperty("startedAt", out _), $"No startedAt in {body}");

        var report = JsonSerializer.Deserialize(body, adminClient.Services.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>());
        Assert.IsNotNull(report);

        using var process = Process.GetCurrentProcess();
        var processStartedAt = new DateTimeOffset(process.StartTime.ToUniversalTime());

        Assert.AreEqual(TimeSpan.Zero, report.StartedAt.Offset, "In UTC, so instances in different zones compare.");
        Assert.IsLessThan(TimeSpan.FromSeconds(1), (report.StartedAt - processStartedAt).Duration(),
            $"startedAt is when the answering process started ({processStartedAt:O}), not when the report was made: {report.StartedAt:O}.");
        Assert.IsLessThanOrEqualTo(report.CheckedAt, report.StartedAt);
    }

    /// <summary>
    /// Each entry says what kind of dependency it watches, taken from the category tag it was registered with, so a
    /// monitor and the Operations page can group a check they don't know by name, one of the app's own included. The
    /// JSON name is what such a monitor codes against.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_SayWhatKindOfDependencyEachCheckWatches()
    {
        await using var server = new AppTestServer();

        await server.Build(configureTestApiAppServices: services => services.AddHealthChecks()
                .AddCheck("paymentGateway", () => HealthCheckResult.Healthy(), tags: [HealthCheckCategories.Integrations])
                .Describe("paymentGateway", "Payment gateway", "Money", "Authorizes and voids a one cent payment.")
                .AddCheck("untagged", () => HealthCheckResult.Healthy()))
            .Start(TestContext.CancellationToken);

        await using var adminClient = server.CreateAppClient();
        await adminClient.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await GetReport(adminClient.HttpClient, new Uri(server.ApiAppAddress, "healthz"));

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        using (var json = JsonDocument.Parse(body))
        {
            Assert.AreEqual("Integrations", json.RootElement.GetProperty("entries").GetProperty("paymentGateway").GetProperty("category").GetString(), body);
        }

        var report = JsonSerializer.Deserialize(body, adminClient.Services.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>());
        Assert.IsNotNull(report);

        foreach (var (name, entry) in report.Entries.Where(entry => entry.Key is not "untagged"))
        {
            Assert.IsFalse(string.IsNullOrEmpty(entry.Category), $"The '{name}' check has no category. Tag it with one of HealthCheckCategories.");
            Assert.AreNotEqual(name, entry.Title, $"The '{name}' check has no title. Describe it in HealthCheckDescriptions.");
            Assert.IsFalse(string.IsNullOrEmpty(entry.IconName), $"The '{name}' check has no icon.");
        }

        Assert.AreEqual("Database", report.Entries["AppDbContext"].Title, "A monitor shows the check the way the app names it.");
        Assert.AreEqual("Mail", report.Entries["smtp"].IconName);
        Assert.IsFalse(string.IsNullOrEmpty(report.Entries["smtp"].About));

        Assert.AreEqual("Platform", report.Entries["binStorage"].Category);
        Assert.AreEqual("Platform", report.Entries["AppDbContext"].Category);
        Assert.AreEqual("Messaging", report.Entries["smtp"].Category);
        //#if (redis == true)
        Assert.AreEqual("Platform", report.Entries["StackExchange.Redis_redis-persistent"].Category, "Aspire registers it without tags, so its category is added afterwards.");
        Assert.AreEqual("Platform", report.Entries["StackExchange.Redis_redis-cache"].Category, "Aspire registers it without tags, so its category is added afterwards.");
        //#endif

        var paymentGateway = report.Entries["paymentGateway"];
        Assert.AreEqual("Integrations", paymentGateway.Category);
        Assert.AreEqual("Payment gateway", paymentGateway.Title, "A check of the app's own is described next to its registration.");
        Assert.AreEqual("Money", paymentGateway.IconName);
        Assert.AreEqual("Authorizes and voids a one cent payment.", paymentGateway.About);
        Assert.Contains(HealthCheckCategories.Integrations, paymentGateway.Tags, "The category tag stays among the tags.");

        Assert.IsNull(report.Entries["untagged"].Category, "A check registered without a category tag has none.");
        Assert.AreEqual("untagged", report.Entries["untagged"].Title, "A check described nowhere goes by its registration name.");
        Assert.AreEqual(HealthCheckCategories.IconOf(null), report.Entries["untagged"].IconName);
    }

    /// <summary>
    /// A monitor given only the app's address finds the app's server here (See bit-app.json in Client.Web's
    /// wwwroot/.well-known, which a statically hosted front end serves as it is).
    /// </summary>
    [TestMethod]
    public async Task BitAppDocument_Should_NameTheServer_AndTheFrontEnd()
    {
        await using var server = new AppTestServer();

        await server.Build().Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient();
        var body = await httpClient.GetStringAsync(new Uri(server.WebAppAddress, ".well-known/bit-app.json"), TestContext.CancellationToken);

        using var json = JsonDocument.Parse(body);
        var document = json.RootElement;

        Assert.AreEqual("server", document.GetProperty("front_end").GetString(), body);
        Assert.IsTrue(Uri.TryCreate(document.GetProperty("server").GetString(), UriKind.Absolute, out var serverAddress), body);
        Assert.AreEqual("/manifest.json", document.GetProperty("manifest").GetString(), body);

        using var metadata = await httpClient.GetAsync(new Uri(serverAddress, ".well-known/oauth-protected-resource/healthz"), TestContext.CancellationToken);
        Assert.AreEqual(HttpStatusCode.OK, metadata.StatusCode, $"The server it names must be the one whose /healthz a monitor reads: {serverAddress}");
    }

    /// <summary>
    /// A missing configuration reports Degraded like a slow dependency, so /healthz says which of the two an entry is (See
    /// HealthReportEntryDto.IsNotConfigured): neither the Operations page nor a monitor has to read the description.
    /// </summary>
    [TestMethod]
    public async Task DetailedReport_Should_TellAMissingConfiguration_FromADegradedDependency()
    {
        await using var server = new AppTestServer();

        await server.Build(configureTestApiAppServices: services => services.AddHealthChecks()
                .AddNotConfiguredCheck("paymentGateway", HealthCheckCategories.Integrations, "PaymentGateway")
                .AddCheck("maps", () => HealthCheckResult.Degraded("Answered in 4 seconds."), tags: [HealthCheckCategories.Integrations]))
            .Start(TestContext.CancellationToken);

        await using var adminClient = server.CreateAppClient();
        await adminClient.AuthManager.SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await GetReport(adminClient.HttpClient, new Uri(server.ApiAppAddress, "healthz"));

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        using (var json = JsonDocument.Parse(body))
        {
            Assert.IsTrue(json.RootElement.GetProperty("entries").GetProperty("paymentGateway").GetProperty("isNotConfigured").GetBoolean(), body);
        }

        var report = JsonSerializer.Deserialize(body, adminClient.Services.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<HealthReportDto>());
        Assert.IsNotNull(report);

        Assert.AreEqual(HealthCheckStatus.Degraded, report.Entries["paymentGateway"].Status);
        Assert.IsTrue(report.Entries["paymentGateway"].IsNotConfigured);
        Assert.AreEqual("Integrations", report.Entries["paymentGateway"].Category, "A stand-in is grouped where the real check would be.");

        Assert.AreEqual(HealthCheckStatus.Degraded, report.Entries["maps"].Status);
        Assert.IsFalse(report.Entries["maps"].IsNotConfigured, "A slow dependency is configured.");
        Assert.IsFalse(report.Entries["binStorage"].IsNotConfigured);
    }

    /// <summary>
    /// The category of every check the template registers (See HealthCheckCategories). A check that isn't listed here
    /// fails <see cref="AssertCategory"/>, so a new one gets a category from the start.
    /// </summary>
    private static readonly Dictionary<string, string> expectedCategories = new()
    {
        ["binStorage"] = HealthCheckCategories.Platform,
        ["AppDbContext"] = HealthCheckCategories.Platform,
        ["hangfire"] = HealthCheckCategories.Platform,
        ["appCertificate"] = HealthCheckCategories.Platform,
        ["userProfileImages"] = HealthCheckCategories.Platform,
        //#if (redis == true)
        ["StackExchange.Redis_redis-persistent"] = HealthCheckCategories.Platform,
        ["StackExchange.Redis_redis-cache"] = HealthCheckCategories.Platform,
        //#endif
        //#if (cloudflare == true)
        ["cloudflare"] = HealthCheckCategories.Platform,
        //#endif
        ["smtp"] = HealthCheckCategories.Messaging,
        ["emailDelivery"] = HealthCheckCategories.Messaging,
        ["sms"] = HealthCheckCategories.Messaging,
        //#if (notification == true)
        ["firebase"] = HealthCheckCategories.Messaging,
        ["apns"] = HealthCheckCategories.Messaging,
        //#endif
        //#if (signalR == true)
        ["azureSignalR"] = HealthCheckCategories.Messaging,
        //#endif
        ["keycloakIdentity"] = HealthCheckCategories.Identity,
        ["googleSignIn"] = HealthCheckCategories.Identity,
        ["gitHubSignIn"] = HealthCheckCategories.Identity,
        ["twitterSignIn"] = HealthCheckCategories.Identity,
        ["facebookSignIn"] = HealthCheckCategories.Identity,
        ["entraId"] = HealthCheckCategories.Identity,
        ["appleSignIn"] = HealthCheckCategories.Identity,
        //#if (captcha == "reCaptcha")
        ["reCaptcha"] = HealthCheckCategories.Identity,
        //#endif
        //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
        ["aiChat"] = HealthCheckCategories.AI,
        ["aiEmbedding"] = HealthCheckCategories.AI,
        //#endif
        //#if (signalR == true)
        ["aiSpeechToText"] = HealthCheckCategories.AI,
        ["aiTextToSpeech"] = HealthCheckCategories.AI,
        ["aiRealtime"] = HealthCheckCategories.AI,
        //#endif
    };

    private static void AssertCategory(HealthCheckRegistration registration)
    {
        var name = registration.Name;

        Assert.IsTrue(expectedCategories.TryGetValue(name, out var expected), $"The '{name}' check has no entry in expectedCategories.");

        string[] categories = [.. registration.Tags.Where(tag => tag.StartsWith(HealthCheckCategories.TagPrefix, StringComparison.Ordinal))];

        Assert.HasCount(1, categories, $"The '{name}' check must carry exactly one category tag, one of HealthCheckCategories.");
        Assert.AreEqual(expected, categories[0], $"The '{name}' check is in the wrong category.");
    }

    /// <summary>
    /// A remote dependency must neither hang <c>/health</c> nor make it answer 503, so every check outside
    /// <c>localChecks</c> needs a timeout, reports Degraded and stays out of <c>/alive</c>. The optional ones are
    /// configured below, as development only registers them then.
    /// </summary>
    [TestMethod]
    public async Task ExternalHealthChecks_Should_BeBoundedAndNonFatal()
    {
        await using var server = new AppTestServer();

        await server.Build(configureTestConfiguration: configuration =>
        {
            // A real-looking SMTP server, as emailDelivery stays out where mail is caught (the shipped ethereal.email).
            configuration["ConnectionStrings:smtp"] = "Endpoint=smtp://smtp.example.com:587";
            configuration["Authentication:Keycloak:KeycloakUrl"] = "http://keycloak.invalid/";
            configuration["Authentication:Keycloak:Realm"] = "dev";
            configuration["Authentication:AzureAD:ClientId"] = "not-a-real-client";
            configuration["Authentication:Apple:ClientId"] = "not-a-real-client";
            configuration["Sms:Providers:Twilio:AccountSid"] = "not-a-real-sid";
            configuration["Sms:Providers:Twilio:AuthToken"] = "not-a-real-token";
            configuration["Sms:Providers:Twilio:From"] = "+15005550006";
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

        var registrations = server.ApiApp.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        string[] localChecks = ["binStorage", "AppDbContext", "hangfire", "appCertificate", "StackExchange.Redis_redis-persistent"];

        string[] expectedExternalChecks =
        [
            "userProfileImages", "smtp", "emailDelivery", "sms", "keycloakIdentity", "entraId", "appleSignIn",
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

        // With every optional check registered, each one, local ones included, carries its category.
        foreach (var registration in registrations)
        {
            AssertCategory(registration);
        }
    }

    //#if (redis == true)
    [TestMethod]
    public async Task RedisPersistentCheck_Should_ExistOnlyWhereServerApiRuns()
    {
        const string checkName = "StackExchange.Redis_redis-persistent";

        var sharedBuilder = WebApplication.CreateBuilder();
        sharedBuilder.AddServerSharedServices();

        await using var sharedServices = sharedBuilder.Services.BuildServiceProvider();
        var sharedRegistrations = sharedServices.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        Assert.DoesNotContain(r => r.Name == checkName, sharedRegistrations,
            "A standalone Server.Web calls AddServerSharedServices without having this connection, so the check would keep its /health at 503.");
        Assert.Contains(r => r.Name == "StackExchange.Redis_redis-cache", sharedRegistrations);

        await using var server = new AppTestServer();
        server.Build();

        var apiRegistrations = server.ApiApp.Services.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        Assert.Contains(r => r.Name == checkName, apiRegistrations, "Hangfire and the distributed lock of Server.Api depend on this connection.");
    }

    //#endif
    /// <summary>
    /// Outside development, an unconfigured dependency reports Degraded and names its settings. Only the services are
    /// built, which leaves <c>AppEnvironment</c>, shared by the parallel tests, untouched.
    /// </summary>
    [TestMethod]
    public async Task UnconfiguredDependencies_Should_ReportDegraded_OutsideDevelopment()
    {
        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        // The checks are Server.Api's (See AddServerApiHealthChecks), and so is the appsettings.json they are built with.
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = typeof(Server.Api.Program).Assembly.GetName().Name,
            ContentRootPath = AppTestServer.ProjectDirectoryOf(typeof(Server.Api.Program).Assembly.GetName().Name!)
        });

        builder.Configuration.AddSharedConfigurations();
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#else
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            EnvironmentName = Environments.Production,
            ApplicationName = typeof(Server.Web.Program).Assembly.GetName().Name
        });

        builder.Configuration.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");
        //#endif

        string[] expectedChecks =
        [
            "sms", "keycloakIdentity", "entraId", "appleSignIn", "googleSignIn", "gitHubSignIn", "twitterSignIn", "facebookSignIn",
            //#if (cloudflare == true)
            "cloudflare",
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
            "Sms:Providers:Twilio:AccountSid", "KEYCLOAK_HTTP", "Authentication:Keycloak:KeycloakUrl", "Authentication:AzureAD:ClientId",
            "Authentication:Google:ClientId", "Authentication:GitHub:ClientId", "Authentication:Twitter:ConsumerKey", "Authentication:Facebook:AppId",
            "Authentication:Apple:ClientId", "Cloudflare:ApiToken", "Azure:SignalR:ConnectionString", "AdsPushFirebase:PrivateKey",
            "AdsPushAPNS:P8PrivateKey", "AI:OpenAI:ChatApiKey", "AI:OpenAI:EmbeddingApiKey", "AI:HuggingFace:EmbeddingEndpoint",
            "AI:OpenAI:SpeechToTextApiKey", "AI:OpenAI:TextToSpeechApiKey", "AI:OpenAI:RealtimeApiKey"
        })
        {
            builder.Configuration[key] = "";
        }

        //#if (api == "Standalone")
        //#if (IsInsideProjectTemplate)
        /*
        //#endif
        builder.Services.AddSharedProjectServices(builder.Configuration);
        Server.Api.Program.AddServerApiProjectServices(builder);
        //#if (IsInsideProjectTemplate)
        */
        //#endif
        //#else
        builder.AddServerWebProjectServices();
        //#endif

        await using var serviceProvider = builder.Services.BuildServiceProvider();
        var registrations = serviceProvider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        foreach (var name in expectedChecks)
        {
            var registration = registrations.SingleOrDefault(r => r.Name == name);
            Assert.IsNotNull(registration, $"No '{name}' health check is registered outside development. See AddServerApiHealthChecks.");

            var result = await registration.Factory(serviceProvider).CheckHealthAsync(new HealthCheckContext { Registration = registration }, TestContext.CancellationToken);

            Assert.AreEqual(HealthStatus.Degraded, result.Status, $"'{name}': {result.Description}");
            Assert.Contains("is not configured", result.Description!, $"'{name}' must say what is missing.");
            Assert.Contains(HealthCheckTags.NotConfigured, registration.Tags, $"/healthz must report '{name}' as not configured, not as failing.");

            // A stand-in is grouped where the real check would be.
            AssertCategory(registration);
        }
        //#if (signalR == true)

        Assert.DoesNotContain(r => r.Name is "azureSignalR", registrations, "Without Azure SignalR the app serves SignalR itself, so a missing connection string is no setup gap.");
        //#endif
    }

    /// <summary>
    /// A failing check must report its registration's FailureStatus. Sms is not configured here, so the check throws
    /// without a network request.
    /// </summary>
    [TestMethod]
    public async Task TwilioHealthCheck_Should_ReportTheRegisteredFailureStatus()
    {
        await using var serviceProvider = new ServiceCollection().AddHttpClient().AddSingleton(new ServerApiSettings()).BuildServiceProvider();

        var healthCheck = ActivatorUtilities.CreateInstance<TwilioHealthCheck>(serviceProvider);

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

        await server.Build(
            configureTestServices: services => services.Replace(ServiceDescriptor.Singleton<IChatClient>(chatClient)),
            configureTestConfiguration: configuration => configuration["AI:OpenAI:ChatApiKey"] = "fake-key-never-used-by-this-test")
            .Start(TestContext.CancellationToken);

        var healthCheckService = server.ApiApp.Services.GetRequiredService<HealthCheckService>();

        async Task<HealthReportEntry> CheckAIChat() => (await healthCheckService.CheckHealthAsync(r => r.Name is "aiChat", TestContext.CancellationToken)).Entries["aiChat"];

        Assert.AreEqual(HealthStatus.Healthy, (await CheckAIChat()).Status);
        Assert.AreEqual(HealthStatus.Healthy, (await CheckAIChat()).Status);
        Assert.AreEqual(1, chatClient.Calls, "A healthy answer must be reused within the cache period instead of paying for another one.");
        Assert.IsNotNull(chatClient.LastOptions);
        Assert.AreEqual(AIChatAgentHealthCheck.MaxOutputTokens, chatClient.LastOptions.MaxOutputTokens);
        Assert.IsNotNull(chatClient.LastMessages);
        Assert.Contains(m => m.Role == ChatRole.User, chatClient.LastMessages, "The agent must actually send a message.");

        chatClient.Failure = new HttpRequestException("insufficient_quota");
        server.ApiApp.Services.GetRequiredService<CachedHealthCheck.ResultsStore>().Clear(); // As if the cache period had passed.

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

        await server.Build(
            configureTestServices: services => services.Replace(ServiceDescriptor.Singleton(embeddingGenerator)),
            configureTestConfiguration: configuration => configuration["AI:OpenAI:EmbeddingApiKey"] = "fake-key-never-used-by-this-test")
            .Start(TestContext.CancellationToken);

        var report = await server.ApiApp.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(r => r.Name is "aiEmbedding", TestContext.CancellationToken);

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
        await server.Build(
            configureTestServices: services => services.AddHttpClient<ResponseCacheService>().ConfigurePrimaryHttpMessageHandler(() => handler),
            configureTestConfiguration: configuration =>
            {
                configuration["Cloudflare:ApiToken"] = "not-a-real-token";
                configuration["Cloudflare:ZoneIds:0"] = "test-zone";
            })
            .Start(TestContext.CancellationToken);

        var report = await server.ApiApp.Services.GetRequiredService<HealthCheckService>().CheckHealthAsync(r => r.Name is "cloudflare", TestContext.CancellationToken);

        Assert.AreEqual(expectedStatus, report.Entries["cloudflare"].Status, report.Entries["cloudflare"].Exception?.Message);
        Assert.IsNotEmpty(purgedTags);
        Assert.IsTrue(purgedTags.All(tag => tag.StartsWith("health-check-", StringComparison.Ordinal)));
    }
    //#endif
}
