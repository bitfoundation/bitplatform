//+:cnd:noEmit
using Hangfire.Server;
using SmsBridge.Abstractions;

namespace Boilerplate.Server.Api.Infrastructure.Services;

public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ISmsClient smsClient = default!;
    [AutoInject] private ApiServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30] /*We primarily send tokens via sms, which expire after 2 minutes by default. It's not worth retrying more than 3 times, with a 30-second delay between attempts.*/,
                    OnAttemptsExceeded = AttemptsExceededAction.Delete)]
    public async Task SendSms(string phoneNumber, string messageText,
        PerformContext context = null!,
        CancellationToken cancellationToken = default)

    {
        SmsSendResult? result = null;
        try
        {
            result = await smsClient.SendAsync(new() { To = phoneNumber, Body = messageText }, cancellationToken);

            if (result.Success is false)
                throw new InvalidOperationException(result.ErrorMessage).WithData(new() { { "Provider", result.Provider }, { "Code", result.ErrorCode }, { "IsTransient", result.IsTransientFailure } });
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new()
            {
                { "PhoneNumber", phoneNumber },
                { "JobId", context.BackgroundJob.Id }
            });
            if (cancellationToken.IsCancellationRequested || (exp is not KnownException && result?.IsTransientFailure is not false))
                throw; // To retry the job
        }
    }
}
