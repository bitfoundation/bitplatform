using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class ResetPasswordPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isWaiting;
    private bool showEmail;
    private bool showPhone;
    private bool isPasswordChanged;
    private string? errorMessage;
    private ResetPasswordRequestDto model = new();

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "phoneNumber")]
    public string? PhoneNumberQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "token")]
    public string? TokenQueryString { get; set; }

    protected override async Task OnInitAsync()
    {
        model.Email = EmailQueryString;
        model.PhoneNumber = PhoneNumberQueryString;
        model.Token = TokenQueryString;

        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            showEmail = true;
        }
        else if (string.IsNullOrEmpty(PhoneNumberQueryString) is false)
        {
            showPhone = true;
        }
        else
        {
            showEmail = showPhone = true;
        }

        await base.OnInitAsync();
    }

    private async Task Submit()
    {
        if (isWaiting) return;

        isWaiting = true;
        errorMessage = null;

        try
        {
            await identityController.ResetPassword(model, CurrentCancellationToken);

            isPasswordChanged = true;
        }
        catch (KnownException e)
        {
            errorMessage = e.Message;
        }
        finally
        {
            isWaiting = false;
        }
    }
}
