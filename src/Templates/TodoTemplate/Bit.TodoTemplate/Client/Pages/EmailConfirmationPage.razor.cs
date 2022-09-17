namespace TodoTemplate.Client.Pages;

public partial class EmailConfirmationPage
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "email-confirmed")]
    public bool EmailConfirmed { get; set; }

    public bool IsLoading { get; set; }

    public BitMessageBarType EmailConfirmationMessageType { get; set; }
    public string? EmailConfirmationMessage { get; set; }

    private void RedirectToSignIn()
    {
        NavigationManager.NavigateTo("/sign-in");
    }

    private async Task ResendLink()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        EmailConfirmationMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = Email
            }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            EmailConfirmationMessageType = BitMessageBarType.Success;

            EmailConfirmationMessage = Localizer[nameof(AppStrings.ResendConfirmationLinkMessage)];
        }
        catch (KnownException e)
        {
            EmailConfirmationMessageType = BitMessageBarType.Error;

            EmailConfirmationMessage = e.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
