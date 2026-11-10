//+:cnd:noEmit
using System.Net.Http.Headers;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Infrastructure.HealthChecks;

/// <summary>
/// Checks Twilio SMS service connectivity by fetching account info. SmsBridge has no health check of its own yet: https://github.com/teesofttech/SMSBridge/issues/69
/// </summary>
public partial class TwilioHealthCheck : IHealthCheck
{
    [AutoInject] private ServerApiSettings appSettings = default!;
    [AutoInject] private IHttpClientFactory httpClientFactory = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var twilio = appSettings.Sms!.Providers.Twilio!;

            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.twilio.com/2010-04-01/Accounts/{twilio.AccountSid}.json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{twilio.AccountSid}:{twilio.AuthToken}")));

            using var response = await httpClientFactory.CreateClient().SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var status = (await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken)).GetProperty("status").GetString();

            return status is "active"
                ? HealthCheckResult.Healthy("Twilio account is active.")
                : HealthCheckResult.Degraded($"Twilio account status: {status}.");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Twilio SMS health check failed.", exp);
        }
    }
}
