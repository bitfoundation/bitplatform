namespace Bit.Butil;

/// <summary>
/// The sampling knobs a <see cref="LanguageModel"/> exposes, and the ceilings on them - what
/// <see cref="LanguageModel.GetParams"/> reports.
/// </summary>
/// <remarks>
/// Use it to clamp a UI slider to what the model actually accepts: passing a temperature above
/// <see cref="MaxTemperature"/> fails the session creation rather than being clamped for you.
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
