using System.Security.Principal;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IDisposable
{
    private bool disposed;
    private bool isMenuOpen;
    private BitDir? currentDir;
    private bool isUserAuthenticated;
    private Action unsubscribeCultureChange = default!;

    [AutoInject] private IPubSubService pubSubService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService = default!;
    [AutoInject] ILogger<AuthenticationManager> authLogger = default!;

    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            authManager.AuthenticationStateChanged += IsUserAuthenticated;

            var authContext = await prerenderStateService.GetValue<(string? userId, string? userName, bool isUserAuthenticated)>(async () =>
            {
                var user = (await AuthenticationStateTask).User;

                return user.IsAuthenticated() ? (user.GetUserId().ToString(), user.GetUserName(), user.IsAuthenticated()) : default;
            });

            isUserAuthenticated = authContext.isUserAuthenticated;

            authLogger.LogInformation("Authentication State: {UserId}, {UserName}, {IsUserAuthenticated}", authContext.userId, authContext.userName, authContext.isUserAuthenticated);

            unsubscribeCultureChange = pubSubService.Subscribe(PubSubMessages.CULTURE_CHANGED, async _ =>
            {
                SetCurrentDir();

                StateHasChanged();
            });

            SetCurrentDir();

            await base.OnInitializedAsync();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }

    protected override void OnParametersSet()
    {
        // TODO: we can try to recover from exception after rendering the ErrorBoundary with this line.
        // but for now it's better to persist the error ui until a force refresh.
        // ErrorBoundaryRef.Recover();

        base.OnParametersSet();
    }

    private void SetCurrentDir()
    {
        var currentCulture = CultureInfo.CurrentCulture;

        currentDir = currentCulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    private void ToggleMenuHandler()
    {
        isMenuOpen = !isMenuOpen;
    }

    private async void IsUserAuthenticated(Task<AuthenticationState> task)
    {
        try
        {
            isUserAuthenticated = (await task).User.IsAuthenticated();
        }
        catch (Exception ex)
        {
            exceptionHandler.Handle(ex);
        }
        finally
        {
            await InvokeAsync(StateHasChanged);
        }
    }

    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed || disposing is false) return;

        authManager.AuthenticationStateChanged -= IsUserAuthenticated;

        unsubscribeCultureChange();

        disposed = true;
    }
}
