using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class TenantInvitationTemplate
{
    [Parameter] public TenantInvitationTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [Inject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class TenantInvitationTemplateModel
{
    public required string DisplayName { get; set; }

    public required string InviterDisplayName { get; set; }

    public required string TenantTitle { get; set; }

    public required Uri Link { get; set; }
}
