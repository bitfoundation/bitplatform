//+:cnd:noEmit
using System.Reflection;
using Boilerplate.Shared.Controllers.Identity;
using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private static readonly BitModalParameters ModalParameters = new() { Classes = new() { Root = "modal" } };


    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;


    [AutoInject] private Keyboard keyboard = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private BitExtraServices bitExtraServices = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService = default!;


    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    private bool? isOnline;
    private BitDir? currentDir;
    private bool? isIdentityPage;
    private UserDto? currentUser;
    private AppThemeType? currentTheme;
    private RouteData? currentRouteData;
    private List<Action> unsubscribers = [];
    private CancellationTokenSource getCurrentUserCts = new();


    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();

        try
        {
            var inPrerenderSession = RendererInfo.IsInteractive is false;
            // During pre-rendering, if any API calls are made, the `isOnline` value will be set 
            // using PubSub's `ClientPubSubMessages.IS_ONLINE_CHANGED`, depending on the success 
            // or failure of the API call. However, if a pre-rendered page has no HTTP API call 
            // dependencies, its value remains null. 
            // Even though Server.Web and Server.Api may be deployed on different servers, 
            // we can still assume that if the client is displaying a pre-rendered result, it is online.
            isOnline = await prerenderStateService.GetValue<bool?>(nameof(isOnline), async () => isOnline ?? inPrerenderSession is true ? true : null);

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            await keyboard.Add(ButilKeyCodes.KeyX, OpenDiagnosticModal, ButilModifiers.Ctrl | ButilModifiers.Shift);
        }
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
            pubSubService.Publish(ClientPubSubMessages.CLOSE_NAV_PANEL);
            return;
        }

        isIdentityPage = false;
    }

    private void OpenDiagnosticModal()
    {
        pubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
    }


    public async ValueTask DisposeAsync()
    {
        if (getCurrentUserCts is not null)
        {
            await getCurrentUserCts.CancelAsync();
            getCurrentUserCts.Dispose();
        }

        authManager.AuthenticationStateChanged -= AuthManager_AuthenticationStateChanged;

        unsubscribers.ForEach(us => us.Invoke());

        if (keyboard is not null)
        {
            await keyboard.DisposeAsync();
        }
    }
}
