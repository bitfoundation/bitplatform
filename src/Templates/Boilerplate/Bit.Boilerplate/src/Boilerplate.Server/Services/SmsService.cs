using System.Net;
using Azure.Communication.Sms;

namespace Boilerplate.Server.Services;

public partial class SmsService
{
    [AutoInject] private IHostEnvironment hostEnvironment = default!;
    [AutoInject] private ILogger<SmsService> logger = default!;
    [AutoInject] private AppSettings appSettings = default!;
    [AutoInject] private SmsClient? smsService = null;

    public async Task SendSms(string message, string phoneNumber, CancellationToken cancellationToken)
    {
        if (hostEnvironment.IsDevelopment())
        {
            LogSendSms(logger, message, phoneNumber);
        }

        if (smsService is not null)
        {
            SmsSendResult sendResult = smsService.Send(
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
    private static partial void LogSendSms(ILogger logger, string message, string phoneNumber);

    [LoggerMessage(Level = LogLevel.Error, Message = "Failed to send Sms to {phoneNumber}. Id: {id}, Status code: {statusCode}, Error message: {errorMessage}")]
    private static partial void LogSendSmsFailed(ILogger logger, string phoneNumber, string id, HttpStatusCode statusCode, string errorMessage);
}
