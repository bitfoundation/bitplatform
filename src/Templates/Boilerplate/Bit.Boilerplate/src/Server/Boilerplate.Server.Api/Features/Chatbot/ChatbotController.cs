//+:cnd:noEmit
using Boilerplate.Shared.Features.Chatbot;

namespace Boilerplate.Server.Api.Features.Chatbot;

[ApiVersion(1)]
[ApiController, Route("api/v{v:apiVersion}/[controller]/[action]"),
    //#if (multitenant == true)
    Authorize(Policy = AuthPolicies.TENANT_SELECTED),
    //#endif
    Authorize(Policy = AppFeatures.Management.SystemPrompts_Write)]
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
        //#if (multitenant == true)
        await cache.RemoveAsync($"SystemPrompt_{TenantProvider.GetCurrentTenantId()}_{dto.PromptKind}");
        //#else
        await cache.RemoveAsync($"SystemPrompt_{dto.PromptKind}");
        //#endif

        return entityToUpdate.Map();
    }
}
