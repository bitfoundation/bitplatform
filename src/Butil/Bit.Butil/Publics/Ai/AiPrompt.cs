namespace Bit.Butil;

/// <summary>
/// One message in a <see cref="LanguageModel"/> conversation - the shape both
/// <see cref="LanguageModelOptions.InitialPrompts"/> and <see cref="LanguageModelSession.Append"/>
/// take.
/// </summary>
public class AiPrompt
{
    /// <summary>
    /// Who is speaking: <c>"system"</c>, <c>"user"</c> or <c>"assistant"</c>. A <c>"system"</c>
    /// message is only allowed as the first of a set, and is the instruction the model is steered by.
    /// </summary>
    public string Role { get; set; } = "user";

    /// <summary>The message text.</summary>
    public string Content { get; set; } = string.Empty;
}
