using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Routing;
using Boilerplate.Client.Core.Components.Pages.Identity.SignIn;

namespace Boilerplate.Client.Core.Infrastructure.Services;

/// <summary>
/// When users opt for external sign-in, they are seamlessly authenticated, whether they are new or returning users.
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

    public async Task<bool> SignIn(string? returnUrl = null)
    {
        signInModalTcs?.TrySetCanceled();
        signInModalTcs = new();

        Dictionary<string, object> signInParameters = new()
        {
            { nameof(SignInModal.ReturnUrl), returnUrl ?? navigationManager.GetRelativePath() },
            { nameof(SignInModal.OnClose), () => { signInModalTcs.SetResult(false); modalReference?.Close(); } },
            { nameof(SignInModal.OnSuccess), () => { signInModalTcs.SetResult(true); modalReference?.Close(); } },
        };
        var modalParameters = new BitModalParameters()
        {
            Draggable = true,
            DragElementSelector = ".header-stack",
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => signInModalTcs.SetResult(false))
        };

        modalReference = await modalService.Show<SignInModal>(signInParameters, modalParameters);

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
