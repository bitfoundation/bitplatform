using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Chatbot.VoiceCall;

/// <summary>
/// A call needs the browser's WebRTC offer, so the provider validates the call's session (See <see cref="VoiceCallRunner"/>) instead.
/// </summary>
public partial class RealtimeHealthCheck : IHealthCheck
{
    [AutoInject] private VoiceCallRunner voiceCallRunner = default!;
    [AutoInject] private OpenAIRealtimeCallClient realtimeCallClient = default!;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var session = voiceCallRunner.CreateSession(instructions: "Health check.", language: null, functions: []);

            await realtimeCallClient.CreateClientSecret(session, cancellationToken);

            return HealthCheckResult.Healthy("Realtime session is accepted");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Realtime session is refused", exp);
        }
    }
}
