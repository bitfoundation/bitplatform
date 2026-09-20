using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Chatbot;

#pragma warning disable MEAI001 // ITextToSpeechClient is still experimental.
/// <summary>
/// Reads "OK" aloud in the voice read aloud uses (See ChatbotController.SynthesizeSpeech), so a wrong voice fails too.
/// </summary>
public class TextToSpeechHealthCheck(ITextToSpeechClient textToSpeechClient, ServerApiSettings settings) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await textToSpeechClient.GetAudioAsync("OK", new()
            {
                VoiceId = settings.AI?.OpenAI?.TextToSpeechVoice,
                AudioFormat = "mp3"
            }, cancellationToken);

            var audio = response.Contents.OfType<DataContent>().FirstOrDefault(content => content.Data.Length > 0)
                ?? throw new InvalidOperationException("The text to speech provider returned no audio.");

            return HealthCheckResult.Healthy("Text to speech answered", data: new Dictionary<string, object> { ["AudioBytes"] = audio.Data.Length });
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Text to speech did not answer", exp);
        }
    }
}
#pragma warning restore MEAI001
