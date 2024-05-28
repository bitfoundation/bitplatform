using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Boilerplate.Server.Services;

public partial class SmsService
{
    [AutoInject] private readonly AppSettings appSettings = default!;
    [AutoInject] private readonly ILogger<SmsService> logger = default!;
    [AutoInject] private readonly IHostEnvironment hostEnvironment = default!;

    public async Task SendSms(string messageText, string phoneNumber, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(logger, messageText, phoneNumber);
        }

        if (appSettings.SmsSettings.Configured is false) return;

        var messageOptions = new CreateMessageOptions(
          new PhoneNumber(phoneNumber))
        {
            From = new PhoneNumber(appSettings.SmsSettings.FromPhoneNumber),
            Body = messageText
        };

        var smsMessage = MessageResource.Create(messageOptions);

        if (smsMessage.ErrorCode is null)
            return;

        LogSendSmsFailed(logger, phoneNumber, smsMessage.ErrorCode, smsMessage.ErrorMessage);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS: {message} to {phoneNumber}.")]
    private static partial void LogSendSms(ILogger logger, string message, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Sms to {phoneNumber}. Code: {errorCode}, Error message: {errorMessage}")]
    private static partial void LogSendSmsFailed(ILogger logger, string phoneNumber, int? errorCode, string errorMessage);
}
