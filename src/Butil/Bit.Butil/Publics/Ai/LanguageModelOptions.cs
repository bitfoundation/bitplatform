namespace Bit.Butil;

/// <summary>
/// Shapes a <see cref="LanguageModel"/> session. Every member is optional; the ones left null are not
/// sent, so the model's own defaults apply.
/// </summary>
/// <remarks>
/// The same options also decide the answer from <see cref="LanguageModel.Availability(LanguageModelOptions)"/> -
/// probe with the options you intend to create with, not with none.
/// </remarks>
public class LanguageModelOptions
{
    /// <summary>
    /// How varied the output is. Must not exceed <see cref="AiModelParams.MaxTemperature"/> - a
    /// higher value fails the creation rather than being clamped.
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>How many candidate tokens are sampled from. Must not exceed <see cref="AiModelParams.MaxTopK"/>.</summary>
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
