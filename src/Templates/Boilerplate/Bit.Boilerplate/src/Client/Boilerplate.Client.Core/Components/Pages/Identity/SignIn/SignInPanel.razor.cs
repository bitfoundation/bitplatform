using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

public partial class SignInPanel
{
    [Parameter] public bool IsWaiting { get; set; }

    [Parameter] public SignInRequestDto Model { get; set; } = default!;

    [Parameter] public EventCallback<string?> OnSocialSignIn { get; set; }

    [Parameter] public EventCallback OnSendOtp { get; set; }


    private const string EmailKey = nameof(EmailKey);
    private const string PhoneKey = nameof(PhoneKey);

    private string selectedKey = EmailKey;

    private void OnSelectedKeyChanged(string key)
    {
        selectedKey = key;

        if (key == EmailKey)
        {
            Model.PhoneNumber = null;
        }

        if (key == PhoneKey)
        {
            Model.Email = null;
        }
    }
}
