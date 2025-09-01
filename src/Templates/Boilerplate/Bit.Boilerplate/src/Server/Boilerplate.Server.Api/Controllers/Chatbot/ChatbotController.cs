//+:cnd:noEmit
using Boilerplate.Shared.Dtos.Chatbot;
using Boilerplate.Shared.Controllers.Chatbot;

namespace Boilerplate.Server.Api.Controllers.Chatbot;

[ApiController, Route("api/[controller]/[action]"),
    Authorize(Policy = AppFeatures.Management.ManageAiPrompt)]
public partial class ChatbotController : AppControllerBase, IChatbotController
{
    [HttpGet]
    [EnableQuery]
    public IQueryable<SystemPromptDto> GetSystemPrompts(CancellationToken cancellationToken)
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

        return entityToUpdate.Map();
    }
}
