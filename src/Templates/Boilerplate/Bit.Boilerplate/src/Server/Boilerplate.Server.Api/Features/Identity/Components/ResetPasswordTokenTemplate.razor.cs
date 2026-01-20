using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class ResetPasswordTokenTemplate
{
    [Parameter] public ResetPasswordTokenTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [Inject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class ResetPasswordTokenTemplateModel
{
    public string? DisplayName { get; set; }
    public required string Token { get; set; }
    public required Uri Link { get; set; }
}
