namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="LanguageModel"/> session. Every member is optional; the ones left null are not
/// sent, so the model's own defaults apply.
/// </summary>
/// <remarks>
/// The same options also decide the answer from <see cref="LanguageModel.Availability(LanguageModelOptions)"/> -
/// probe with the options you intend to create with, not with none.
/// <para>
/// <see cref="Temperature"/> and <see cref="TopK"/> are a pair: an option set carrying one without the
/// other is refused, which surfaces as a failed creation and as an Unavailable availability.
/// </para>
/// </remarks>
public class LanguageModelOptions
{
    /// <summary>
    /// How varied the output is. Keep it within <see cref="AiModelParams.MaxTemperature"/>: a higher
    /// value is clamped to that ceiling rather than honoured, so the session runs at a temperature
    /// you did not ask for.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// How many candidate tokens are sampled from. Keep it within <see cref="AiModelParams.MaxTopK"/>,
    /// which is clamped the same way as <see cref="Temperature"/>.
    /// </summary>
    public double? TopK { get; set; }

    /// <summary>
    /// The standing instruction the model is steered by. Sent as the conversation's first
    /// <c>system</c> message, ahead of <see cref="InitialPrompts"/>.
    /// </summary>
    public string? SystemPrompt { get; set; }

    /// <summary>
    /// A conversation to start from - previous turns the model should treat as having happened.
    /// </summary>
    public AiPrompt[]? InitialPrompts { get; set; }

    /// <summary>
    /// The languages the input will be in, as BCP 47 tags (<c>["en", "fr"]</c>). Declaring them lets
    /// the runtime refuse up front rather than answering badly.
    /// </summary>
    public string[]? ExpectedInputLanguages { get; set; }

    /// <summary>The language the output should be in, as a BCP 47 tag.</summary>
    public string? OutputLanguage { get; set; }
}
