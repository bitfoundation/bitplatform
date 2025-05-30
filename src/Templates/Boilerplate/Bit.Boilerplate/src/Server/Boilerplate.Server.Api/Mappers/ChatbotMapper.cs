//+:cnd:noEmit
using Riok.Mapperly.Abstractions;
using Boilerplate.Shared.Dtos.Chatbot;

namespace Boilerplate.Server.Api.Mappers;

/// <summary>
/// More info at Server/Mappers/README.md
/// </summary>
[Mapper]
public static partial class ChatbotMapper
{
    public static partial IQueryable<SystemPromptDto> Project(this IQueryable<SystemPrompt> query);

    public static partial SystemPromptDto Map(this SystemPrompt source);
    public static partial SystemPrompt Map(this SystemPromptDto source);
    public static partial void Patch(this SystemPromptDto source, SystemPrompt dest);
}
