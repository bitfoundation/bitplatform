namespace Boilerplate.Shared.Controllers.Chatbot;

[Route("api/[controller]/[action]/")]
public interface IChatbotController : IAppController
{
    [HttpGet("{kind}")]
    Task<string> GetSystemPromptMarkdown(PromptKind kind, CancellationToken cancellationToken);

    /*[HttpPost("{kind}"), AuthorizedApi]
    Task UpdateSystemPrompt(PromptKind kind, string systemPrompt, CancellationToken cancellationToken);*/
}
