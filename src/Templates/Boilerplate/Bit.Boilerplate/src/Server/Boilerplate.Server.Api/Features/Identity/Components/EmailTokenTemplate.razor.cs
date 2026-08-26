using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class EmailTokenTemplate
{
    [Parameter] public EmailTokenTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [AutoInject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class EmailTokenTemplateModel
{
    public string? Email { get; set; }
    public string? Token { get; set; }
    public Uri? Link { get; set; }
}
