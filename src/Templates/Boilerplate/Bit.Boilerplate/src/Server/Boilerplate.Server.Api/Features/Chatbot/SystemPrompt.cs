//+:cnd:noEmit
using Boilerplate.Shared.Features.Chatbot;
//#if (multitenancy == true)
using Boilerplate.Server.Api.Features.Tenants;
//#endif

namespace Boilerplate.Server.Api.Features.Chatbot;

public class SystemPrompt
//#if (multitenancy == true)
    : ITenantAware
//#endif
{
    public Guid Id { get; set; }

    public PromptKind PromptKind { get; set; }

    [Required]
    public string? Markdown { get; set; }

    public long Version { get; set; }

    //#if (multitenancy == true)
    [ForeignKey(nameof(TenantId))]
    public Tenant? Tenant { get; set; }

    public Guid TenantId { get; set; }
    //#endif
}
