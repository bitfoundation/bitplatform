//+:cnd:noEmit
using System.Net;
using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary><c>GET /api/v1/Diagnostic/GetDeploymentConfiguration</c>, behind the operations page's Configuration section.</summary>
[TestClass, TestCategory("IntegrationTest")]
public partial class DeploymentConfigurationTests
{
    public TestContext TestContext { get; set; } = default!;

    private const string Url = "api/v1/Diagnostic/GetDeploymentConfiguration";

    [TestMethod]
    public async Task DeploymentConfiguration_Should_NeedTheFeature()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using (var anonymousClient = new HttpClient { BaseAddress = server.WebAppServerAddress })
        using (var anonymousResponse = await anonymousClient.GetAsync(Url, TestContext.CancellationToken))
        {
            Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode, "An anonymous caller must not read the configuration.");
        }

        await using var userScope = server.WebApp.Services.CreateAsyncScope();

        await TestAccountUtils.CreateAndSignIn(server, userScope, TestContext.CancellationToken);

        await Assert.ThrowsExactlyAsync<ForbiddenException>(
            () => userScope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync(Url, TestContext.CancellationToken),
            "A signed-in user without the feature must not read the configuration.");
    }

    /// <summary>
    /// A credential is reported as a boolean, never as itself. Asserted against the deployment's real connection
    /// strings, so a value added to <c>DeploymentConfigurationDto</c> that carries one fails here.
    /// </summary>
    [TestMethod]
    public async Task DeploymentConfiguration_Should_CarryNoSecret()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var adminScope = server.WebApp.Services.CreateAsyncScope();

        await adminScope.ServiceProvider.GetRequiredService<AuthManager>().SignIn(new()
        {
            Email = TestData.DefaultTestEmail,
            Password = TestData.DefaultTestPassword
        }, TestContext.CancellationToken);

        using var response = await adminScope.ServiceProvider.GetRequiredService<HttpClient>().GetAsync(Url, TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        var configuration = server.WebApp.Services.GetRequiredService<IConfiguration>();

        // Every connection string this deployment holds, plus the parts that identify one wherever it is spelled out.
        // A name is fine - "sqlite" is a name - so only the values are looked for.
        var secrets = configuration.GetSection("ConnectionStrings").GetChildren()
            .Select(entry => entry.Value)
            .Where(value => string.IsNullOrWhiteSpace(value) is false)
            .Concat(["Password=", "Pwd=", "AccountKey=", "AccessKey=", "SecretKey=", "SharedAccessKey", "InstrumentationKey"]);

        foreach (var secret in secrets)
        {
            Assert.DoesNotContain(secret!, body, StringComparison.OrdinalIgnoreCase,
                $"The deployment configuration carries a secret: '{secret}'.");
        }

        var report = await response.Content.ReadFromJsonAsync(
            adminScope.ServiceProvider.GetRequiredService<JsonSerializerOptions>().GetTypeInfo<DeploymentConfigurationDto>(), TestContext.CancellationToken);

        Assert.IsNotNull(report);
        Assert.IsFalse(string.IsNullOrWhiteSpace(report.Environment), "The environment is what the rest of the report is read against.");
    }
}
