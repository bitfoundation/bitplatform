using System.Text;

namespace Boilerplate.Tests.E2E.Features.Urls;

/// <summary>
/// <c>GetWebAppUrl()</c> reads a caller-supplied origin - the <c>origin</c> query value or the <c>X-Origin</c> header -
/// on an anonymous, pre-authorization path: <c>IdentityController</c> injects <c>IFido2</c>, whose scoped
/// <c>Fido2Configuration</c> factory calls it, so an origin it cannot parse used to fault while the controller was
/// still being CONSTRUCTED (BP-162). Its contract for an origin it does not like is a 400.
/// <para>
/// <c>Boilerplate.Tests</c> pins the same rule in process. Here it is asserted on the deployments, because that is
/// where the header actually arrives: behind Cloudflare the header set differs from an in-process request's, and the
/// hosts run a forwarded-headers configuration the test server never sees. The in-process test can only send the query
/// value through the typed controller proxy; this one sends the header too.
/// </para>
/// </summary>
[TestClass, TestCategory(TestCategories.Api), Retry(2)]
public class WebAppUrlOriginHardeningTests
{
    /// <summary>
    /// Each one is a value an anonymous caller can put there, and none of them is an absolute URI. <c>null</c> is the
    /// control: with no origin at all the endpoint answers its own validation error, which is the same 400 - so the
    /// rows only prove something together with it.
    /// </summary>
    private static readonly string?[] malformedOrigins = ["", " ", "notaurl", "/relative", null];

    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task AMalformedOrigin_Should_NotFaultTheDeployedApi(string api)
    {
        foreach (var origin in malformedOrigins)
        {
            foreach (var asHeader in new[] { false, true })
            {
                // The control has only one shape: no origin is no origin, whether or not a header was the way to send it.
                if (origin is null && asHeader)
                    continue;

                var (status, body) = await SignIn(api, origin, asHeader);

                var sent = origin is null ? "no origin" : $"{(asHeader ? "X-Origin" : "?origin")}='{origin}'";

                Assert.AreNotEqual(HttpStatusCode.InternalServerError, status,
                    $"{api} answered {sent} with a server fault, which is the LogCritical half of BP-162: {body}");

                Assert.AreEqual(HttpStatusCode.BadRequest, status,
                    $"{api} answered {sent} with {(int)status} rather than the documented 400: {body}");
            }
        }
    }

    /// <summary>
    /// The other half: an origin that IS a well-formed absolute URI but belongs to nobody must still be refused, and
    /// by name. Without it, "never faults" would be satisfied by a deployment that accepts every origin it is given.
    /// </summary>
    [TestMethod]
    [DataRow(DeployedApps.AdminPanelApi, DisplayName = "AdminPanelApi")]
    [DataRow(DeployedApps.TodoApi, DisplayName = "TodoApi")]
    [DataRow(DeployedApps.Sales, DisplayName = "Sales (integrated API)")]
    public async Task AnUntrustedButWellFormedOrigin_Should_StillBeRefusedByName(string api)
    {
        var (status, body) = await SignIn(api, "https://evil.example", asHeader: false);

        Assert.AreEqual(HttpStatusCode.BadRequest, status, $"{api} did not refuse an untrusted origin: {body}");

        Assert.Contains("Invalid origin", body, StringComparison.OrdinalIgnoreCase,
            $"{api} refused the untrusted origin without saying that is what it refused: {body}");
    }

    /// <summary>
    /// An empty body on purpose: the endpoint's own answer to it is a validation failure, so anything else means the
    /// origin decided the outcome. Anonymous, and json - the antiforgery filter only guards the non-json posts.
    /// </summary>
    private async Task<(HttpStatusCode Status, string Body)> SignIn(string api, string? origin, bool asHeader)
    {
        var path = "api/v1/Identity/SignIn";

        if (origin is not null && asHeader is false)
        {
            path += $"?origin={Uri.EscapeDataString(origin)}";
        }

        using var httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(2) };
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(new Uri(api), path))
        {
            Content = new StringContent("{}", Encoding.UTF8, "application/json")
        };

        if (origin is not null && asHeader)
        {
            request.Headers.TryAddWithoutValidation("X-Origin", origin);
        }

        using var response = await httpClient.SendAsync(request, TestContext.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        // The identity endpoints share one rate limit window with every other anonymous caller of the deployment, so a
        // 429 here says nothing about the origin - and asserting a 400 over it would be reporting someone else's burst.
        if (response.StatusCode is HttpStatusCode.TooManyRequests)
        {
            Assert.Inconclusive($"{api}'s identity rate limit window was already exhausted, so this request never reached the origin handling.");
        }

        return (response.StatusCode, body);
    }
}
