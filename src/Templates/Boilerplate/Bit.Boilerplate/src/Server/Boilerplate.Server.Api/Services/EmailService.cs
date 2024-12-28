using FluentEmail.Core;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Boilerplate.Server.Api.Models.Emailing;
using Boilerplate.Server.Api.Models.Identity;

namespace Boilerplate.Server.Api.Services;

public partial class EmailService
{
    [AutoInject] private HtmlRenderer htmlRenderer = default!;
    [AutoInject] private ILogger<EmailService> logger = default!;
    [AutoInject] private ServerApiSettings appSettings = default!;
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private IHttpContextAccessor httpContextAccessor = default!;
    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;
    [AutoInject] private RootServiceScopeProvider rootServiceScopeProvider = default!;

    public async Task SendResetPasswordToken(User user, string token, Uri link, CancellationToken cancellationToken)
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

        await SendEmail(body, user.Email!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendOtp(User user, string token, Uri link, CancellationToken cancellationToken)
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
            LogSendEmail(logger, subject, user.Email!, "EmailToken", link.ToString());
        }

        var body = await BuildBody<EmailTokenTemplate>(new Dictionary<string, object?>()
        {
            [nameof(EmailTokenTemplate.Model)] = new EmailTokenTemplateModel { Email = toEmailAddress, Token = token, Link = link },
            [nameof(EmailTokenTemplate.HttpContext)] = httpContextAccessor.HttpContext
        });

        await SendEmail(body, toEmailAddress!, user.DisplayName!, subject, cancellationToken);
    }

    public async Task SendElevatedAccessToken(User user, string token, CancellationToken cancellationToken)
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
        var defaultFromName = emailLocalizer[nameof(EmailStrings.DefaultFromName)];
        var defaultFromEmail = appSettings.Email!.DefaultFromEmail;

        _ = Task.Run(async () => // Let's not wait for the email to be sent. Consider using a proper message queue or background job system like Hangfire.
        {
            await using var scope = rootServiceScopeProvider.Invoke();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<EmailService>>();

            try
            {
                var fluentEmail = scope.ServiceProvider.GetRequiredService<IFluentEmail>();
                var localizer = scope.ServiceProvider.GetRequiredService<IStringLocalizer<AppStrings>>();
                var emailResult = await fluentEmail.To(toEmailAddress, toName)
                                               .Subject(subject)
                                               .SetFrom(defaultFromEmail, defaultFromName)
                                               .Body(body, isHtml: true)
                                               .SendAsync(default);

                if (emailResult.Successful is false)
                    throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => localizer[err]).ToArray());
            }
            catch (Exception exp)
            {
                LogSendEmailFailed(logger, exp, subject, toEmailAddress);
            }

        }, default);
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send e-mail with subject '{Subject}' to {ToEmailAddress}.")]
    private static partial void LogSendEmailFailed(ILogger logger, Exception exp, string subject, string toEmailAddress);

    [LoggerMessage(Level = LogLevel.Information, Message = "{type} e-mail with subject '{subject}' to {toEmailAddress}. {link}")]
    private static partial void LogSendEmail(ILogger logger, string subject, string toEmailAddress, string type, string? link = null);
}
