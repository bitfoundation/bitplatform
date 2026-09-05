namespace Bit.Butil;

/// <summary>
/// What JS reports about a decoded <c>AudioBuffer</c>. The samples themselves stay in JS and are
/// reached through <see cref="AudioBufferHandle"/>.
/// </summary>
internal class AudioBufferJsInfo
{
    public double Duration { get; set; }

    public int SampleRate { get; set; }

    public int NumberOfChannels { get; set; }

    public int Length { get; set; }
}
