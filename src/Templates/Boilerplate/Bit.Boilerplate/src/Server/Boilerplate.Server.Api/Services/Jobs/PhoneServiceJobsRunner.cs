using Twilio.Rest.Api.V2010.Account;

namespace Boilerplate.Server.Api.Services.Jobs;

public partial class PhoneServiceJobsRunner
{
    [AutoInject] private ServerExceptionHandler serverExceptionHandler = default!;

    [AutomaticRetry(Attempts = 3)]
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
