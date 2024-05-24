using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class ChangeEmailSection
{
    private bool isWaiting;
    private string? message;
    private bool isEmailedChanged;
    private bool showConfirmation;
    private bool isEmailUnavailable = true;
    private readonly ChangeEmailRequestDto changeModel = new();
    private readonly SendEmailTokenRequestDto sendModel = new();


    [AutoInject] private IUserController userController = default!;


    [Parameter] public bool Loading { get; set; }

    [Parameter] public string? Email { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "email")]
    public string? EmailQueryString { get; set; }

    [Parameter, SupplyParameterFromQuery(Name = "emailToken")]
    public string? EmailTokenQueryString { get; set; }

    protected override async Task OnInitAsync()
    {
        if (string.IsNullOrEmpty(EmailQueryString) is false)
        {
            isEmailUnavailable = false;
            showConfirmation = true;
            changeModel.Email = EmailQueryString;

            if (string.IsNullOrEmpty(EmailTokenQueryString) is false)
            {
                changeModel.Token = EmailTokenQueryString;

                if (InPrerenderSession is false)
                {
                    await ChangeEmail();
                }
            }
        }

        await base.OnInitAsync();
    }

    private async Task SendToken()
    {
        if (isWaiting || sendModel.Email == Email) return;

        isWaiting = true;
        message = null;

        try
        {
            await userController.SendChangeEmailToken(sendModel, CurrentCancellationToken);

            isEmailUnavailable = false;
            showConfirmation = true;
            changeModel.Email = sendModel.Email;
        }
        catch (KnownException e)
        {
            message = e.Message;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task ChangeEmail()
    {
        if (isWaiting) return;

        isWaiting = true;
        message = null;

        try
        {
            await userController.ChangeEmail(changeModel, CurrentCancellationToken);

            NavigationManager.NavigateTo("profile");
        }
        catch (KnownException e)
        {
            message = e.Message;
        }
        finally
        {
            isWaiting = false;
        }
    }

    private async Task GoBack()
    {
        sendModel.Email = null;
        changeModel.Email = null;

        showConfirmation = false;
        isEmailUnavailable = true;
    }
}
