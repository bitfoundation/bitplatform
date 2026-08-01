using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Boilerplate.Shared.Features.Identity;
using Boilerplate.Tests.Infrastructure.Services;
using Boilerplate.Server.Api.Infrastructure.Data;
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Server.Api.Features.Identity.Resources;

namespace Boilerplate.Tests.Features.Identity;

/// <summary>
/// What the "send me a code" endpoints must not commit when the send leg fails.
/// <para>
/// Each of them is throttled by a <c>*RequestedOn</c> column that is written before the message goes out, so anything
/// that can still fail after that write refuses the account's own next request for the whole token lifetime having
/// delivered nothing. The one caller-controlled trigger is <c>HttpRequest.GetWebAppUrl()</c>, which resolves the
/// <c>?origin=</c> / <c>X-Origin</c> value and throws for anything that is neither the server's own address nor a
/// configured <c>TrustedOrigin</c> (and <c>TrustedOrigins</c> ships empty).
/// </para>
/// <para>
/// <b>What protects <c>IdentityController</c> is not the ordering inside the action - it is that the origin is
/// validated during CONTROLLER ACTIVATION.</b> <c>IdentityController.WebAuthn</c> injects <c>IFido2</c>, whose scoped
/// <c>Fido2Configuration</c> factory calls <c>GetWebAppUrl()</c> while the controller is being constructed (see the
/// <c>AddScoped(sp =&gt; new Fido2Configuration { ... })</c> registration in <c>Program.Services.cs</c>), so a hostile
/// origin is refused before a single line of any action body runs. <c>UserController</c> is covered the same way.
/// </para>
/// <para>
/// That protection is load-bearing and entirely implicit, which is exactly what this test pins: move the Fido2
/// dependency behind a lazily resolved service, or split the WebAuthn actions into a controller of their own, and every
/// <c>Send*Token</c> action silently starts stamping its throttle for requests that deliver nothing.
/// </para>
/// </summary>
[TestClass, TestCategory("IntegrationTest")]
public class IdentityEmailDeliveryTests
{
    [TestMethod]
    public async Task SendResetPasswordToken_WithAnUntrustedOrigin_Should_BeRefusedBeforeAnythingIsCommitted()
    {
        await using var server = new AppTestServer();
        await server.Build(s => s.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);

        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var (email, _) = await TestAccountUtils.CreateAndSignIn(server, scope, TestContext.CancellationToken);

        // Driven over raw HTTP rather than the typed proxy: the header is the thing under test and the generated proxy
        // has no way to set it.
        using var anonymousHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };

        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/Identity/SendResetPasswordToken")
        {
            Content = JsonContent.Create(new { Email = email })
        };
        request.Headers.Add("X-Origin", "https://evil.example");

        using var response = await anonymousHttpClient.SendAsync(request, TestContext.CancellationToken);

        var body = await response.Content.ReadAsStringAsync(TestContext.CancellationToken);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
            $"An origin that is neither the server's own address nor a TrustedOrigin has to be refused. Body: {body}");

        // Pins WHICH 400 this is. Without it the test would pass just as happily on a request rejected by model
        // binding or validation, and would then be asserting nothing.
        Assert.Contains("Invalid origin", body, StringComparison.Ordinal,
            $"The refusal has to be the origin check in GetWebAppUrl, not some earlier rejection. Body: {body}");

        var store = server.WebApp.Services.GetRequiredService<EmailCaptureStore>();

        Assert.IsFalse(store.Captured.Any(e => e.IsTo(email) && e.Kind is CapturedEmailKind.ResetPassword),
            "The request was refused, so no reset-password e-mail may have gone out.");

        // The property worth protecting: the refused request left no trace on the account. Read off the row rather
        // than inferred from the next call's behaviour, so a failure names the column instead of surfacing as a
        // confusing TooManyRequests three lines further down.
        await using (var dbScope = server.WebApp.Services.CreateAsyncScope())
        {
            var dbContext = dbScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var normalizedEmail = email.ToUpperInvariant();

            var requestedOn = await dbContext.Set<User>()
                .Where(u => u.NormalizedEmail == normalizedEmail)
                .Select(u => u.ResetPasswordTokenRequestedOn)
                .SingleAsync(TestContext.CancellationToken);

            Assert.IsNull(requestedOn,
                "The refused request stamped ResetPasswordTokenRequestedOn, which means the origin is no longer being " +
                "validated during controller activation. A request carrying a hostile origin now consumes the account's " +
                "resend window while delivering nothing - once per victim per ResetPasswordTokenLifetime.");
        }

        // And the account really can still ask for a code.
        var identityController = scope.ServiceProvider.GetRequiredService<IIdentityController>();

        await identityController.SendResetPasswordToken(new() { Email = email }, TestContext.CancellationToken);

        var captured = await server.WaitForCapturedEmail(email,
            e => e.Kind is CapturedEmailKind.ResetPassword, TestContext.CancellationToken);

        Assert.IsFalse(string.IsNullOrEmpty(captured.Token),
            "The legitimate request that follows must still deliver a usable reset-password code.");
    }

    /// <summary>
    /// <b>Currently failing by design - BP-154 is open, pending a product decision.</b>
    /// <para>
    /// Five identity e-mail subjects interpolate the live six-digit credential as <c>{0}</c>, and each template repeats
    /// it in its <c>&lt;title&gt;</c>. A subject line leaves the encrypted body: it shows in lock-screen notification
    /// previews, is written into every mail-server log along the path, and is retained in the Hangfire job payload. The
    /// code stays usable for roughly six to nine minutes whatever the configured lifetime says, because
    /// <c>Rfc6238AuthenticationService</c> accepts +/-2 timesteps of three minutes.
    /// </para>
    /// <para>
    /// It is deliberate, not an oversight - putting the code in the subject is what makes the preview useful - so
    /// unblocking this needs a decision rather than a patch: either keep it and say so in the resx comments, or move
    /// the token out of the five <c>*Subject</c> strings in all ten locales and out of each template's title.
    /// </para>
    /// </summary>
    [TestMethod, Ignore("BP-154 is open: whether the code belongs in the subject line is a product decision. " +
        "Without the attribute it fails with 'These subject lines still put the live code outside the encrypted body: " +
        "OtpEmailSubject, TfaTokenEmailSubject, ResetPasswordEmailSubject, ElevatedAccessTokenEmailSubject, ConfirmationEmailSubject'.")]
    public void IdentityEmailSubjects_Should_NotCarryTheToken()
    {
        // The subject is built by IdentityEmailService as emailLocalizer[EmailStrings.XxxSubject, token], so a subject
        // that formats a placeholder is a subject that carries the credential. Asserted against the resource itself
        // because the test double replaces IdentityEmailService before any subject is ever composed.
        var localizer = new AppTestServer().Build(s => s.AddIntegrationApiOnlyTestsServices())
            .WebApp.Services.GetRequiredService<IStringLocalizer<EmailStrings>>();

        string[] tokenBearingSubjects =
        [
            nameof(EmailStrings.OtpEmailSubject),
            nameof(EmailStrings.TfaTokenEmailSubject),
            nameof(EmailStrings.ResetPasswordEmailSubject),
            nameof(EmailStrings.ElevatedAccessTokenEmailSubject),
            nameof(EmailStrings.ConfirmationEmailSubject)
        ];

        const string marker = "483920";

        var offenders = tokenBearingSubjects
            .Where(key => localizer[key, marker].Value.Contains(marker, StringComparison.Ordinal))
            .ToArray();

        Assert.AreEqual(0, offenders.Length,
            $"These subject lines still put the live code outside the encrypted body: {string.Join(", ", offenders)}.");
    }

    public TestContext TestContext { get; set; } = default!;
}
