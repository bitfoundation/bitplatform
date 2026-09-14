using System.ComponentModel;

namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// What the model is made to write. It reaches the model as a json schema to fill in (See <c>AppChatbot.AnswerFormat</c>),
/// which is why the answer's rules are on its property rather than in the system prompt. Arrives inside an
/// <see cref="AssistantTurn"/>.
/// </summary>
public class AssistantReply
{
    [Description("Your reply to the user, as markdown. Write all of it here, and never mention this json or its properties in it.")]
    public string? Answer { get; set; }
}
