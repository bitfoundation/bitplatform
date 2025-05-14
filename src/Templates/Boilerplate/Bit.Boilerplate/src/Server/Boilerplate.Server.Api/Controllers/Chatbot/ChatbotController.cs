//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Server.Api.Controllers.Chatbot;

[ApiController, Route("api/[controller]/[action]"),
    Authorize(Policy = AppFeatures.Management.ManageAiPrompt)]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [HttpGet("{kind}")]
    public async Task<SystemPromptDto> GetSystemPrompt(PromptKind kind, CancellationToken cancellationToken)
    {
        return await DbContext.SystemPrompts
            .Where(p => p.PromptKind == kind)
            .Project()
            .FirstOrDefaultAsync(cancellationToken) ?? throw new ResourceNotFoundException();
    }

    [HttpPost, Authorize(Policy = AuthPolicies.ELEVATED_ACCESS)]
    public async Task<SystemPromptDto> UpdateSystemPrompt(SystemPromptDto dto, CancellationToken cancellationToken)
    {
        var entityToUpdate = await DbContext.SystemPrompts.FirstOrDefaultAsync(sp => sp.PromptKind == dto.PromptKind, cancellationToken)
            ?? throw new ResourceNotFoundException(Localizer[nameof(AppStrings.CategoryCouldNotBeFound)]);

        dto.Patch(entityToUpdate);

        await DbContext.SaveChangesAsync(cancellationToken);

        return entityToUpdate.Map();
    }
}
