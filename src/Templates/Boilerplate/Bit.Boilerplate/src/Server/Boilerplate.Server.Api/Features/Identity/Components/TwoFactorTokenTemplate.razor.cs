using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class TwoFactorTokenTemplate
{
    [Parameter] public TwoFactorTokenTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [Inject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class TwoFactorTokenTemplateModel
{
    public required string DisplayName { get; set; }

    public required string Token { get; set; }
}
