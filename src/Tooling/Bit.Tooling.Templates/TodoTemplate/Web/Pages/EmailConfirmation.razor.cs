using Bit.Tooling.SourceGenerators;

namespace TodoTemplate.App.Pages;

public partial class EmailConfirmation
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

    [AutoInject] public HttpClient HttpClient { get; set; } = default!;

    [AutoInject] public NavigationManager NavigationManager { get; set; } = default!;

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
            }, TodoTemplateJsonContext.Default.SendConfirmationEmailRequestDto);

            EmailConfirmationMessageType = BitMessageBarType.Success;

            EmailConfirmationMessage = "The confirmation link has been re-sent to your email address.";
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
