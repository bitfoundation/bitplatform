//+:cnd:noEmit
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Features.Chatbot;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"),
    Authorize(Policy = AppFeatures.Management.ManageAiPrompt)]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [AutoInject] private IFusionCache cache = default!;

    [HttpGet]
    [EnableQuery]
    public IQueryable<SystemPromptDto> GetSystemPrompts()
    {
        return DbContext.SystemPrompts
            .Project();
    }

    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<SystemPromptDto> UpdateSystemPrompt(SystemPromptDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.SystemPrompts.FirstOrDefaultAsync(sp => sp.PromptKind == dto.PromptKind, cancellationToken)
            ?? throw new ResourceNotFoundException();

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        // Invalidate cache for the updated system prompt
        await cache.RemoveAsync($"SystemPrompt_{dto.PromptKind}");

        return entityToUpdate.Map();
    }
}
