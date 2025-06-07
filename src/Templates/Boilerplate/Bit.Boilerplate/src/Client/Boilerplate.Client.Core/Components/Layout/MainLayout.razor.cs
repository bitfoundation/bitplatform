//+:cnd:noEmit
using System.Reflection;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Controllers.Identity;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private BitDir? currentDir;
    private bool isNavPanelOpen;
    private bool? isIdentityPage;
    private bool isNavPanelToggled;
    private readonly BitModalParameters modalParameters = new() { Classes = new() { Root = "modal" } };

    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    private bool? isOnline;

    private UserDto? currentUser;
    private AppThemeType? currentTheme;
    private RouteData? currentRouteData;
    private List<Action> unsubscribers = [];
    private CancellationTokenSource getCurrentUserCts = new();

    [AutoInject] private Keyboard keyboard = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private BitExtraServices bitExtraServices = default!;
    [AutoInject] private IAppUpdateService appUpdateService = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private SignInModalService signInModalService = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService = default!;


    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            var inPrerenderSession = RendererInfo.IsInteractive is false;
            isOnline = await prerenderStateService.GetValue<bool?>(nameof(isOnline), async () => isOnline ?? inPrerenderSession is true ? true : null);
            // During pre-rendering, if any API calls are made, the `isOnline` value will be set 
            // using PubSub's `ClientPubSubMessages.IS_ONLINE_CHANGED`, depending on the success 
            // or failure of the API call. However, if a pre-rendered page has no HTTP API call 
            // dependencies, its value remains null. 
            // Even though Server.Web and Server.Api may be deployed on different servers, 
            // we can still assume that if the client is displaying a pre-rendered result, it is online.

            navigationManager.LocationChanged += NavigationManager_LocationChanged;
            authManager.AuthenticationStateChanged += AuthManager_AuthenticationStateChanged;

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.CULTURE_CHANGED, async _ =>
            {
                SetCurrentDir();
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.THEME_CHANGED, async payload =>
            {
                if (payload is null) return;
                currentTheme = (AppThemeType)payload;
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.ROUTE_DATA_UPDATED, async payload =>
            {
                currentRouteData = (RouteData?)payload;
                SetRouteData();
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.IS_ONLINE_CHANGED, async payload =>
            {
                telemetryContext.IsOnline = isOnline = (bool?)payload;
                await InvokeAsync(StateHasChanged);
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.OPEN_NAV_PANEL, async _ =>
            {
                isNavPanelOpen = true;
                isNavPanelToggled = false;
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientPubSubMessages.PROFILE_UPDATED, async payload =>
            {
                if (payload is null) return;

                currentUser = payload is JsonElement jsonDocument
                    ? jsonDocument.Deserialize(jsonSerializerOptions.GetTypeInfo<UserDto>())! // PROFILE_UPDATED can be invoked from server through SignalR
                    : (UserDto)payload;

                await InvokeAsync(StateHasChanged);
            }));

            await SetCurrentUser(AuthenticationStateTask);

            SetCurrentDir();
            currentTheme = await themeService.GetCurrentTheme();

            await bitExtraServices.AddRootCssClasses();
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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await keyboard.Add(ButilKeyCodes.KeyX, OpenDiagnosticModal, ButilModifiers.Ctrl | ButilModifiers.Shift);
        }
    }


    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        // The sign-in and sign-up buttons href are bound to NavigationManager.GetRelativePath().
        // To ensure the bound values update with each route change, it's necessary to call StateHasChanged on location changes.
        StateHasChanged();
    }

    private async void AuthManager_AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            await SetCurrentUser(task);
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

    private async Task SetCurrentUser(Task<AuthenticationState> task)
    {
        var authUser = (await task).User;

        await SetNavPanelItems(authUser);

        using var currentCts = getCurrentUserCts;
        getCurrentUserCts = new();
        await currentCts.CancelAsync();

        if (authUser.IsAuthenticated() is false)
        {
            currentUser = null;
        }
        else if (authUser.GetUserId() != currentUser?.Id)
        {
            currentUser = await userController.GetCurrentUser(getCurrentUserCts.Token);
        }
    }

    private void SetCurrentDir()
    {
        currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    private void SetRouteData()
    {
        if (currentRouteData is null)
        {
            isIdentityPage = false;
            return;
        }

        var type = currentRouteData.PageType;

        if (type.GetCustomAttributes<AuthorizeAttribute>(inherit: true).Any())
        {
            isIdentityPage = false;
            return;
        }

        if (type.Namespace?.Contains("Client.Core.Components.Pages.Identity") is true)
        {
            isIdentityPage = true;
            isNavPanelOpen = false;
            return;
        }

        isIdentityPage = false;
    }

    private void OpenDiagnosticModal()
    {
        pubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }

    private string GetMainCssClass()
    {
        return isIdentityPage is true ? "identity"
             : isIdentityPage is false ? "non-identity"
             : string.Empty;
    }

    private async Task ModalSignIn()
    {
        await signInModalService.SignIn();
    }

    private async Task UpdateApp()
    {
        try
        {
            await appUpdateService.ForceUpdate();
        }
        catch (Exception exp)
        {
            exceptionHandler.Handle(exp);
        }
    }


    public async ValueTask DisposeAsync()
    {
        if (getCurrentUserCts is not null)
        {
            await getCurrentUserCts.CancelAsync();
            getCurrentUserCts.Dispose();
        }

        navigationManager.LocationChanged -= NavigationManager_LocationChanged;
        authManager.AuthenticationStateChanged -= AuthManager_AuthenticationStateChanged;

        unsubscribers.ForEach(d => d.Invoke());

        if (keyboard is not null)
        {
            await keyboard.DisposeAsync();
        }
    }
}
