namespace Bit.Butil;

/// <summary>
/// The sampling knobs a <see cref="LanguageModel"/> exposes, and the ceilings on them - what
/// <see cref="LanguageModel.GetParams"/> reports.
/// </summary>
/// <remarks>
/// Use it to bound a UI slider to what the model actually accepts. What a runtime does with a
/// temperature above <see cref="MaxTemperature"/> - or a top-K above <see cref="MaxTopK"/> - is not
/// specified: Chromium clamps it to the ceiling rather than refusing the session, so an
/// out-of-range value silently becomes a different one. Clamp it yourself if the exact value matters.
/// </remarks>
public class AiModelParams
{
    /// <summary>The temperature used when none is asked for. Higher means more varied output.</summary>
    public double DefaultTemperature { get; set; }

    /// <summary>The highest temperature the model accepts.</summary>
    public double MaxTemperature { get; set; }

    /// <summary>The top-K used when none is asked for.</summary>
    public double DefaultTopK { get; set; }

    /// <summary>The highest top-K the model accepts.</summary>
    public double MaxTopK { get; set; }
}
