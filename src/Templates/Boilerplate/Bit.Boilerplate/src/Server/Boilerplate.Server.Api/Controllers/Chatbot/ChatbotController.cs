//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Server.Api.Controllers.Chatbot;

[ApiController, Route("api/[controller]/[action]"), 
    Authorize(Policy = AppFeatures.Management.ManageAiPrompt)]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [HttpGet("{kind}")]
    public async Task<string> GetSystemPromptMarkdown(PromptKind kind, CancellationToken cancellationToken)
    {
        return (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == kind, cancellationToken))?.Markdown
            ?? throw new ResourceNotFoundException();
    }

    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task UpdateSystemPrompt(UpdateSystemPromptDto request, CancellationToken cancellationToken)
    {
        var systemPrompt = (await DbContext.SystemPrompts.FirstOrDefaultAsync(p => p.PromptKind == request.Kind, cancellationToken))
            ?? throw new ResourceNotFoundException();

        systemPrompt.Markdown = request.Markdown;

        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
