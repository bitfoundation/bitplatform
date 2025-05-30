using Boilerplate.Shared.Dtos.Chatbot;

namespace Boilerplate.Shared.Controllers.Chatbot;

[AuthorizedApi]
[Route("api/[controller]/[action]/")]
public interface IChatbotController : IAppController
{
    [HttpGet("{kind}")]
    Task<SystemPromptDto> GetSystemPrompt(PromptKind kind, CancellationToken cancellationToken);

    [HttpPost]
    Task<SystemPromptDto> UpdateSystemPrompt(SystemPromptDto dto, CancellationToken cancellationToken);
}
