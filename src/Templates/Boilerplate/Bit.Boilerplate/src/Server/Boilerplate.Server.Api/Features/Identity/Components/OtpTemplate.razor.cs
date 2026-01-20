using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class OtpTemplate
{
    [Parameter] public OtpTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [Inject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class OtpTemplateModel
{
    public string? DisplayName { get; set; }
    public required string Token { get; set; }
    public required Uri Link { get; set; }
}
