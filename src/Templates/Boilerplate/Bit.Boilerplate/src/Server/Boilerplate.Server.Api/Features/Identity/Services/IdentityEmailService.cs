//+:cnd:noEmit
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Server.Api.Features.Identity.Services;

public partial class IdentityEmailService
{
    [AutoInject] private AppDbContext dbContext = default!;
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private ILogger<IdentityEmailService> logger = default!;
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IBackgroundJobClient backgroundJobClient = default!;
    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    public virtual async Task SendResetPasswordToken(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ResetPasswordEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "ResetPassword", link.ToString());
        }

        var body = await BuildBody<ResetPasswordTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(ResetPasswordTokenTemplate.Model)] = new ResetPasswordTokenTemplateModel
            {
                Token = token,
                Link = link,
                DisplayName = user.DisplayName!,
            },
            [nameof(ResetPasswordTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject);
    }

    public virtual async Task SendOtp(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.OtpEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "Otp", link.ToString());
        }

        var body = await BuildBody<OtpTemplate>(new Dictionary<string, object?>()
        {
            [nameof(OtpTemplate.Model)] = new OtpTemplateModel
            {
                Token = token,
                Link = link,
                DisplayName = user.DisplayName!,
            },
            [nameof(OtpTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject);
    }

    public virtual async Task SendTwoFactorToken(User user, string token, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.TfaTokenEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "TwoFactor");
        }

        var body = await BuildBody<TwoFactorTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(TwoFactorTokenTemplate.Model)] = new TwoFactorTokenTemplateModel { DisplayName = user.DisplayName!, Token = token },
            [nameof(TwoFactorTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject);
    }

    public virtual async Task SendEmailToken(User user, string toEmailAddress, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ConfirmationEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "EmailToken", link.ToString());
        }

        var body = await BuildBody<EmailTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(EmailTokenTemplate.Model)] = new EmailTokenTemplateModel { Email = toEmailAddress, Token = token, Link = link },
            [nameof(EmailTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, toEmailAddress!, user.DisplayName!, subject);
    }

    public virtual async Task SendElevatedAccessToken(User user, string token, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ElevatedAccessTokenEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "ElevatedAccess");
        }

        var body = await BuildBody<ElevatedAccessTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(ElevatedAccessTokenTemplate.Model)] = new ElevatedAccessTokenTemplateModel { DisplayName = user.DisplayName!, Token = token },
            [nameof(ElevatedAccessTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject);
    }

    //#if (multitenant == true)
    public virtual async Task SendTenantInvitation(User user, string inviterDisplayName, string tenantTitle, Uri link, CancellationToken cancellationToken)
    {
        // The invitation's recipient is NOT the caller: CurrentUICulture belongs to the INVITER's request, so prefer
        // the culture the recipient's most recent session reported (See UserController.UpdateSession; sessions that
        // never reported one are skipped). No session culture -> the inviter's language stays the best guess.
        var recipientCulture = CultureInfoManager.GetCultureInfo(await dbContext.UserSessions
            .Where(session => session.UserId == user.Id && session.CultureName != null)
            .OrderByDescending(session => session.RenewedOn ?? session.StartedOn)
            .Select(session => session.CultureName)
            .FirstOrDefaultAsync(cancellationToken));

        // Flow-scoped and restored below, so the rest of the inviter's request stays in their own culture.
        var (originalCulture, originalUICulture) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);

        if (recipientCulture is not null)
        {
            CultureInfo.CurrentCulture = recipientCulture;
            CultureInfo.CurrentUICulture = recipientCulture;
        }

        try
        {
            await SendTenantInvitationCore(user, inviterDisplayName, tenantTitle, link);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUICulture;
        }
    }

    private async Task SendTenantInvitationCore(User user, string inviterDisplayName, string tenantTitle, Uri link)
    {
        var subject = emailLocalizer[EmailStrings.TenantInvitationEmailSubject, tenantTitle];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "TenantInvitation", link.ToString());
        }

        var body = await BuildBody<TenantInvitationTemplate>(new Dictionary<string, object?>()
        {
            [nameof(TenantInvitationTemplate.Model)] = new TenantInvitationTemplateModel
            {
                DisplayName = user.DisplayName!,
                InviterDisplayName = inviterDisplayName,
                TenantTitle = tenantTitle,
                Link = link
            },
            [nameof(TenantInvitationTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject);
    }
    //#endif

    private async Task<string> BuildBody<TTemplate>(Dictionary<string, object?> parameters)
        where TTemplate : IComponent
    {
        var body = await htmlRenderer.Dispatcher.InvokeAsync(async () =>
        {
            var renderedComponent = await htmlRenderer.RenderComponentAsync<TTemplate>(ParameterView.FromDictionary(parameters));

            return renderedComponent.ToHtmlString();
        });

        return body!;
    }

    private async Task SendEmail(string body, string toEmailAddress, string toName, string subject)
    {
        backgroundJobClient.Enqueue<EmailServiceJobsRunner>(jobRunner => jobRunner.SendEmailJob(toEmailAddress, toName, subject, body));
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} e-mail with subject '{subject}' to {toEmailAddress}. {link}")]
    private static partial void LogSendEmail(ILogger logger, string subject, string toEmailAddress, string type, string? link = null);
}
