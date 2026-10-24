using Microsoft.AspNetCore.Components;

namespace Boilerplate.Server.Api.Features.Identity.Components;

/// <summary>
/// Signing up with an email that already has an account looks exactly like a new sign-up, so the sign-up form can't be
/// used to find out who has an account here. The real owner gets this email instead: if it was them, it links to sign-in
/// and to the password reset; if not, they can ignore it (See IdentityController.SignUp).
/// </summary>
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
