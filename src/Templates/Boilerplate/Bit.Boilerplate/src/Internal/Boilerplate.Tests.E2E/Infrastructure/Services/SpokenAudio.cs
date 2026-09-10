using System.Text;
using System.Diagnostics;
using System.Security.Cryptography;

namespace Boilerplate.Tests.E2E.Infrastructure.Services;

/// <summary>
/// A recording of a sentence being spoken, for the tests that put words into the app's microphone.
/// <para>
/// Synthesised on the spot with the engine Windows ships (System.Speech / SAPI, driven through PowerShell like adb
/// and the emulator) rather than committed as a fixture, which would be an unreviewable binary stuck on one voice
/// and one language. This suite already runs on Windows.
/// </para>
/// <para>
/// 16 kHz mono 16-bit PCM: what a speech-to-text provider wants and what chromium's fake microphone requires. A
/// sentence is a few hundred kilobytes, well inside <c>ChatbotController.MaxSpeechUploadSizeBytes</c>.
/// </para>
/// </summary>
public static class SpokenAudio
{
    private static readonly SemaphoreSlim gate = new(1, 1);

    /// <summary>
    /// The wav file of <paramref name="text"/> being spoken, made once per text and kept - several tests and every
    /// retry ask for the same sentence, and synthesising is the slowest part of a fast test.
    /// </summary>
    public static async Task<string> WavFileOf(string text, CancellationToken cancellationToken)
    {
        var directory = Path.Combine(Path.GetTempPath(), "boilerplate-e2e-speech");
        Directory.CreateDirectory(directory);

        var name = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)))[..16];
        var wavFile = Path.Combine(directory, $"{name}.wav");

        await gate.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(wavFile) && new FileInfo(wavFile).Length > 0)
                return wavFile;

            await Synthesize(text, wavFile, cancellationToken);
        }
        finally
        {
            gate.Release();
        }

        return wavFile;
    }

    /// <summary>
    /// The path and the text are arguments rather than being written into the script, so nothing the caller passes
    /// can be read as PowerShell.
    /// </summary>
    private const string SynthesizeScript = """
        Add-Type -AssemblyName System.Speech
        $synthesizer = New-Object System.Speech.Synthesis.SpeechSynthesizer
        $synthesizer.Rate = -1
        $format = New-Object System.Speech.AudioFormat.SpeechAudioFormatInfo(16000, [System.Speech.AudioFormat.AudioBitsPerSample]::Sixteen, [System.Speech.AudioFormat.AudioChannel]::Mono)
        $synthesizer.SetOutputToWaveFile($args[0], $format)
        $synthesizer.Speak($args[1])
        $synthesizer.Dispose()
        """;

    private static async Task Synthesize(string text, string wavFile, CancellationToken cancellationToken)
    {
        var scriptFile = Path.ChangeExtension(wavFile, ".ps1");
        await File.WriteAllTextAsync(scriptFile, SynthesizeScript, cancellationToken);

        try
        {
            using var process = Process.Start(new ProcessStartInfo("powershell")
            {
                ArgumentList = { "-NoProfile", "-NonInteractive", "-ExecutionPolicy", "Bypass", "-File", scriptFile, wavFile, text },
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }) ?? throw new InvalidOperationException("powershell did not start.");

            var error = await process.StandardError.ReadToEndAsync(cancellationToken);
            await process.WaitForExitAsync(cancellationToken);

            if (process.ExitCode is not 0 || File.Exists(wavFile) is false || new FileInfo(wavFile).Length is 0)
            {
                throw new InvalidOperationException(
                    $"Windows' own speech engine did not record '{text}' to a wav file (exit {process.ExitCode}). {error}");
            }
        }
        finally
        {
            File.Delete(scriptFile);
        }
    }
}
