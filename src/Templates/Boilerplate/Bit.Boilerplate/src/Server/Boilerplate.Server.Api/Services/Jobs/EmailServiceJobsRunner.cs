//+:cnd:noEmit
using FluentEmail.Core;

namespace Boilerplate.Server.Api.Services.Jobs;

public partial class EmailServiceJobsRunner
{
    [AutoInject] IFluentEmail fluentEmail = default!;
    [AutoInject] private ServerApiSettings appSettings = default!;
    [AutoInject] IStringLocalizer<AppStrings> localizer = default!;
    [AutoInject] ServerExceptionHandler serverExceptionHandler = default!;
    [AutoInject] private IStringLocalizer<EmailStrings> emailLocalizer = default!;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30] /*We primarily send tokens via email, which expire after 2 minutes by default. It's not worth retrying more than 3 times, with a 30-second delay between attempts.*/)]
    public async Task SendEmailJob(string toEmailAddress, string toName, string subject, string body, CancellationToken cancellationToken)
    {
        try
        {
            var defaultFromName = emailLocalizer[nameof(EmailStrings.DefaultFromName)];
            var defaultFromEmail = appSettings.Email!.DefaultFromEmail;

            var emailResult = await fluentEmail
                                           .To(toEmailAddress, toName)
                                           .Subject(subject)
                                           .SetFrom(defaultFromEmail, defaultFromName)
                                           .Body(body, isHtml: true)
                                           .SendAsync(cancellationToken);

            if (emailResult.Successful is false)
                throw new ResourceValidationException(emailResult.ErrorMessages.Select(err => localizer[err]).ToArray());
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "Subject", subject }, { "ToEmailAddress", toEmailAddress } });
            if (exp is not KnownException && cancellationToken.IsCancellationRequested is false)
                throw; // To retry the job
        }
    }
}
