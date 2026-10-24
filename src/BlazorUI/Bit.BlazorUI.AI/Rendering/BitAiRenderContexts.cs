namespace Bit.BlazorUI;

/// <summary>
/// What a custom part renderer is given.
/// </summary>
/// <param name="Part">The part to render.</param>
/// <param name="Message">The message it belongs to.</param>
/// <param name="Conversation">The conversation, when the part is being rendered inside one.</param>
/// <param name="Session">The session driving the conversation, when there is one.</param>
/// <remarks>
/// The session is included so a renderer can act as well as display: a custom approval card can
/// call back into the session without the application having to thread state through cascading
/// values.
/// </remarks>
public sealed record BitAiMessagePartContext(
    BitAiMessagePart Part,
    BitAiMessage Message,
    BitAiConversation? Conversation = null,
    BitAiChatSession? Session = null);

/// <summary>
/// What a custom tool renderer is given.
/// </summary>
/// <param name="Call">The tool call, including its arguments, status, and result.</param>
/// <param name="Message">The message it belongs to.</param>
/// <param name="Session">The session driving the conversation, when there is one.</param>
public sealed record BitAiToolCallContext(BitAiToolCallPart Call, BitAiMessage Message, BitAiChatSession? Session = null);

/// <summary>
/// What a custom artifact renderer is given.
/// </summary>
/// <param name="Artifact">The artifact to render.</param>
/// <param name="IsExpanded">Whether the artifact is being shown full size rather than as a card.</param>
public sealed record BitAiArtifactContext(BitAiArtifact Artifact, bool IsExpanded = false);

/// <summary>
/// What a custom structured-data renderer is given.
/// </summary>
/// <param name="Part">The structured part, which may still be streaming.</param>
/// <param name="Message">The message it belongs to.</param>
public sealed record BitAiStructuredDataContext(BitAiStructuredDataPart Part, BitAiMessage Message);

/// <summary>
/// What a custom citation renderer is given.
/// </summary>
/// <param name="Citation">The citation.</param>
/// <param name="Message">The message it belongs to.</param>
/// <param name="OnActivate">
/// What activating the citation does where it is drawn - the <c>OnCitationClick</c> of the view drawing it - so the chip
/// a renderer draws can act the way the built-in one does. Call it through <see cref="ActivateAsync"/>.
/// </param>
public sealed record BitAiCitationContext(
    BitAiCitation Citation,
    BitAiMessage? Message = null,
    EventCallback<BitAiCitation> OnActivate = default)
{
    /// <summary>
    /// Activates the citation the way the built-in chip does when it is clicked: raises <see cref="OnActivate"/> with
    /// <see cref="Citation"/>. Does nothing when the view drawing it was given no callback.
    /// </summary>
    /// <returns>A task that completes when the callback has run.</returns>
    public Task ActivateAsync() => OnActivate.InvokeAsync(Citation);
}

/// <summary>Renders a message part.</summary>
/// <param name="context">The part and its surroundings.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiMessagePartRenderer(BitAiMessagePartContext context);

/// <summary>Renders a tool call.</summary>
/// <param name="context">The call and its surroundings.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiToolCallRenderer(BitAiToolCallContext context);

/// <summary>Renders an artifact.</summary>
/// <param name="context">The artifact and how it is being shown.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiArtifactRenderer(BitAiArtifactContext context);

/// <summary>Renders a structured value.</summary>
/// <param name="context">The part and its surroundings.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiStructuredDataRenderer(BitAiStructuredDataContext context);

/// <summary>Renders a citation.</summary>
/// <param name="context">The citation and its surroundings.</param>
/// <returns>The markup.</returns>
public delegate RenderFragment BitAiCitationRenderer(BitAiCitationContext context);
