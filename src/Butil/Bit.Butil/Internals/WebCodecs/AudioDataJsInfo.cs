namespace Bit.Butil;

/// <summary>
/// What JS reports about an <c>AudioData</c> it is holding, reached from .NET through
/// <see cref="AudioDataHandle"/>.
/// </summary>
internal class AudioDataJsInfo
{
    public long Timestamp { get; set; }

    public long? Duration { get; set; }

    public int SampleRate { get; set; }

    public int NumberOfFrames { get; set; }

    public int NumberOfChannels { get; set; }

    public string Format { get; set; } = string.Empty;
}
