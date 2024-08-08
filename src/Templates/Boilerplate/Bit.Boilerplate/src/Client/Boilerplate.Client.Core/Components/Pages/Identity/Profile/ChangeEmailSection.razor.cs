﻿using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.Profile;

public partial class ChangeEmailSection
{
    private bool isWaiting;
    private string? message;
    private BitColor messageColor;
    private bool showConfirmation;
    private bool isEmailUnavailable = true;
    private ElementReference messageRef = default!;
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
            showConfirmation = true;
            isEmailUnavailable = false;
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

            showConfirmation = true;
            isEmailUnavailable = false;
            changeModel.Email = sendModel.Email;

            messageColor = BitColor.Success;
            message = Localizer[nameof(AppStrings.SuccessfulSendChangeEmailTokenMessage)];
            await messageRef.ScrollIntoView();
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
            await messageRef.ScrollIntoView();
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

            NavigationManager.NavigateTo(Urls.ProfilePage);
        }
        catch (KnownException e)
        {
            message = e.Message;
            messageColor = BitColor.Error;
            await messageRef.ScrollIntoView();
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
