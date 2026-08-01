using System.Net;

namespace Boilerplate.Tests.Features.ForceUpdate;

/// <summary>
/// <b>Pins an open finding (BP-079). Remove the <c>[Ignore]</c> when it is fixed.</b>
/// <para>
/// <c>ForceUpdateMiddleware</c> runs on <b>every</b> request, before authentication, and parses two request headers
/// with three throwing APIs and no guards: <c>Headers["X-App-Platform"].Single()</c>,
/// <c>Enum.Parse&lt;AppPlatformType&gt;(...)</c> and <c>Version.Parse(appVersion)</c>. Every one of them is fed
/// directly from headers any anonymous caller controls, so each is a one-line request that turns into a 500 plus a
/// <c>LogCritical</c> record - free, unauthenticated log noise on any endpoint, and a 500 where the caller should
/// have got the real answer.
/// </para>
/// <para>
/// The <c>Single()</c> case is the sharpest: the middleware only checks that <c>X-App-Version</c> is present, then
/// reads <c>X-App-Platform</c> as though presence of the first guaranteed the second. Sending just
/// <c>X-App-Version</c> - which is what a curl, a health prober or a partial client does - is enough.
/// </para>
/// <para>
/// What unblocks this: read both headers defensively (<c>FirstOrDefault()</c>, <c>Enum.TryParse</c>,
/// <c>Version.TryParse</c>) and simply skip the version check when the client did not supply a usable pair. The
/// middleware's job is to force an update, not to validate headers - failing open is the correct direction here,
/// since a caller that cannot state its version is not a client this check applies to.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class ForceUpdateHeaderHardeningTests
{
    /// <summary>
    /// Each row is a header combination an anonymous caller can send today. The endpoint is an ordinary anonymous
    /// GET whose real answer is 404; the assertion is simply that the client version headers did not change it into
    /// a server fault.
    /// </summary>
    [TestMethod, Ignore("Pins BP-079: ForceUpdateMiddleware parses attacker-controlled headers with Single()/Enum.Parse/Version.Parse. Verified 2026-08-01 - all four rows return 500 UnknownException + LogCritical today.")]
    [DataRow("1.0.0", null, DisplayName = "X-App-Platform missing -> Single() on an empty StringValues")]
    [DataRow("1.0.0", "NotAPlatform", DisplayName = "unknown platform -> Enum.Parse throws")]
    [DataRow("not-a-version", "Web", DisplayName = "unparsable version -> Version.Parse throws")]
    [DataRow("1.0.0, 2.0.0", "Web", DisplayName = "repeated X-App-Version, which Kestrel joins into one comma-separated value")]
    public async Task AMalformedClientVersionHeader_Should_NotFaultTheRequest(string appVersion, string? appPlatform)
    {
        await using var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/v1/Attachment/GetAttachment/{Guid.NewGuid()}/UserProfileImageSmall");
        request.Headers.TryAddWithoutValidation("X-App-Version", appVersion);

        if (appPlatform is not null)
        {
            request.Headers.TryAddWithoutValidation("X-App-Platform", appPlatform);
        }

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode,
            $"A header the caller controls must not turn the real 404 into a server fault. Body: {body}");
    }

    public TestContext TestContext { get; set; } = default!;
}
