using System.Net;
using Azure.Communication.Sms;

namespace Boilerplate.Server.Services;

public partial class SmsService
{
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private ILogger<SmsService> logger = default!;
    [AutoInject] private Lazy<SmsClient> smsClientProvider = default!;
    [AutoInject] private AppSettings appSettings = default!;

    public async Task SendSms(string message, string phoneNumber, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSms(logger, message, phoneNumber);
        }

        if (appSettings.SmsSettings.Configured)
        {
            SmsSendResult sendResult = smsClientProvider.Value.Send(
                from: appSettings.SmsSettings.FromPhoneNumber,
                to: phoneNumber,
                message: message,
                options: new(enableDeliveryReport: true),
                cancellationToken
            );

            if (sendResult.Successful is false)
            {
                LogSendSmsFailed(logger, sendResult.To, sendResult.MessageId, (HttpStatusCode)sendResult.HttpStatusCode, sendResult.ErrorMessage);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "SMS: {message} to {phoneNumber}.")]
    static partial void LogSms(ILogger logger, string message, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Sms to {phoneNumber}. Id: {id}, Status code: {statusCode}, Error message: {errorMessage}")]
    static partial void LogSendSmsFailed(ILogger logger, string phoneNumber, string id, HttpStatusCode statusCode, string errorMessage);
}
