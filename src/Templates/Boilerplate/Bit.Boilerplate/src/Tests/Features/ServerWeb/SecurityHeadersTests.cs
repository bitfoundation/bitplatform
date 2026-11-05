//+:cnd:noEmit
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Tests.Features.ServerWeb;

/// <summary>
/// Pins the parts of <c>UseSecurityHeaders</c> (<c>Server.Shared/Infrastructure/Extensions/WebApplicationExtensions.cs</c>)
/// that a shipped feature depends on, as opposed to the ones that are pure hardening and can be tightened freely.
/// <para>
/// The middleware only runs outside Development, so these tests host it on their own minimal app rather than through
/// <see cref="AppTestServer"/>: that one is Development by construction, and the production branch it would have to
/// take also turns on <c>UseHttpsRedirection</c>, which would answer every plain-http assertion with a 307.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class SecurityHeadersTests
{
    /// <summary>
    /// <c>Permissions-Policy</c> lists the features this origin may even ask for, and an empty allowlist -
    /// <c>microphone=()</c> - denies the origin itself, not just third parties. The AI chat panel dictates into the
    /// message box, so it needs <c>microphone=(self)</c>.
    /// <para>
    /// This gets a test because the failure is invisible everywhere it would plausibly be looked for: the header is
    /// only sent outside Development, and the browser reports the refusal as a <c>getUserMedia</c> rejection that
    /// cannot be told apart from the user declining the microphone prompt. So tightening it back to <c>()</c> breaks
    /// dictation in production only, with an error message that blames the user.
    /// </para>
    /// </summary>
    [TestMethod]
    public async Task PermissionsPolicy_Should_LetThisOriginUseTheMicrophone()
    {
        var permissionsPolicy = await ReadPermissionsPolicy();

        Assert.Contains("microphone=(self)", permissionsPolicy,
            $"Dictation in the AI chat panel needs this origin to be allowed to request the microphone. Header: {permissionsPolicy}");
    }

    /// <summary>
    /// Non-vacuity, and the reason the assertion above is not merely "the header mentions the microphone": everything
    /// the template does not use stays denied. Without this, relaxing the whole header would keep the test above green
    /// while quietly widening the attack surface.
    /// </summary>
    [TestMethod]
    public async Task PermissionsPolicy_Should_StillDenyTheFeaturesNothingUses()
    {
        var permissionsPolicy = await ReadPermissionsPolicy();

        foreach (var feature in (string[])["geolocation", "camera", "payment", "usb", "display-capture"])
        {
            Assert.Contains($"{feature}=()", permissionsPolicy,
                $"Nothing in the template uses {feature}, so it should stay denied. Header: {permissionsPolicy}");
        }
    }

    [TestMethod]
    [DataRow("Test", true)]
    [DataRow("Staging", true)]
    [DataRow("Production", false)]
    public async Task XRobotsTag_Should_KeepEverythingButProductionOutOfSearchResults(string environment, bool isSent)
    {
        using var response = await Send(environment);

        Assert.AreEqual(isSent, response.Headers.TryGetValues("X-Robots-Tag", out var values), environment);
        if (isSent)
        {
            Assert.AreEqual("noindex, nofollow", string.Join(", ", values!));
        }
    }

    [TestMethod]
    public async Task XXssProtection_Should_NotBeSent()
    {
        using var response = await Send(Environments.Production);

        Assert.IsFalse(response.Headers.Contains("X-XSS-Protection"));
    }

    [TestMethod]
    public async Task ContentSecurityPolicy_Should_BeTheEndpointsOwn_WhenItWroteOne()
    {
        using var ownPolicy = await Send(Environments.Production, "/own-policy");
        using var baseline = await Send(Environments.Production);

        Assert.AreEqual("default-src 'self';", string.Join(", ", ownPolicy.Headers.GetValues("Content-Security-Policy")));
        Assert.Contains("base-uri 'self';", string.Join(", ", baseline.Headers.GetValues("Content-Security-Policy")));
    }

    /// <summary>
    /// Hosts <c>UseSecurityHeaders</c> on its own, in Production, and returns the <c>Permissions-Policy</c> it wrote.
    /// </summary>
    private async Task<string> ReadPermissionsPolicy()
    {
        using var response = await Send(Environments.Production);

        Assert.IsTrue(response.Headers.TryGetValues("Permissions-Policy", out var values),
            "UseSecurityHeaders should write a Permissions-Policy header; without one these tests assert nothing.");

        return string.Join(", ", values!);
    }

    private async Task<HttpResponseMessage> Send(string environment, string path = "/")
    {
        var address = $"http://127.0.0.1:{Random.Shared.Next(20000, 30000)}";

        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = environment });
        builder.WebHost.UseUrls(address);
        builder.Logging.ClearProviders();

        await using var app = builder.Build();

        app.UseSecurityHeaders();
        app.MapGet("/", () => "ok");
        app.MapGet("/own-policy", (HttpContext context) =>
        {
            context.Response.Headers.ContentSecurityPolicy = "default-src 'self';";
            return "ok";
        });

        await app.StartAsync(TestContext.CancellationToken);

        try
        {
            using var httpClient = new HttpClient { BaseAddress = new Uri(address) };

            return await httpClient.GetAsync(path, TestContext.CancellationToken);
        }
        finally
        {
            await app.StopAsync(TestContext.CancellationToken);
        }
    }

    public TestContext TestContext { get; set; } = default!;
}
