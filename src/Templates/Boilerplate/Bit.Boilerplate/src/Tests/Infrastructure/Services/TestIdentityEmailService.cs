//+:cnd:noEmit
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components.Web;
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Server.Api.Features.Identity.Services;
using Boilerplate.Server.Api.Features.Identity.Resources;

namespace Boilerplate.Tests.Infrastructure.Services;

/// <summary>
/// Replaces <see cref="IdentityEmailService"/> in tests. Every e-mail method is overridden to capture what a test needs
/// (recipient, token, link) synchronously into a per-server <see cref="EmailCaptureStore"/>, instead of rendering the
/// e-mail and enqueuing a Hangfire delivery job. This is both simpler - the token and link are direct method arguments,
/// so there is no e-mail body to parse - and reliable under parallel test load, where the in-memory Hangfire storage can
/// starve and never run the delivery job. Not calling <c>base</c> also means no e-mail ever reaches SMTP.
/// </summary>
public class TestIdentityEmailService : IdentityEmailService
{
    private readonly EmailCaptureStore captureStore;

    // IdentityEmailService's dependencies come from its AutoInject-generated constructor and there is no parameterless
    // one, so forward them to base and take the capture store on top. This is written out explicitly because the
    // AutoInject generator does not chain a base constructor that lives in another assembly. If the base's injected
    // dependencies change, this constructor is where the compiler will point.
    public TestIdentityEmailService(
        EmailCaptureStore captureStore,
        IBackgroundJobClient backgroundJobClient,
        IStringLocalizer<EmailStrings> emailLocalizer,
        IHostEnvironment hostEnvironment,
        HtmlRenderer htmlRenderer,
        IHttpContextAccessor httpContextAccessor,
        ILogger<IdentityEmailService> logger)
        : base(backgroundJobClient, emailLocalizer, hostEnvironment, htmlRenderer, httpContextAccessor, logger)
    {
        this.captureStore = captureStore;
    }

    public override Task SendResetPasswordToken(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.ResetPassword, ToEmailAddress = user.Email!, Token = token, Link = link });
        return Task.CompletedTask;
    }

    public override Task SendOtp(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.Otp, ToEmailAddress = user.Email!, Token = token, Link = link });
        return Task.CompletedTask;
    }

    public override Task SendTwoFactorToken(User user, string token, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.TwoFactor, ToEmailAddress = user.Email!, Token = token });
        return Task.CompletedTask;
    }

    public override Task SendEmailToken(User user, string toEmailAddress, string token, Uri link, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.EmailToken, ToEmailAddress = toEmailAddress, Token = token, Link = link });
        return Task.CompletedTask;
    }

    public override Task SendElevatedAccessToken(User user, string token, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.ElevatedAccess, ToEmailAddress = user.Email!, Token = token });
        return Task.CompletedTask;
    }

    //#if (multitenant == true)
    public override Task SendTenantInvitation(User user, string inviterDisplayName, string tenantTitle, Uri link, CancellationToken cancellationToken)
    {
        captureStore.Add(new() { Kind = CapturedEmailKind.TenantInvitation, ToEmailAddress = user.Email!, Link = link });
        return Task.CompletedTask;
    }
    //#endif
}
