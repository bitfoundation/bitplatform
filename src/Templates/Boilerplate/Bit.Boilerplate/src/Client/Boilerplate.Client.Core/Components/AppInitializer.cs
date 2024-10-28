//+:cnd:noEmit
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
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif
    [AutoInject] private SnackBarService snackBarService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private NavigationManager navigationManager = default!;

    protected async override Task OnInitAsync()
    {
        AuthenticationManager.AuthenticationStateChanged += AuthenticationStateChanged;

        AuthenticationStateChanged(AuthenticationManager.GetAuthenticationStateAsync());

        if (AppPlatform.IsBlazorHybrid)
        {
            if (CultureInfoManager.MultilingualEnabled)
            {
                cultureInfoManager.SetCurrentCulture(new Uri(navigationManager.Uri).GetCulture() ??  // 1- Culture query string OR Route data request culture
                                                     await storageService.GetItem("Culture") ?? // 2- User settings
                                                     CultureInfo.CurrentUICulture.Name); // 3- OS settings
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
            //#if (signalr == true)
            if (InPrerenderSession is false)
            {
                await ConnectSignalR();
            }
            //#endif

            //#if (notification == true)
            if (InPrerenderSession is false)
            {
                await pushNotificationService.RegisterDevice(CurrentCancellationToken);
            }
            //#endif
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }

    //#if (signalr == true)
    private async Task ConnectSignalR()
    {
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }

        var access_token = await AuthTokenProvider.GetAccessToken();

        hubConnection = new HubConnectionBuilder()
            .WithAutomaticReconnect()
            .WithUrl($"{Configuration.GetServerAddress()}/app-hub?access_token={access_token}", options =>
            {
                options.Transports = Microsoft.AspNetCore.Http.Connections.HttpTransportType.WebSockets;
                // Avoid enabling long polling or Server-Sent Events. Focus on resolving the issue with WebSockets instead.
                // WebSockets should be enabled on services like IIS or Cloudflare CDN, offering significantly better performance.
                options.HttpMessageHandlerFactory = signalrHttpMessageHandler =>
                {
                    return serviceProvider.GetRequiredService<Func<HttpMessageHandler, HttpMessageHandler>>()
                        .Invoke(signalrHttpMessageHandler);
                };
            })
            .Build();

        hubConnection.On<string>("DisplayMessage", async (message) =>
        {
            snackBarService.Show(message, "");

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

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        AuthenticationManager.AuthenticationStateChanged -= AuthenticationStateChanged;

        //#if (signalr == true)
        if (hubConnection is not null)
        {
            await hubConnection.DisposeAsync();
        }
        //#endif

        await base.DisposeAsync(disposing);
    }
}
