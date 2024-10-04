//+:cnd:noEmit
using Microsoft.Extensions.Logging;
//#if (signalr == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif

namespace Boilerplate.Client.Core.Components;

public partial class AppInitializer : AppComponentBase
{
    //#if (signalr == true)
    private HubConnection? hubConnection;
    [AutoInject] private IServiceProvider serviceProvider = default!;
    //#endif
    [AutoInject] private MessageBoxService messageBoxService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private ILogger<AuthenticationManager> authLogger = default!;
    [AutoInject] private IPushNotificationService pushNotificationService = default!;

    protected async override Task OnInitAsync()
    {
        AuthenticationManager.AuthenticationStateChanged += AuthenticationStateChanged;

        AuthenticationStateChanged(AuthenticationManager.GetAuthenticationStateAsync());

        if (AppPlatform.IsBlazorHybrid)
        {
            if (CultureInfoManager.MultilingualEnabled)
            {
                cultureInfoManager.SetCurrentCulture(await storageService.GetItem("Culture") ?? // 1- User settings
                                                     CultureInfo.CurrentUICulture.Name); // 2- OS settings
            }

            await SetupBodyClasses();
        }

        await base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        if (AppPlatform.IsBlazorHybrid is false)
        {
            AppPlatform.OSDescription = await jsRuntime.GetBrowserPlatform();
        }
    }

    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            var user = (await AuthenticationStateTask).User;

            var (isUserAuthenticated, userId, userName, email, sessionId) = user.IsAuthenticated() ? (user.IsAuthenticated(), user.GetUserId().ToString(), user.GetUserName(), user.GetEmail(), user.GetSessionId()) : default;

            LogAuthenticationState(authLogger, isUserAuthenticated, userId, userName, email, sessionId);

            //#if (signalr == true)
            if (InPrerenderSession is false)
            {
                await ConnectSignalR();
            }
            //#endif

            if (InPrerenderSession is false)
            {
                await pushNotificationService.RegisterDeviceAsync(CurrentCancellationToken);
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Authentication State: {IsUserAuthenticated}, {UserId}, {UserName}, {Email}, {UserSessionId}")]
    private static partial void LogAuthenticationState(ILogger logger, bool isUserAuthenticated, string userId, string userName, string? email, Guid? userSessionId);

    //#if (signalr == true)
    private async Task ConnectSignalR()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        var access_token = await AuthTokenProvider.GetAccessTokenAsync();

        hubConnection = new HubConnectionBuilder()
            .WithUrl($"{Configuration.GetServerAddress()}/app-hub?access_token={access_token}", options =>
            {
                options.HttpMessageHandlerFactory = signalrHttpMessageHandler =>
                {
                    return serviceProvider.GetRequiredService<Func<HttpMessageHandler, HttpMessageHandler>>()
                        .Invoke(signalrHttpMessageHandler);
                };
            })
            .Build();

        hubConnection.On<string>("TwoFactorToken", async (token) =>
        {
            await messageBoxService.Show(Localizer[nameof(AppStrings.TwoFactorTokenPushText), token]);

            // The following code block is not required for Bit.BlazorUI components to perform UI changes. However, it may be necessary in other scenarios.
            /*await InvokeAsync(async () =>
            {
                StateHasChanged();
            });*/

            // You can also leverage IPubSubService to notify other components in the application.
        });

        await hubConnection.StartAsync(CurrentCancellationToken);
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

    //#if (signalr == true)
    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        await base.DisposeAsync(disposing);
    }
    //#endif
}
