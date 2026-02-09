namespace Boilerplate.Shared.Features.Chatbot;

[AuthorizedApi]
[Route("api/v1/[controller]/[action]/")]
public interface IChatbotController : IAppController
{
    [HttpGet]
    Task<List<SystemPromptDto>> GetSystemPrompts(CancellationToken cancellationToken) => default!;

    [HttpPost]
    Task<SystemPromptDto> UpdateSystemPrompt(SystemPromptDto dto, CancellationToken cancellationToken);
}
