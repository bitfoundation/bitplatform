//+:cnd:noEmit
//#if (multitenant == true)
using Boilerplate.Shared.Features.Tenants;
using Boilerplate.Shared.Features.Tenants.Dtos;
//#endif

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private static readonly BitModalParameters ModalParameters = new() { Classes = new() { Root = "modal" } };
    private static readonly BitProModalParameters ProModalParameters = new() { Classes = new() { Root = "modal" } };


    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;


    [AutoInject] private Document document = default!;
    [AutoInject] private Keyboard keyboard = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private IUserController userController = default!;
    //#if (multitenant == true)
    [AutoInject] private ITenantController tenantController = default!;
    //#endif
    [AutoInject] private BitExtraServices bitExtraServices = default!;
    [AutoInject] private ClientExceptionHandlerBase exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private JsonSerializerOptions jsonSerializerOptions = default!;


    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    [PersistentState] public bool? IsOnline { get; set; }

    private BitDir? currentDir;
    private bool? isIdentityPage;
    private UserDto? currentUser;
    //#if (multitenant == true)
    private TenantDto? currentTenant;
    //#endif
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
            // using PubSub's `ClientAppMessages.IS_ONLINE_CHANGED`, depending on the success 
            // or failure of the API call. However, if a pre-rendered page has no HTTP API call 
            // dependencies, its value remains null. 
            // Even though Server.Web and Server.Api may be deployed on different servers, 
            // we can still assume that if the client is displaying a pre-rendered result, it is online.
            if (inPrerenderSession)
            {
                IsOnline ??= true;
            }

            authManager.AuthenticationStateChanged += AuthManager_AuthenticationStateChanged;

            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.CULTURE_CHANGED, async _ =>
            {
                SetCurrentDir();
                await ApplyCultureToDocument();
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.THEME_CHANGED, async payload =>
            {
                if (payload is null) return;
                currentTheme = (AppThemeType)payload;
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.ROUTE_DATA_UPDATED, async payload =>
            {
                currentRouteData = (RouteData?)payload;
                SetRouteData();
                StateHasChanged();
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.IS_ONLINE_CHANGED, async payload =>
            {
                if (IsOnline == (bool?)payload)
                    return;
                telemetryContext.IsOnline = IsOnline = (bool?)payload;
                await InvokeAsync(StateHasChanged);
            }));

            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.PROFILE_UPDATED, async payload =>
            {
                if (payload is null) return;

                currentUser = payload is JsonElement jsonDocument
                    ? jsonDocument.Deserialize(jsonSerializerOptions.GetTypeInfo<UserDto>())! /* Message gets published from server through SignalR */
                    : (UserDto)payload;

                await InvokeAsync(StateHasChanged);
            }));

            //#if (multitenant == true)
            unsubscribers.Add(pubSubService.Subscribe(ClientAppMessages.CURRENT_TENANT_CHANGED, async payload =>
            {
                // Published by the pages/menus that change the current tenant (See ManageMyTenantsPage). Switching, signing in/out and
                // leaving a tenant already update this through the authentication-state change, so this mainly covers renaming the current tenant.
                currentTenant = (TenantDto?)payload;

                await InvokeAsync(StateHasChanged);
            }));
            //#endif

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

            // Stamps the booted culture onto <html> for the static hosts, whose index.html carries no lang/dir;
            // a no-op re-stamp on Server.Web, where App.razor already rendered the same values.
            await ApplyCultureToDocument();
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

    private ClaimsPrincipal? lastAuthUser;
    private async Task SetCurrentUser(Task<AuthenticationState> task)
    {
        var authUser = (await task).User;

        await SetNavPanelItems(authUser);

        using var currentCts = getCurrentUserCts;
        getCurrentUserCts = new();
        await currentCts.TryCancel();

        if (authUser.IsAuthenticated() is false)
        {
            currentUser = null;
            //#if (multitenant == true)
            currentTenant = null;
            //#endif
        }
        else
        {
            if (currentUser is null || authUser.IsTheSame(lastAuthUser) is false)
            {
                currentUser = await userController.GetCurrentUser(getCurrentUserCts.Token);
            }

            //#if (multitenant == true)
            await SetCurrentTenantIfNeeded(authUser.GetTenantId(), getCurrentUserCts.Token);
            //#endif
        }

        lastAuthUser = authUser;
    }

    //#if (multitenant == true)
    /// <summary>
    /// Resolves the tenant the user is currently signed into (shown next to the app version in AppShell).
    /// Switching, signing in/out and leaving a tenant all change the tenant claim, so re-resolving here keeps the display in sync.
    /// </summary>
    private async Task SetCurrentTenantIfNeeded(Guid? tenantId, CancellationToken cancellationToken)
    {
        if (tenantId is null)
        {
            currentTenant = null;
            return;
        }

        if (currentTenant?.Id == tenantId) return; // Already showing this tenant (e.g. a page already published it).

        currentTenant = await tenantController.GetCurrentTenant(cancellationToken);
    }
    //#endif

    private void SetCurrentDir()
    {
        currentDir = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? BitDir.Rtl : null;
    }

    /// <summary>
    /// Stamps the current culture's name and directionality onto the &lt;html&gt; element, so assistive technologies,
    /// css <c>:lang()</c> rules and the document's layout direction follow the rendered language.
    /// </summary>
    private async Task ApplyCultureToDocument()
    {
        if (CultureInfoManager.InvariantGlobalization || RendererInfo.IsInteractive is false) return;

        var culture = CultureInfo.CurrentUICulture;
        await document.SetLang(culture.Name);
        await document.SetDir(culture.TextInfo.IsRightToLeft ? DocumentDir.Rtl : DocumentDir.Ltr);
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
            pubSubService.Publish(ClientAppMessages.CLOSE_NAV_PANEL);
            return;
        }

        isIdentityPage = false;
    }

    private void OpenDiagnosticModal()
    {
        pubSubService.Publish(ClientAppMessages.SHOW_DIAGNOSTIC_MODAL);
    }


    public async ValueTask DisposeAsync()
    {
        if (getCurrentUserCts is not null)
        {
            await getCurrentUserCts.TryCancel();
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
