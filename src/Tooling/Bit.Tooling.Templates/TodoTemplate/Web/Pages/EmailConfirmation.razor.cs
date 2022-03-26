namespace TodoTemplate.App.Pages;

public partial class EmailConfirmation
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery(Name = "email-confirmed")]
    public bool EmailConfirmed { get; set; }

    public bool IsResendButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType EmailConfirmationMessageType { get; set; }
    public string? EmailConfirmationMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (EmailConfirmed)
        {
            EmailConfirmationMessageType = BitMessageBarType.Success;
            EmailConfirmationMessage = "Congratulation! Your email is confirmed.";
        }
        else
        {
            EmailConfirmationMessageType = BitMessageBarType.Warning;
            EmailConfirmationMessage = "Oops! Unfortunately, your email was not confirmed.";
            IsResendButtonEnabled = true;
        }
    }

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
        IsResendButtonEnabled = false;
        EmailConfirmationMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/SendConfirmationEmail", new()
            {
                Email = Email
            }, ToDoTemplateJsonContext.Default.SendConfirmationEmailRequestDto);

            EmailConfirmationMessageType = BitMessageBarType.Success;

            EmailConfirmationMessage = "The confirmation link has been re-sent.";
        }
        catch (KnownException e)
        {
            EmailConfirmationMessageType = BitMessageBarType.Error;

            EmailConfirmationMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
        }
        finally
        {
            IsResendButtonEnabled = true;
            IsLoading = false;
        }
    }
}
