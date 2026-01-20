//+:cnd:noEmit
namespace Boilerplate.Shared.Features.Chatbot;

[Mapper(UseDeepCloning = true)]
public static partial class SystemPromptsMapper
{
    public static partial void Patch(this SystemPromptDto source, SystemPromptDto destination);
}
