using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using FluentEmail.Core;
using Boilerplate.Server.Api.Models.Emailing;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services;

public partial class EmailService
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] private IFluentEmail fluentEmail = default!;
    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private ILogger<EmailService> logger = default!;
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private ServerApiSettings appSettings = default!;

    public async Task SendResetPasswordToken(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ResetPasswordEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "ResetPassword", Uri.UnescapeDataString(link.ToString()));
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

        await SendEmail(body, user.Email!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendOtp(User user, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.OtpEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "Otp", Uri.UnescapeDataString(link.ToString()));
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

        await SendEmail(body, user.Email!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendTwoFactorToken(User user, string token, CancellationToken cancellationToken)
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

        await SendEmail(body, user.Email!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendEmailToken(User user, string toEmailAddress, string token, Uri link, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ConfirmationEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "EmailToken", Uri.UnescapeDataString(link.ToString()));
        }

        var body = await BuildBody<EmailTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(EmailTokenTemplate.Model)] = new EmailTokenTemplateModel { Email = toEmailAddress, Token = token, Link = link },
            [nameof(EmailTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, toEmailAddress!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendPrivilegedAccessToken(User user, string token, CancellationToken cancellationToken)
    {
        var subject = emailLocalizer[EmailStrings.ConfirmationEmailSubject, token];

        if (hostEnvironment.IsDevelopment())
        {
            LogSendEmail(logger, subject, user.Email!, "PrivilegedAccessToken");
        }

        return;

        var body = await BuildBody<EmailTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(EmailTokenTemplate.Model)] = new EmailTokenTemplateModel { Email = user.Email, Token = token },
            [nameof(EmailTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, user.Email!, user.DisplayName!, subject, cancellationToken);
    }

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

    private async Task SendEmail(string body, string toEmailAddress, string toName, string subject, CancellationToken cancellationToken)
    {
        var emailResult = await fluentEmail.To(toEmailAddress, toName)
                                           .Subject(subject)
                                           .SetFrom(appSettings.Email!.DefaultFromEmail, emailLocalizer[nameof(EmailStrings.DefaultFromName)])
                                           .Body(body, isHtml: true)
                                           .SendAsync(cancellationToken);

        if (emailResult.Successful is false)
            throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => localizer[err]).ToArray());
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} e-mail with subject '{subject}' to {toEmailAddress}. {link}")]
    private static partial void LogSendEmail(ILogger logger, string subject, string toEmailAddress, string type, string? link = null);
}
