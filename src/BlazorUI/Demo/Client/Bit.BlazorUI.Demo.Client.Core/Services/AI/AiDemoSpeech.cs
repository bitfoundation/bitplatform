namespace Bit.BlazorUI.Demo.Client.Core.Services.AI;

/// <summary>
/// The stand-in text-to-speech of the read-aloud demos. A real app hands the text to its own service (Azure Speech, a
/// model's speech endpoint, ...) and returns the audio it gets back; this one has no voice, so it plays a soft tone for
/// about as long as the text would take to read - enough to hear a reading start, play out, be stopped, and fall silent
/// as a dictation starts.
/// </summary>
public static class AiDemoSpeech
{
    private const int SampleRate = 8000;

    /// <summary>"Synthesizes" a text, after a short pause standing in for the round trip to a speech service.</summary>
    /// <param name="text">The text of the reply.</param>
    /// <param name="cancellationToken">Cancelled when the reading is stopped.</param>
    /// <returns>A WAV of a tone, or null for a text with no words.</returns>
    public static async Task<BitAiSpeechAudio?> SynthesizeAsync(string text, CancellationToken cancellationToken)
    {
        await Task.Delay(400, cancellationToken);

        var words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        if (words == 0) return null;

        // Roughly a reading pace, kept short enough for a demo.
        var seconds = Math.Clamp(words * 0.25, 1, 6);

        return BitAiSpeechAudio.FromBytes(Tone(seconds), "audio/wav");
    }

    // A mono 16-bit PCM WAV of a quiet 440 Hz tone that fades in and out, beeping once a second like words being read.
    private static byte[] Tone(double seconds)
    {
        var samples = (int)(SampleRate * seconds);
        var data = new byte[44 + samples * 2];

        void Text(int offset, string value)
        {
            for (var i = 0; i < value.Length; i++) data[offset + i] = (byte)value[i];
        }

        Text(0, "RIFF");
        BitConverter.TryWriteBytes(data.AsSpan(4), 36 + samples * 2);
        Text(8, "WAVE");
        Text(12, "fmt ");
        BitConverter.TryWriteBytes(data.AsSpan(16), 16);
        BitConverter.TryWriteBytes(data.AsSpan(20), (short)1);
        BitConverter.TryWriteBytes(data.AsSpan(22), (short)1);
        BitConverter.TryWriteBytes(data.AsSpan(24), SampleRate);
        BitConverter.TryWriteBytes(data.AsSpan(28), SampleRate * 2);
        BitConverter.TryWriteBytes(data.AsSpan(32), (short)2);
        BitConverter.TryWriteBytes(data.AsSpan(34), (short)16);
        Text(36, "data");
        BitConverter.TryWriteBytes(data.AsSpan(40), samples * 2);

        for (var i = 0; i < samples; i++)
        {
            var t = (double)i / SampleRate;
            var beat = t % 1;
            var envelope = beat < 0.6 ? Math.Sin(Math.PI * beat / 0.6) : 0;
            var value = (short)(Math.Sin(2 * Math.PI * 440 * t) * envelope * 3000);

            BitConverter.TryWriteBytes(data.AsSpan(44 + i * 2), value);
        }

        return data;
    }
}
