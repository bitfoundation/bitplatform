//+:cnd:noEmit
using System.Web;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
//#endif
//#if (appInsights == true)
using BlazorApplicationInsights.Interfaces;
//#endif
using Boilerplate.Client.Core.Infrastructure.Services.DiagnosticLog;

namespace Boilerplate.Client.Core.Components;

/// <summary>
/// Manages the initialization and coordination of core services and settings within the client application.
/// This includes authentication state handling, telemetry setup, culture configuration, and optional
/// services such as SignalR connections, push notifications, and application insights.
/// </summary>
public partial class AppClientCoordinator : AppComponentBase
{
    //#if (signalR == true)
    [AutoInject] private Notification notification = default!;
    [AutoInject] private ThemeService themeService = default!;
    [AutoInject] private HubConnection hubConnection = default!;
    [AutoInject] private SignInModalService signInModalService = default!;
    //#endif
    [AutoInject] private CultureService cultureService = default!;
    //#if (appInsights == true)
    [AutoInject] private IApplicationInsights appInsights = default!;
    //#endif
    [AutoInject] private UserAgent userAgent = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private ILogger<AuthManager> authLogger = default!;
    [AutoInject] private ILogger<Navigator> navigatorLogger = default!;
    [AutoInject] private ILogger<AppClientCoordinator> logger = default!;
    [AutoInject] private BitAccentColorService accentColorService = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif
    //#if (brouter == true)
    [AutoInject] private IBrouter brouter = default!;
    //#endif

    private List<Action> unsubscribes = [];

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        if (AppPlatform.IsBlazorHybrid)
        {
            await ConfigureUISetup();
        }

        if (InPrerenderSession is false)
        {
            unsubscribes.Add(PubSubService.Subscribe(ClientAppMessages.NAVIGATE_TO, async (uri) =>
            {
                var (url, replace, forceLoad) = ParseNavigateToOptions(uri?.ToString()!);

                if (Uri.IsAppRelativeUrl(url, requireLeadingSlash: false) is false)
                    return;

                NavigationManager.NavigateTo(url, forceLoad, replace);
            }));
            //#if (signalR == true)
            unsubscribes.Add(PubSubService.Subscribe(SharedAppMessages.EXCEPTION_THROWN, async (payload) =>
            {
                if (payload is null) return;

                var appProblemDetails = payload is JsonElement jsonDocument
                    ? jsonDocument.Deserialize(JsonSerializerOptions.GetTypeInfo<AppProblemDetails>())! /* Message gets published from server through SignalR */
                    : (AppProblemDetails)payload;

                ExceptionHandler.Handle(appProblemDetails, displayKind: ExceptionDisplayKind.NonInterrupting);
            }));
            //#endif

            if (AppPlatform.IsBlazorHybrid is false)
            {
                var userAgentData = await userAgent.Extract();
                TelemetryContext.Platform = string.Join(' ', [userAgentData.Manufacturer, userAgentData.OsName, userAgentData.Name, "browser"]);
                await cultureService.PersistCurrentCulture();
            }
            await TimeZoneService.ApplyPreferredTimeZone();
            TelemetryContext.PageUrl = new Uri(NavigationManager.Uri).GetUrlWithMaskedQueryValues();

            //#if (appInsights == true)
            _ = appInsights.AddTelemetryInitializer(new()
            {
                Data = new()
                {
                    ["ai.application.ver"] = TelemetryContext.AppVersion,
                    ["ai.session.id"] = TelemetryContext.AppSessionId,
                    ["ai.device.locale"] = CultureInfo.CurrentUICulture.Name
                }
            });

            // Nothing to apply at startup: UpdateCfg reads the decision itself. This only says it changed, since
            // consent is withdrawable. The empty config asks for nothing - the switches are filled in there.
            unsubscribes.Add(PubSubService.Subscribe(ClientAppMessages.CONSENT_CHANGED, async _ => await appInsights.UpdateCfg(new())));
            //#endif

            await accentColorService.InitializeAsync();

            NavigationManager.LocationChanged += NavigationManager_LocationChanged;
            AuthManager.AuthenticationStateChanged += AuthenticationStateChanged;
            //#if (signalR == true)
            SubscribeToSignalRSharedAppMessages();
            //#endif
            await PropagateAuthState(firstRun: true, AuthenticationStateTask);
        }
    }

    /// <summary>
    /// Splits a NAVIGATE_TO payload into the url the app should go to plus the two control flags, which the server
    /// (and the service worker) pass as ordinary query parameters. Only the exact <c>replace</c> and <c>forceLoad</c>
    /// keys are consumed; every other parameter, including one that merely contains those words, is left alone.
    /// </summary>
    private static (string Url, bool Replace, bool ForceLoad) ParseNavigateToOptions(string uriValue)
    {
        var queryStartIndex = uriValue.IndexOf('?', StringComparison.Ordinal);

        if (queryStartIndex is -1)
            return (uriValue, false, false);

        var parsedQuery = HttpUtility.ParseQueryString(uriValue[(queryStartIndex + 1)..]);

        bool IsTrue(string key) => string.Equals(parsedQuery[key], "true", StringComparison.OrdinalIgnoreCase);

        var replace = IsTrue("replace");
        var forceLoad = IsTrue("forceLoad");

        parsedQuery.Remove("replace");
        parsedQuery.Remove("forceLoad");

        var remainingQuery = parsedQuery.ToString();

        return ($"{uriValue[..queryStartIndex]}{(string.IsNullOrWhiteSpace(remainingQuery) ? "" : $"?{remainingQuery}")}", replace, forceLoad);
    }

    private void NavigationManager_LocationChanged(object? sender, LocationChangedEventArgs e)
    {
        TelemetryContext.PageUrl = new Uri(e.Location).GetUrlWithMaskedQueryValues();
        navigatorLogger.LogInformation("Navigator's location changed to {Location}", TelemetryContext.PageUrl);
    }

    private ClaimsPrincipal? lastPropagatedUser;
    /// <summary>
    /// This code manages the association of a user with sensitive services, such as SignalR, push notifications, App Insights, and others,
    /// ensuring the user is correctly set or cleared as needed.
    /// </summary>
    public async Task PropagateAuthState(bool firstRun, Task<AuthenticationState> task)
    {
        try
        {
            var user = (await task).User;
            var isAuthenticated = user.IsAuthenticated();
            var userId = isAuthenticated ? user.GetUserId() : (Guid?)null;

            if (user.IsTheSame(lastPropagatedUser))
                return;

            await Abort(); // Cancels ongoing user id propagation, because the new authentication state is available.

            //#if (brouter == true)
            // KeepAlive routes are hidden rather than disposed, so a retained page would otherwise hand the next
            // principal the previous one's search text and grid filters.
            brouter.ClearKeepAlive();
            //#endif

            TelemetryContext.UserId = userId;
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
                authLogger.LogInformation("Propagating {AuthStateType} {AuthState} authentication state.", firstRun ? "Initial" : "Updated", user.IsAuthenticated() ? "Authenticated" : "Anonymous");
            }

            //#if (signalR == true)
            await EnsureSignalRStarted();
            //#endif

            //#if (notification == true)
            await pushNotificationService.Subscribe(CurrentCancellationToken);
            //#endif

            if (isAuthenticated)
            {
                await UpdateUserSession();
            }

            lastPropagatedUser = user;
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    private void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _ = PropagateAuthState(firstRun: false, task);
    }

    //#if (signalR == true)
    private void SubscribeToSignalRSharedAppMessages()
    {
        hubConnection.Remove(SharedAppMessages.PUBLISH_MESSAGE);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.PUBLISH_MESSAGE, async (string message, object? payload) =>
        {
            logger.LogInformation("SignalR Message {Message} received from server to publish.", message);
            PubSubService.Publish(message, payload);
            return true;
        }));
        // Generally, you're expected to use ShardAppMessages.PUBLISH_MESSAGE at server side to publish messages to clients through SignalR using Server.Api/Extensions/IClientProxyExtensions.cs's Publish method.
        // However, in some scenarios, you might want client to return a value to server, or simply return `true` confirming that the message is received and processed successfully,
        // so the server can use `InvokeAsync<bool>` instead of `SendAsync` when sending the message.
        // That's why in the following code block, we subscribe to **some** SharedAppMessages directly using HubConnection:

        hubConnection.Remove(SharedAppMessages.SHOW_MESSAGE);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.SHOW_MESSAGE, async (string message, Dictionary<string, string?>? data) =>
        {
            logger.LogInformation("SignalR Message {Message} received from server to show.", message);
            if (await notification.IsNotificationAvailable())
            {
                // Show local notification
                // Note that this code has nothing to do with push notification.
                await notification.Show("Boilerplate SignalR", new()
                {
                    Icon = "/images/icons/bit-icon-512.png",
                    Body = message,
                    Data = data
                });
            }
            else
            {
                SnackBarService.Show("Boilerplate", message);

                return data is null;  // Snack bar service does not support payload data. It would be a good idea to return false to the server so server knows that the message was not shown properly.
            }

            return true; // Message gets shown successfully. You CAN (not implemented yet) use this in server side in order to not to send push notifications for messages that are already shown in the client side.

            // The following code block is not required for Bit.BlazorUI components to perform UI changes. However, it may be necessary in other scenarios.
            /*await InvokeAsync(async () =>
            {
                StateHasChanged();
            });*/

            // You can also leverage IPubSubService to notify other components in the application.
        }));

        hubConnection.Remove(SharedAppMessages.NAVIGATE_TO);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.NAVIGATE_TO, async (string url) =>
        {
            if (Uri.IsAppRelativeUrl(url) is false)
                return false;

            await InvokeAsync(async () =>
            {
                NavigationManager.NavigateTo(url);
            });
            return true;
        }));

        hubConnection.Remove(SharedAppMessages.CHANGE_CULTURE);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.CHANGE_CULTURE, async (int cultureLcid) =>
        {
            await InvokeAsync(async () =>
            {
                var culture = CultureInfo.GetCultureInfo(cultureLcid);
                await cultureService.ChangeCulture(culture.Name);
            });
            return true;
        }));

        hubConnection.Remove(SharedAppMessages.CHANGE_THEME);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.CHANGE_THEME, async (string requestedTheme) =>
        {
            bool themeChanged = false;

            await InvokeAsync(async () =>
            {
                var currentTheme = (await themeService.GetCurrentTheme()).ToString();

                if (string.Equals(currentTheme, requestedTheme, StringComparison.InvariantCultureIgnoreCase) is false)
                {
                    await themeService.ToggleTheme();
                    themeChanged = true;
                }
            });

            return themeChanged;
        }));

        hubConnection.Remove(SharedAppMessages.CLEAR_APP_FILES);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.CLEAR_APP_FILES, async () =>
        {
            PubSubService.Publish(ClientAppMessages.CLEAR_APP_FILES);

            return true;
        }));

        hubConnection.Remove(SharedAppMessages.UPLOAD_LAST_ERROR);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.UPLOAD_LAST_ERROR, async () =>
        {
            return DiagnosticLogger.Store.LastOrDefault(l => l.Level is LogLevel.Error or LogLevel.Critical);
        }));

        hubConnection.Remove(SharedAppMessages.SHOW_SIGN_IN_MODAL);
        signalROnDisposables.Add(hubConnection.On(SharedAppMessages.SHOW_SIGN_IN_MODAL, async () =>
        {
            await signInModalService.SignIn();
            return await StorageService.GetItem("access_token");
        }));

        hubConnection.Closed += HubConnectionStateChange;
        hubConnection.Reconnected += HubConnectionConnected;
        hubConnection.Reconnecting += HubConnectionStateChange;
    }

    private async Task EnsureSignalRStarted()
    {
        try
        {
            if (hubConnection.State is not HubConnectionState.Connected)
            {
                await hubConnection.StartAsync(CurrentCancellationToken);
                await HubConnectionConnected(null);
            }
            else
            {
                await hubConnection.InvokeAsync(SharedAppMessages.ChangeAuthenticationState, await AuthTokenProvider.GetAccessToken(), CurrentCancellationToken);
            }
        }
        catch (Exception exp)
        {
            await HubConnectionStateChange(exp);
        }
    }

    private async Task HubConnectionConnected(string? _)
    {
        PubSubService.Publish(ClientAppMessages.IS_ONLINE_CHANGED, true);
        logger.LogInformation("SignalR connection established.");
    }

    private async Task HubConnectionStateChange(Exception? exception)
    {
        PubSubService.Publish(ClientAppMessages.IS_ONLINE_CHANGED, exception is null && hubConnection!.State is HubConnectionState.Connected);

        if (exception is null)
        {
            logger.LogInformation("SignalR state changed to {State}", hubConnection!.State);
        }
        else if (exception is OperationCanceledException)
        {
            logger.LogInformation("SignalR connection attempt cancelled.");
        }
        else
        {
            logger.LogWarning(exception, "SignalR connection lost.");

            if (exception is HubException)
            {
                if (exception.Message.EndsWith(nameof(AppStrings.UnauthorizedException)))
                {
                    await AuthManager.RefreshToken(requestedBy: nameof(HubException));
                }
                else if (exception.Message.EndsWith(nameof(AppStrings.ForceUpdateTitle)))
                {
                    PubSubService.Publish(ClientAppMessages.FORCE_UPDATE);
                }
            }
        }
    }

    //#endif

    private async Task UpdateUserSession()
    {
        await userController.UpdateSession(new()
        {
            AppVersion = TelemetryContext.AppVersion,
            DeviceInfo = TelemetryContext.Platform,
            CultureName = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.Name,
            PlatformType = AppPlatform.Type
        }, CurrentCancellationToken);
    }

    private async Task ConfigureUISetup()
    {
        if (CultureInfoManager.InvariantGlobalization is false)
        {
            CultureInfoManager.SetCurrentCulture(new Uri(NavigationManager.Uri).GetCulture() ??  // 1- Culture query string OR Route data request culture
                                                 await StorageService.GetItem("Culture") ?? // 2- User settings
                                                 CultureInfo.CurrentUICulture.Name); // 3- OS settings
        }
    }

    private List<IDisposable> signalROnDisposables = [];
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        unsubscribes.ForEach(unsubscribe => unsubscribe());
        unsubscribes = [];

        NavigationManager.LocationChanged -= NavigationManager_LocationChanged;
        AuthManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        //#if (signalR == true)
        hubConnection.Closed -= HubConnectionStateChange;
        hubConnection.Reconnected -= HubConnectionConnected;
        hubConnection.Reconnecting -= HubConnectionStateChange;
        signalROnDisposables.ForEach(d => d.Dispose());
        signalROnDisposables = [];
        //#endif

        await base.DisposeAsync(disposing);
    }
}
