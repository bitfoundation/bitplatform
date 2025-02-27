//+:cnd:noEmit
using System.Reflection;
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class MainLayout : IAsyncDisposable
{
    private BitDir? currentDir;
    private bool isNavPanelOpen;
    private bool? isIdentityPage;
    private bool isNavPanelToggled = true;
    private readonly BitModalParameters modalParameters = new() { Classes = new() { Root = "modal" } };

    /// <summary>
    /// <inheritdoc cref="Parameters.IsOnline"/>
    /// </summary>
    private bool? isOnline;

    private bool? isAuthenticated;
    private AppThemeType? currentTheme;
    private RouteData? currentRouteData;
    private List<Action> unsubscribers = [];
    private List<BitNavItem> navPanelAuthenticatedItems = [];
    private List<BitNavItem> navPanelUnAuthenticatedItems = [];


    [AutoInject] private Keyboard keyboard = default!;
    [AutoInject] private AuthManager authManager = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private PubSubService pubSubService = default!;
    [AutoInject] private BitExtraServices bitExtraServices = default!;
    [AutoInject] private IExceptionHandler exceptionHandler = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private IPrerenderStateService prerenderStateService = default!;


    [CascadingParameter] public Task<AuthenticationState> AuthenticationStateTask { get; set; } = default!;


    protected override async Task OnInitializedAsync()
    {
        try
        {
            InitializeNavPanelItems();

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

            isAuthenticated = (await AuthenticationStateTask).User.IsAuthenticated();

            SetCurrentDir();
            currentTheme = await themeService.GetCurrentTheme();

            await bitExtraServices.AddRootCssClasses();

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

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await keyboard.Add(ButilKeyCodes.KeyX, OpenDiagnosticModal, ButilModifiers.Ctrl | ButilModifiers.Shift);
        }

        await base.OnAfterRenderAsync(firstRender);
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
            isAuthenticated = (await task).User.IsAuthenticated();
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


    public async ValueTask DisposeAsync()
    {
        navigationManager.LocationChanged -= NavigationManager_LocationChanged;
        authManager.AuthenticationStateChanged -= AuthManager_AuthenticationStateChanged;

        unsubscribers.ForEach(d => d.Invoke());

        if (keyboard is not null)
        {
            await keyboard.DisposeAsync();
        }
    }
}
