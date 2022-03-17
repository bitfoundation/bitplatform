using System.Web;

namespace TodoTemplate.App.Pages;

public partial class EmailConfirmation
{
    private string? email;

    public bool EmailConfirmed { get; set; }

    public bool IsResendButtonEnabled { get; set; }
    public bool IsLoading { get; set; }

    public BitMessageBarType EmailConfirmationMessageType { get; set; }
    public string? EmailConfirmationMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    protected override void OnInitialized()
    {
        base.OnInitialized();

        var uri = new Uri(NavigationManager.Uri);
        var queryParam = HttpUtility.ParseQueryString(uri.Query);
        email = queryParam["email"];
    }

    protected override async Task OnInitAsync()
    {
        await CheckConfirmation();

        await base.OnInitAsync();
    }
    
    private async Task CheckConfirmation()
    {
        try
        {
            var response = await HttpClient.PostAsJsonAsync("Auth/EmailConfirmed", new()
            {
                Email = email
            }, ToDoTemplateJsonContext.Default.EmailConfirmedRequestDto);

            EmailConfirmed = Convert.ToBoolean(response.Content.ReadAsStringAsync().Result);

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
        catch (KnownException e)
        {
            EmailConfirmationMessageType = BitMessageBarType.Error;

            EmailConfirmationMessage = ErrorStrings.ResourceManager.Translate(e.Message);
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
            await HttpClient.PostAsJsonAsync("Auth/SendEmailConfirmLink", new()
            {
                Email = email
            }, ToDoTemplateJsonContext.Default.SendEmailConfirmLinkRequestDto);

            EmailConfirmationMessageType = BitMessageBarType.Success;

            EmailConfirmationMessage = "The confirmation link has been re-sent.";
        }
        catch (KnownException e)
        {
            EmailConfirmationMessageType = BitMessageBarType.Error;

            EmailConfirmationMessage = ErrorStrings.ResourceManager.Translate(e.Message, email!);

            IsResendButtonEnabled = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
