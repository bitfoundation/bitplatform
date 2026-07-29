//+:cnd:noEmit
using Riok.Mapperly.Abstractions;
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Features.Chatbot;

/// <summary>
/// More info at src/Server/Boilerplate.Server.Api/Features/Mappers.md
/// </summary>
[Mapper]
public static partial class ChatbotMapper
{
    public static partial IQueryable<SystemPromptDto> Project(this IQueryable<SystemPrompt> query);

    public static partial SystemPromptDto Map(this SystemPrompt source);
    public static partial SystemPrompt Map(this SystemPromptDto source);
    public static partial void Patch(this SystemPromptDto source, SystemPrompt destination);
}
