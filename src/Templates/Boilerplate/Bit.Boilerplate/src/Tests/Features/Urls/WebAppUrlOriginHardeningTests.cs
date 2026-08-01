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
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class WebAppUrlOriginHardeningTests
{
    /// <summary>
    /// Every row is a value an anonymous caller can put on the query string. The request goes through the app's
    /// own <see cref="HttpClient"/> (resolved from a request scope), so <c>ExceptionDelegatingHandler</c> turns
    /// the response back into the typed exception a real client would see - which is what makes the assertion
    /// meaningful: a <see cref="KnownException"/> is the endpoint answering, an <see cref="UnknownException"/> is
    /// a server fault.
    /// </summary>
    [TestMethod]
    [DataRow("?origin=", DisplayName = "present but empty -> treated as 'no origin supplied'")]
    [DataRow("?origin=%20", DisplayName = "whitespace -> treated as 'no origin supplied'")]
    [DataRow("?origin=notaurl", DisplayName = "not absolute -> the documented 400, not a 500")]
    [DataRow("?origin=/relative", DisplayName = "relative -> the documented 400, not a 500")]
    [DataRow("", DisplayName = "CONTROL: no origin at all -> the endpoint's own validation error")]
    public async Task AMalformedOriginQueryValue_Should_NotFaultTheRequest(string queryString)
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        using var content = JsonContent.Create(new { }, options: scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>());

        // The body is empty on purpose: the endpoint's real answer is a validation failure, so anything other
        // than a KnownException means the origin value - not the body - decided the outcome.
        var exception = await Assert.ThrowsAsync<Exception>(async ()
            => await httpClient.PostAsync($"api/v1/Identity/SignIn{queryString}", content, TestContext.CancellationToken));

        Assert.IsNotInstanceOfType<UnknownException>(exception,
            $"A caller-supplied origin must never turn into a server fault. Got: {exception}");

        Assert.IsInstanceOfType<KnownException>(exception,
            $"Expected the endpoint's own error. Got: {exception}");
    }

    /// <summary>
    /// The other half of the fix: an origin that IS a well-formed absolute URI but is not trusted must still be
    /// refused, and it must say so. Without this, "never 500" could be satisfied by accepting everything.
    /// </summary>
    [TestMethod]
    public async Task AnUntrustedButWellFormedOrigin_Should_StillBeRefusedByName()
    {
        await using var server = new AppTestServer();

        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();

        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        using var content = JsonContent.Create(new { }, options: scope.ServiceProvider.GetRequiredService<JsonSerializerOptions>());

        var exception = await Assert.ThrowsAsync<Exception>(async ()
            => await httpClient.PostAsync("api/v1/Identity/SignIn?origin=https://evil.example", content, TestContext.CancellationToken));

        Assert.IsNotInstanceOfType<UnknownException>(exception, $"An untrusted origin is a 400, not a fault. Got: {exception}");

        Assert.Contains("Invalid origin", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    public TestContext TestContext { get; set; } = default!;
}
