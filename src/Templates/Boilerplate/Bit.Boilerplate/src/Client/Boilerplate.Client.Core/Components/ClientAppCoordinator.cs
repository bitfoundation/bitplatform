//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
//#endif
//#if (appInsights == true)
using BlazorApplicationInsights.Interfaces;
//#endif
using Microsoft.AspNetCore.Components.Routing;

namespace Boilerplate.Client.Core.Components;

/// <summary>
/// Manages the initialization and coordination of core services and settings within the client application.
/// This includes authentication state handling, telemetry setup, culture configuration, and optional
/// services such as SignalR connections, push notifications, and application insights.
/// </summary>
public partial class ClientAppCoordinator : AppComponentBase
{
    //#if (signalR == true)
    [AutoInject] private Notification notification = default!;
    [AutoInject] private HubConnection hubConnection = default!;
    //#endif
    //#if (appInsights == true)
    [AutoInject] private IApplicationInsights appInsights = default!;
    //#endif
    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private ILogger<Navigator> navigatorLogger = default!;
    [AutoInject] private ILogger<ClientAppCoordinator> logger = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private ILogger<AuthenticationManager> authLogger = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif

    protected override async Task OnInitAsync()
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            await ConfigureUISetup();
        }

        if (InPrerenderSession is false)
        {
            TelemetryContext.UserAgent = await navigator.GetUserAgent();
            TelemetryContext.TimeZone = await jsRuntime.GetTimeZone();
            TelemetryContext.Culture = CultureInfo.CurrentCulture.Name;
            TelemetryContext.PageUrl = NavigationManager.Uri;
            if (AppPlatform.IsBlazorHybrid is false)
            {
                TelemetryContext.OS = await jsRuntime.GetBrowserPlatform();
            }

            //#if (appInsights == true)
            _ = appInsights.AddTelemetryInitializer(new()
            {
                Data = new()
                {
                    ["ai.application.ver"] = TelemetryContext.AppVersion,
                    ["ai.session.id"] = TelemetryContext.AppSessionId,
                    ["ai.device.locale"] = TelemetryContext.Culture
                }
            });
            //#endif

            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            AuthenticationManager.AuthenticationStateChanged += AuthenticationStateChanged;
            //#if (signalR == true)
            SubscribeToSignalREventsMessages();
            //#endif
            await PropagateUserId(firstRun: true, AuthenticationManager.GetAuthenticationStateAsync());
        }

        await base.OnInitAsync();
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        TelemetryContext.PageUrl = e.Location;
        navigatorLogger.LogInformation("Navigation's location changed to {Location}", e.Location);
    }

    /// <summary>
    /// This code manages the association of a user with sensitive services, such as SignalR, push notifications, App Insights, and others, 
    /// ensuring the user is correctly set or cleared as needed.
    /// </summary>
    public async Task PropagateUserId(bool firstRun, Task<AuthenticationState> task)
    {
        try
        {
            Abort(); // Cancels ongoing user id propagation, because the new authentication state is available.

            var user = (await task).User;
            var isAuthenticated = user.IsAuthenticated();
            TelemetryContext.UserId = isAuthenticated ? user.GetUserId() : null;
            TelemetryContext.UserSessionId = isAuthenticated ? user.GetSessionId() : null;

            // Typically, we use the logger directly without utilizing logger.BeginScope.
            // While many loggers provide specific methods to set userId and other context-related information,
            // we use this method to propagate the user ID and other telemetry contexts via Microsoft.Extensions.Logging's Scope feature.
            // PropagateUserId method is invoked both during app startup and when the authentication state changes.
            // Additionally, this is a convenient place to manage user-specific contexts for services like:
            // - App Insights: Set or clear the user ID for tracking purposes.
            // - Push Notifications: Update subscriptions to ensure user-specific notifications are routed to the correct devices.
            // - SignalR: Map connection IDs to a user's group of connections for message targeting.
            // By leveraging this method during authentication state changes, we streamline the propagation of user-specific contexts across these systems.

            //#if (appInsights == true)
            if (isAuthenticated)
            {
                _ = appInsights.SetAuthenticatedUserContext(user.GetUserId().ToString());
            }
            else
            {
                _ = appInsights.ClearAuthenticatedUserContext();
            }
            //#endif

            var data = TelemetryContext.ToDictionary();
            using var scope = authLogger.BeginScope(data);
            {
                authLogger.LogInformation($"Propagating {(firstRun ? "initial" : "changed")} authentication state.");
            }

            //#if (notification == true)
            await pushNotificationService.RegisterSubscription(CurrentCancellationToken);
            //#endif

            //#if (signalR == true)
            await StartSignalR();
            //#endif
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    private void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _ = PropagateUserId(firstRun: false, task);
    }

    //#if (signalR == true)
    private void SubscribeToSignalREventsMessages()
    {
        signalROnDisposables.Add(hubConnection.On<string>(SignalREvents.SHOW_MESSAGE, async (message) =>
        {
            if (await notification.IsNotificationAvailable())
            {
                // Show local notification
                // Note that this code has nothing to do with push notification.
                await notification.Show("Boilerplate", new() { Body = message });
            }
            else
            {
                SnackBarService.Show("Boilerplate", message);
            }

            // The following code block is not required for Bit.BlazorUI components to perform UI changes. However, it may be necessary in other scenarios.
            /*await InvokeAsync(async () =>
            {
                StateHasChanged();
            });*/

            // You can also leverage IPubSubService to notify other components in the application.
        }));

        signalROnDisposables.Add(hubConnection.On<string>(SignalREvents.PUBLISH_MESSAGE, async (message) =>
        {
            logger.LogInformation("Message {Message} received from server.", message);
            PubSubService.Publish(message);
        }));

        hubConnection.Closed += HubConnectionStateChange;
        hubConnection.Reconnected += HubConnectionConnected;
        hubConnection.Reconnecting += HubConnectionStateChange;
    }

    private async Task StartSignalR()
    {
        try
        {
            await hubConnection.StopAsync(CurrentCancellationToken);
            await hubConnection.StartAsync(CurrentCancellationToken);
            await HubConnectionConnected(null);
        }
        catch (Exception exp)
        {
            await HubConnectionStateChange(exp);
        }
    }

    private async Task HubConnectionConnected(string? _)
    {
        PubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, true);
        logger.LogInformation("SignalR connection established.");
    }

    private async Task HubConnectionStateChange(Exception? exception)
    {
        PubSubService.Publish(ClientPubSubMessages.IS_ONLINE_CHANGED, exception is null && hubConnection!.State is HubConnectionState.Connected);

        if (exception is null)
        {
            logger.LogInformation("SignalR state changed {State}", hubConnection!.State);
        }
        else
        {
            logger.LogWarning(exception, "SignalR connection lost.");

            if (exception is HubException && exception.Message.EndsWith(nameof(AppStrings.UnauthorizedException)))
            {
                await AuthenticationManager.RefreshToken(requestedBy: nameof(HubException));
            }
        }
    }

    //#endif

    private async Task ConfigureUISetup()
    {
        if (CultureInfoManager.MultilingualEnabled)
        {
            cultureInfoManager.SetCurrentCulture(new Uri(NavigationManager.Uri).GetCulture() ??  // 1- Culture query string OR Route data request culture
                                                 await storageService.GetItem("Culture") ?? // 2- User settings
                                                 CultureInfo.CurrentUICulture.Name); // 3- OS settings
        }

        var cssClasses = new List<string> { };

        if (AppPlatform.IsWindows)
        {
            cssClasses.Add("bit-windows");
        }
        else if (AppPlatform.IsMacOS)
        {
            cssClasses.Add("bit-macos");
        }
        else if (AppPlatform.IsIOS)
        {
            cssClasses.Add("bit-ios");
        }
        else if (AppPlatform.IsAndroid)
        {
            cssClasses.Add("bit-android");
        }

        var cssVariables = new Dictionary<string, string>
        {
        };

        await jsRuntime.ApplyBodyElementClasses(cssClasses, cssVariables);
    }

    private List<IDisposable> signalROnDisposables = [];
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        AuthenticationManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        //#if (signalR == true)
        hubConnection.Closed -= HubConnectionStateChange;
        hubConnection.Reconnected -= HubConnectionConnected;
        hubConnection.Reconnecting -= HubConnectionStateChange;
        signalROnDisposables.ForEach(d => d.Dispose());
        //#endif

        await base.DisposeAsync(disposing);
    }
}
