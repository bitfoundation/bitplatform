using System.Net;

namespace Boilerplate.Tests.Features.ForceUpdate;

/// <summary>
/// <b>Regression test for BP-079.</b>
/// <para>
/// <c>ForceUpdateMiddleware</c> runs on <b>every</b> request, before authentication, and used to parse two
/// request headers with throwing APIs and no guards: <c>Headers["X-App-Platform"].Single()</c>,
/// <c>Enum.Parse&lt;AppPlatformType&gt;(...)</c>, <c>Version.Parse(...)</c> and <c>SupportedAppVersions!</c>.
/// Every one of them is fed directly from headers any anonymous caller controls, so each turned a one-line
/// request into a 500 plus a <c>LogCritical</c> record - free, unauthenticated log noise on any endpoint.
/// </para>
/// <para>
/// The <c>Single()</c> case was the sharpest: the middleware only checked that <c>X-App-Version</c> was present,
/// then read <c>X-App-Platform</c> as though the presence of the first guaranteed the second. Sending just
/// <c>X-App-Version</c> - what a curl, a health prober or a partial client does - was enough.
/// </para>
/// <para>
/// The middleware's job is to force an update, not to validate headers, so every read now fails <b>open</b>:
/// a caller that cannot state a usable version and platform is not a client the check applies to.
/// </para>
/// <para>
/// <b>Why a bare <see cref="HttpClient"/> here</b>, unlike the other integration tests: the app's own
/// <c>RequestHeadersDelegatingHandler</c> <i>adds</i> <c>X-App-Version</c> and <c>X-App-Platform</c> to every
/// internal request, so the DI-resolved client cannot express "send exactly this header" - the value under test
/// would arrive joined with the real one. These two headers are the input, so they have to be the only ones.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ForceUpdateHeaderHardeningTests
{
    /// <summary>
    /// Each row is a header combination an anonymous caller can send. The endpoint is an ordinary anonymous GET
    /// whose real answer is 404, and that 404 is the assertion: the client version headers must not change it
    /// into a server fault. The last row is the control - a well-formed pair must reach the same 404, so a green
    /// result cannot come from the middleware simply rejecting everything.
    /// </summary>
    [TestMethod]
    [DataRow("1.0.0", null, DisplayName = "X-App-Platform missing - used to be Single() on an empty StringValues")]
    [DataRow("1.0.0", "NotAPlatform", DisplayName = "unknown platform - used to be Enum.Parse")]
    [DataRow("1.0.0", "999", DisplayName = "numeric platform - Enum.TryParse accepts it, Enum.IsDefined must not")]
    [DataRow("1.0.0", "Linux", DisplayName = "Linux - a real platform with no configured minimum version")]
    [DataRow("not-a-version", "Web", DisplayName = "unparsable version - used to be Version.Parse")]
    [DataRow("1.0.0, 2.0.0", "Web", DisplayName = "repeated X-App-Version, which Kestrel joins into one comma-separated value")]
    [DataRow("", "Web", DisplayName = "empty version")]
    [DataRow("1.0.0", "web", DisplayName = "CONTROL: a valid pair, lower-cased - accepted and reaches the same 404")]
    public async Task AMalformedClientVersionHeader_Should_NotFaultTheRequest(string appVersion, string? appPlatform)
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var response = await SendAttachmentRequest(server, appVersion, appPlatform);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            $"A header the caller controls must not turn the real 404 into a server fault. Body: {body}");
    }

    /// <summary>
    /// The check itself must still work: a client below the configured minimum is refused. Without this, "never
    /// faults" would also be satisfied by a middleware that does nothing at all - which is exactly what a
    /// too-eager fail-open produces.
    /// </summary>
    [TestMethod]
    public async Task AClientBelowTheMinimumVersion_Should_StillBeRefused()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices(),
            configureTestConfigurations: configuration => configuration["SupportedAppVersions:MinimumSupportedWebAppVersion"] = "9.9.9")
            .Start(TestContext.CancellationToken);

        using var response = await SendAttachmentRequest(server, appVersion: "1.0.0", appPlatform: nameof(AppPlatformType.Web));

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, $"An out-of-date client must be refused. Body: {body}");

        Assert.Contains(nameof(ClientNotSupportedException), body, StringComparison.Ordinal);
    }

    private async Task<HttpResponseMessage> SendAttachmentRequest(AppTestServer server, string appVersion, string? appPlatform)
    {
        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Attachment/GetAttachment/{Guid.NewGuid()}/UserProfileImageSmall");

        request.Headers.TryAddWithoutValidation("X-App-Version", appVersion);

        if (appPlatform is not null)
        {
            request.Headers.TryAddWithoutValidation("X-App-Platform", appPlatform);
        }

        return await httpClient.SendAsync(request, TestContext.CancellationToken);
    }

    public TestContext TestContext { get; set; } = default!;
}
