using System.Net;

namespace Boilerplate.Tests.Features.Urls;

/// <summary>
/// <b>Regression test for BP-162.</b>
/// <para>
/// <c>HttpRequestExtensions.GetWebAppUrl()</c> used to do
/// <c>request.Query["origin"].Union(request.Headers["X-Origin"]).Select(o =&gt; new Uri(o)).FirstOrDefault()</c>,
/// i.e. it handed raw caller-controlled text to a <b>throwing</b> <see cref="Uri"/> constructor <b>before</b> the
/// trusted-origin check below it. The method's contract for an origin it does not like is
/// <c>throw new BadRequestException($"Invalid origin {origin}")</c> - a 400 - and any value that is not an
/// absolute URI never reached that line, becoming an unhandled <see cref="UriFormatException"/> instead: a 500
/// plus a <c>LogCritical</c> record.
/// </para>
/// <para>
/// It is anonymous and pre-authorization: <c>IdentityController</c> and <c>UserController</c> inject
/// <c>IFido2</c>, whose scoped <c>Fido2Configuration</c> factory calls <c>GetWebAppUrl()</c>, so the throw
/// happened while the controller was being <b>constructed</b> - before model binding, before any action body, and
/// on <c>[AllowAnonymous]</c> endpoints (established as BP-155).
/// </para>
/// <para>
/// A blank <c>?origin=</c> now correctly means "no origin supplied" rather than "bad origin", so it must succeed
/// as far as the endpoint's own model validation - which is what the first row asserts.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class WebAppUrlOriginHardeningTests
{
    /// <summary>
    /// Every row is a value an anonymous caller can put on the query string. The endpoint is an anonymous POST
    /// whose real answer for an empty body is a 400 model-validation failure, so the assertion is that the origin
    /// value did not turn that 400 into a server fault. The last row is the control: with no origin at all the
    /// request must already be a 400, which proves the request really reaches the pipeline rather than the
    /// assertion holding vacuously.
    /// </summary>
    [TestMethod]
    [DataRow("?origin=", DisplayName = "present but empty -> treated as 'no origin supplied'")]
    [DataRow("?origin=%20", DisplayName = "whitespace -> treated as 'no origin supplied'")]
    [DataRow("?origin=notaurl", DisplayName = "not absolute -> the documented 400, not a 500")]
    [DataRow("", DisplayName = "CONTROL: no origin at all -> the endpoint's real 400")]
    public async Task AMalformedOriginQueryValue_Should_NotFaultTheRequest(string queryString)
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        using var httpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");

        using var response = await httpClient.PostAsync($"api/v1/Identity/SignIn{queryString}", content, TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            $"A caller-supplied origin must produce the documented 400, never a server fault. Body: {body}");
    }

    public TestContext TestContext { get; set; } = default!;
}
