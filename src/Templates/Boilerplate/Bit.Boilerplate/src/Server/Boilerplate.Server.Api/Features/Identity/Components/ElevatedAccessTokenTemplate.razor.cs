using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class ElevatedAccessTokenTemplate
{
    [Parameter] public ElevatedAccessTokenTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [Inject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class ElevatedAccessTokenTemplateModel
{
    public required string DisplayName { get; set; }

    public required string Token { get; set; }
}
