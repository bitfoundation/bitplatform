using Microsoft.AspNetCore.Components.Web;
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
        modalReference?.Close();
        signInModalTcs?.TrySetResult(false);

        // The callbacks close over LOCALS, not the fields. When they read the fields, a stale modal's close button
        // completed whichever TaskCompletionSource the newest SignIn() call had installed - returning false to a
        // caller nobody cancelled, closing the wrong dialog, and then throwing InvalidOperationException out of a
        // Blazor click handler on the second click (which tears down the circuit in Blazor Server).
        var currentTcs = new TaskCompletionSource<bool>();
        BitModalReference? currentModalReference = null;

        signInModalTcs = currentTcs;

        Dictionary<string, object> signInParameters = new()
        {
            { nameof(SignInModal.ReturnUrl), returnUrl ?? navigationManager.GetRelativePath() },
            { nameof(SignInModal.OnClose), () => { currentTcs.TrySetResult(false); currentModalReference?.Close(); } },
            { nameof(SignInModal.OnSuccess), () => { currentTcs.TrySetResult(true); currentModalReference?.Close(); } },
        };
        var modalParameters = new BitModalParameters()
        {
            Draggable = true,
            DragElementSelector = ".header-stack",
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => currentTcs.TrySetResult(false))
        };

        currentModalReference = await modalService.Show<SignInModal>(signInParameters, modalParameters);

        // Show() yields, so a second SignIn() - or a navigation - can complete currentTcs while this await is
        // pending. When that happens this call's modal is already obsolete: close it instead of publishing it as
        // the current one, which would leave the newer modal orphaned behind a stale field.
        if (currentTcs.Task.IsCompleted)
        {
            currentModalReference.Close();
        }
        else
        {
            modalReference = currentModalReference;
        }

        return await currentTcs.Task;
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
