using BlazorWeb.Shared.Dtos.Identity;

namespace BlazorWeb.Client.Pages.Identity;

public partial class ResetPasswordPage
{
    private bool isLoading;
    private string? resetPasswordMessage;
    private BitMessageBarType resetPasswordMessageType;
    private ResetPasswordRequestDto resetPasswordModel = new();

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    protected override async Task OnInitAsync()
    {
        resetPasswordModel.Email = Email;
        resetPasswordModel.Token = Token;

        await base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if ((await AuthenticationStateTask).User.IsAuthenticated())
        {
            NavigationManager.NavigateTo("/");
        }
    }

    private async Task DoSubmit()
    {
        if (isLoading) return;

        isLoading = true;
        resetPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Identity/ResetPassword", resetPasswordModel, AppJsonContext.Default.ResetPasswordRequestDto);

            resetPasswordMessageType = BitMessageBarType.Success;

            resetPasswordMessage = Localizer[nameof(AppStrings.PasswordChangedSuccessfullyMessage)];
        }
        catch (KnownException e)
        {
            resetPasswordMessageType = BitMessageBarType.Error;

            resetPasswordMessage = e.Message;
        }
        finally
        {
            isLoading = false;
        }
    }
}
