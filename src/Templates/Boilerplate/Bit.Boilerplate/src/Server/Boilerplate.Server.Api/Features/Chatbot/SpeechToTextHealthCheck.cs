using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Boilerplate.Server.Api.Features.Chatbot;

#pragma warning disable MEAI001 // ISpeechToTextClient is still experimental.
/// <summary>
/// Transcribes half a second of generated silence; any answer proves the key, the model and the quota.
/// </summary>
public class SpeechToTextHealthCheck(ISpeechToTextClient speechToTextClient) : IHealthCheck
{
    public static readonly TimeSpan SilenceDuration = TimeSpan.FromMilliseconds(500); // OpenAI rejects anything under 0.1 seconds.

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using var silence = CreateSilentWav(SilenceDuration);

            await speechToTextClient.GetTextAsync(silence, cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Speech to text answered");
        }
        catch (Exception exp)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, "Speech to text did not answer", exp);
        }
    }

    /// <summary>16 kHz, 16-bit, mono PCM. The provider recognizes the format from the RIFF header.</summary>
    private static MemoryStream CreateSilentWav(TimeSpan duration)
    {
        const int sampleRate = 16_000;
        const short bitsPerSample = 16;
        const short channels = 1;
        var blockAlign = (short)(channels * bitsPerSample / 8);
        var dataLength = (int)(sampleRate * duration.TotalSeconds) * blockAlign;

        var stream = new MemoryStream(44 + dataLength);

        using (var writer = new BinaryWriter(stream, Encoding.ASCII, leaveOpen: true))
        {
            writer.Write("RIFF"u8);
            writer.Write(36 + dataLength);
            writer.Write("WAVE"u8);
            writer.Write("fmt "u8);
            writer.Write(16); // PCM format chunk size
            writer.Write((short)1); // PCM
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(sampleRate * blockAlign); // Byte rate
            writer.Write(blockAlign);
            writer.Write(bitsPerSample);
            writer.Write("data"u8);
            writer.Write(dataLength);
            writer.Write(new byte[dataLength]); // Zeros are silence.
        }

        stream.Position = 0;
        return stream;
    }
}
#pragma warning restore MEAI001
