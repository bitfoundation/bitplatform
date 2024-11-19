//+:cnd:noEmit
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Http.Connections;
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
    private HubConnection? hubConnection;
    [AutoInject] private Notification notification = default!;
    //#endif
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
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

    protected override async Task OnInitAsync()
    {
        if (AppPlatform.IsBlazorHybrid)
        {
            if (CultureInfoManager.MultilingualEnabled)
            {
                cultureInfoManager.SetCurrentCulture(new Uri(NavigationManager.Uri).GetCulture() ??  // 1- Culture query string OR Route data request culture
                                                     await storageService.GetItem("Culture") ?? // 2- User settings
                                                     CultureInfo.CurrentUICulture.Name); // 3- OS settings
            }

            await SetupBodyClasses();
        }

        if (InPrerenderSession is false)
        {
            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            AuthenticationManager.AuthenticationStateChanged += AuthenticationStateChanged;

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

            AuthenticationStateChanged(AuthenticationManager.GetAuthenticationStateAsync());
        }

        await base.OnInitAsync();
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        TelemetryContext.PageUrl = e.Location;
        navigatorLogger.LogInformation("Navigation's location changed to {Location}", e.Location);
    }

    private SemaphoreSlim semaphore = new(1, 1);

    /// <summary>
    /// This code manages the association of a user with sensitive services, such as SignalR, push notifications, App Insights, and others, 
    /// ensuring the user is correctly set or cleared as needed.
    /// </summary>
    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            await semaphore.WaitAsync(CurrentCancellationToken);
            // About Semaphore: The following code may take significant time to execute.
            // During this period, the authentication state could change. For instance, the app might start with the user authenticated, but they could sign out while this method is running.
            // To handle such scenarios, we must ensure the code below runs in a safe and sequential manner, preventing parallel execution.
            // Without this safeguard, SignalR or push notifications might incorrectly associate the device with a user who has already signed out.

            var user = (await task).User;
            var isAuthenticated = user.IsAuthenticated();
            TelemetryContext.UserId = isAuthenticated ? user.GetUserId() : null;
            TelemetryContext.UserSessionId = isAuthenticated ? user.GetSessionId() : null;

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
                authLogger.LogInformation("Authentication state changed.");
            }

            //#if (notification == true)
            await pushNotificationService.RegisterDevice(CurrentCancellationToken);
            //#endif

            //#if (signalR == true)
            await ConnectSignalR();
            //#endif
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
        finally
        {
            semaphore.Release();
        }
    }

    //#if (signalR == true)
    private async Task ConnectSignalR()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        hubConnection = new HubConnectionBuilder()
            .WithAutomaticReconnect(new SignalRInfinitiesRetryPolicy())
            .WithUrl(new Uri(AbsoluteServerAddress, "app-hub"), options =>
            {
                options.SkipNegotiation = true;
                options.Transports = HttpTransportType.WebSockets;
                // Avoid enabling long polling or Server-Sent Events. Focus on resolving the issue with WebSockets instead.
                // WebSockets should be enabled on services like IIS or Cloudflare CDN, offering significantly better performance.
                options.AccessTokenProvider = async () =>
                {
                    var accessToken = await AuthTokenProvider.GetAccessToken();

                    if (string.IsNullOrEmpty(accessToken) is false &&
                        AuthTokenProvider.ParseAccessToken(accessToken, validateExpiry: true).IsAuthenticated() is false)
                    {
                        return await AuthenticationManager.RefreshToken(requestedBy: nameof(HubConnectionBuilder));
                    }

                    return accessToken;
                };
            })
            .Build();

        hubConnection.On<string>(SignalREvents.SHOW_MESSAGE, async (message) =>
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
        });

        hubConnection.On<string>(SignalREvents.PUBLISH_MESSAGE, async (message) =>
        {
            logger.LogInformation("Message {Message} received from server.", message);
            PubSubService.Publish(message);
        });

        try
        {
            hubConnection.Closed += HubConnectionStateChange;
            hubConnection.Reconnected += HubConnectionConnected;
            hubConnection.Reconnecting += HubConnectionStateChange;

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

    private async Task SetupBodyClasses()
    {
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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        AuthenticationManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        //#if (signalR == true)
        if (hubConnection is not null)
        {
            hubConnection.Closed -= HubConnectionStateChange;
            hubConnection.Reconnected -= HubConnectionConnected;
            hubConnection.Reconnecting -= HubConnectionStateChange;
            await hubConnection.DisposeAsync();
        }
        //#endif

        await base.DisposeAsync(disposing);
    }
}
