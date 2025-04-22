//+:cnd:noEmit
using Twilio.Rest.Api.V2010.Account;

namespace Boilerplate.Server.Api.Services.Jobs;

public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(Attempts = 3, DelaysInSeconds = [30] /*We primarily send tokens via sms, which expire after 2 minutes by default. It's not worth retrying more than 3 times, with a 30-second delay between attempts.*/)]
    public async Task SendSms(string phoneNumber, string from, string messageText, CancellationToken cancellationToken)

    {
        try
        {
            var messageOptions = new CreateMessageOptions(new(phoneNumber))
            {
                From = new(from),
                Body = messageText
            };

            var smsMessage = MessageResource.Create(messageOptions);

            if (smsMessage.ErrorCode is not null)
                throw new InvalidOperationException(smsMessage.ErrorMessage).WithData(new() { { "Code", smsMessage.ErrorCode } });
        }
        catch (Exception exp)
        {
            serverExceptionHandler.Handle(exp, new() { { "PhoneNumber", phoneNumber } });
            if (exp is not KnownException && cancellationToken.IsCancellationRequested is false)
                throw; // To retry the job
        }
    }
}
