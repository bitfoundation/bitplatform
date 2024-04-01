using Boilerplate.Client.Core.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity;

public partial class EmailConfirmationPage
{
    [AutoInject] IIdentityController identityController = default!;

    private bool isLoading = true;
    private string? error;
    private bool emailConfirmed = false;
    private BitMessageBarType emailConfirmationMessageType = BitMessageBarType.Error;

    [SupplyParameterFromQuery, Parameter] public string? Email { get; set; }
    [SupplyParameterFromQuery, Parameter] public string? Token { get; set; }

    protected override async Task OnAfterFirstRenderAsync()
    {
        try
        {
            await identityController.ConfirmEmail(new() { Email = Email!, Token = Token! });
            emailConfirmed = true;
        }
        catch (ResourceValidationException exp)
        {
            error = string.Join(", ", exp.Payload.Details.SelectMany(d => d.Errors));
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }

        await base.OnAfterFirstRenderAsync();
    }

    private void RedirectToSignIn()
    {
        NavigationManager.NavigateTo($"/sign-in?email={Uri.EscapeDataString(Email ?? string.Empty)}");
    }

    private async Task DoResendLink()
    {
        if (isLoading) return;

        isLoading = true;
        error = null;

        try
        {
            await identityController.SendConfirmationEmail(new() { Email = Email }, CurrentCancellationToken);

            emailConfirmationMessageType = BitMessageBarType.Success;

            error = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            emailConfirmationMessageType = BitMessageBarType.Error;

            error = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
