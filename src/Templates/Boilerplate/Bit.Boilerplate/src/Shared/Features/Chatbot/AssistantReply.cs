using System.ComponentModel;

namespace Boilerplate.Shared.Features.Chatbot;

/// <summary>
/// What the model is made to write: the reply, and what to offer the user next. It reaches the model as a json schema
/// to fill in (See <c>AppChatbot.AnswerFormat</c>), which is why each property's rules are on the property rather than
/// in the system prompt. Property order is streaming order. Arrives inside an <see cref="AssistantTurn"/>.
/// </summary>
public class AssistantReply
{
    [Description("Your reply to the user, as markdown. Write all of it here, and never mention this json, its properties or the suggestions below in it.")]
    public string? Answer { get; set; }

    [Description("Exactly 3 things the user might want to ask or do next, written as they would say them, each under 60 characters and in the language you answered in. Base them on where the conversation has got to. Only suggest what you can actually deliver with the tools you have, and for anything about finding or opening a page, call GetAppPages first and only suggest pages it returns.")]
    public List<string>? FollowUpSuggestions { get; set; }
}
