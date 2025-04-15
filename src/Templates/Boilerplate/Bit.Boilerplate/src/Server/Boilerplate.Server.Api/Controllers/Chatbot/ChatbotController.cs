//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Server.Api.Controllers.Chatbot;

[ApiController, Route("api/[controller]/[action]")]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [HttpGet("{kind}"), AllowAnonymous]
    public async Task<string> GetSystemPromptMarkdown(PromptKind kind, CancellationToken cancellationToken)
    {
        return (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == kind, cancellationToken))?.Markdown
            ?? throw new ResourceNotFoundException();
    }

    [HttpPost, Authorize(Roles = AppRoles.SUPER_ADMIN, Policy = AuthPolicies.PRIVILEGED_ACCESS)]
    public async Task UpdateSystemPrompt(UpdateSystemPromptDto request, CancellationToken cancellationToken)
    {
        var systemPrompt = (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == request.Kind, cancellationToken))
            ?? throw new ResourceNotFoundException();

        systemPrompt.Markdown = request.Markdown;

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
