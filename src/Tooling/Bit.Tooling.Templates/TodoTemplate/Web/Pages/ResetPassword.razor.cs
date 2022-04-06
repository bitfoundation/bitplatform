using TodoTemplate.App.Models;
using TodoTemplate.Shared.Dtos.Account;

namespace TodoTemplate.App.Pages;

public partial class ResetPassword
{
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Email { get; set; }

    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; set; }

    public ResetPasswordModel ResetPasswordModel { get; set; } = new();

    public bool IsLoading { get; set; }

    public BitMessageBarType ResetPasswordMessageType { get; set; }

    public string? ResetPasswordMessage { get; set; }

    [Inject] public HttpClient HttpClient { get; set; } = default!;

    [Inject] public NavigationManager NavigationManager { get; set; } = default!;

    [Inject] public ITodoTemplateAuthenticationService TodoTemplateAuthenticationService { get; set; } = default!;

    private async Task Submit()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;
        ResetPasswordMessage = null;

        try
        {
            await HttpClient.PostAsJsonAsync("Auth/ResetPassword", new()
            {
                Email = Email,
                Token = Token,
                Password = ResetPasswordModel.NewPassword
            }, TodoTemplateJsonContext.Default.ResetPasswordRequestDto);

            ResetPasswordMessageType = BitMessageBarType.Success;

            ResetPasswordMessage = "Your password changed successfully.";

            await TodoTemplateAuthenticationService.SignIn(new SignInRequestDto
            {
                UserName = Email,
                Password = ResetPasswordModel.NewPassword
            });

            NavigationManager.NavigateTo("/");
        }
        catch (KnownException e)
        {
            ResetPasswordMessageType = BitMessageBarType.Error;

            ResetPasswordMessage = ErrorStrings.ResourceManager.Translate(e.Message, Email!);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
