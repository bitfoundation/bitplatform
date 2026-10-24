using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

public partial class AccountAlreadyExistsTemplate
{
    [Parameter] public AccountAlreadyExistsTemplateModel Model { get; set; } = default!;
    [Parameter] public HttpContext HttpContext { get; set; } = default!;
    [AutoInject] public IStringLocalizer<EmailStrings> EmailLocalizer { get; set; } = default!;
}

public partial class AccountAlreadyExistsTemplateModel
{
    public string? DisplayName { get; set; }
    public required Uri SignInLink { get; set; }
    public required Uri ForgotPasswordLink { get; set; }
}
