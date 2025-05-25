using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

namespace Boilerplate.Client.Core.Services;

/// <summary>
/// When users opt for social sign-in, they are seamlessly authenticated, whether they are new or returning users.
/// To provide a similarly streamlined experience for email/password sign-in, this service displays a modal dialog, enabling users to log in quickly without leaving the current page.
/// For optimal use of this service, it is recommended to remove the sign-up page and its associated links from the project entirely.
/// Optionally, you may also eliminate the password field from the sign-in form to allow users to authenticate solely via phone/OTP or email/magic-link.
/// </summary>
public partial class SignInModalService : IAsyncDisposable
{
    public SignInModalService(BitModalService modalService, NavigationManager navigationManager)
    {
        this.modalService = modalService;
        this.navigationManager = navigationManager;
        this.navigationManager.LocationChanged += NavigationManager_LocationChanged;
    }

    private BitModalService modalService;
    private NavigationManager navigationManager;
    private BitModalReference? modalReference = null;
    private TaskCompletionSource<bool>? signInModalTcs;

    public async Task<bool> SignIn()
    {
        signInModalTcs?.TrySetCanceled();
        signInModalTcs = new();

        Dictionary<string, object> signInParameters = new()
        {
            { nameof(SignInPage.SignInPanelType), SignInPanelType.OtpOnly },
            { nameof(SignInPage.OnSuccess), () => { signInModalTcs.SetResult(true); modalReference?.Close(); } }
        };
        var modalParameters = new BitModalParameters()
        {
            FullHeight = true,
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => signInModalTcs.SetResult(false))
        };

        modalReference = await modalService.Show<SignInPage>(signInParameters, modalParameters);

        return await signInModalTcs.Task;
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        modalReference?.Close();
        signInModalTcs?.TrySetResult(false);
    }

    public async ValueTask DisposeAsync()
    {
        modalReference?.Close();
        signInModalTcs?.TrySetResult(false);
        navigationManager.LocationChanged -= NavigationManager_LocationChanged;
    }
}
