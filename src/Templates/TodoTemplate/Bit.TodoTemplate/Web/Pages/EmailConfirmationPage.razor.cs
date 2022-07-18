namespace TodoTemplate.App.Pages;

public partial class EmailConfirmationPage
{
    [AutoInject] private HttpClient httpClient = default!;

    [AutoInject] private NavigationManager navigationManager = default!;

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
        navigationManager.NavigateTo("/sign-in");
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
            await httpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = Email
            }, AppJsonContext.Default.SendConfirmationEmailRequestDto);

            EmailConfirmationMessageType = BitMessageBarType.Success;

            EmailConfirmationMessage = TodoStrings.ResourceManager.Translate(TodoStrings.ResendConfirmationLinkMessage);
        }
        catch (KnownException e)
        {
            EmailConfirmationMessageType = BitMessageBarType.Error;

            EmailConfirmationMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
