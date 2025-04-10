//+:cnd:noEmit
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Server.Api.Controllers.AI;

[ApiController, Route("api/[controller]/[action]")]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [HttpGet("{kind}")]
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS, Roles = AppRoles.SUPER_ADMIN)]
    public async Task<string> GetSystemPromptMarkdown(PromptKind kind, CancellationToken cancellationToken)
    {
        return (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == kind, cancellationToken))?.Markdown
            ?? throw new ResourceNotFoundException();
    }

    /*[HttpPost("{kind}")]
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS, Roles = AppRoles.SUPER_ADMIN)]
    public async Task UpdateSystemPrompt(PromptKind kind, [FromBody] string markdown, CancellationToken cancellationToken)
    {
        var systemPrompt = (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == kind, cancellationToken))
            ?? throw new ResourceNotFoundException();

        systemPrompt.Markdown = markdown;

        await DbContext.SaveChangesAsync(cancellationToken);
    }*/
}
