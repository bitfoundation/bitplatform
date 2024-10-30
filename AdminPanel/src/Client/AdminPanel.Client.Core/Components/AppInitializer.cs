using Microsoft.Extensions.Logging;

namespace AdminPanel.Client.Core.Components;

public partial class AppInitializer : AppComponentBase
{
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    [AutoInject] private Navigator navigator = default!;
    [AutoInject] private IJSRuntime jsRuntime = default!;
    [AutoInject] private Bit.Butil.Console console = default!;
    [AutoInject] private IStorageService storageService = default!;
    [AutoInject] private ILogger<AppInitializer> logger = default!;
    [AutoInject] private SnackBarService snackBarService = default!;
    [AutoInject] private AuthenticationManager authManager = default!;
    [AutoInject] private NavigationManager navigationManager = default!;
    [AutoInject] private CultureInfoManager cultureInfoManager = default!;
    [AutoInject] private IBitDeviceCoordinator bitDeviceCoordinator = default!;

    protected override async Task OnInitAsync()
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

            BrowserConsoleLoggerProvider.SetConsole(console);
        }

        await base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        TelemetryContext.UserAgent = await navigator.GetUserAgent();
        TelemetryContext.TimeZone = await jsRuntime.GetTimeZone();
        TelemetryContext.Culture = CultureInfo.CurrentCulture.Name;
        if (AppPlatform.IsBlazorHybrid is false)
        {
            TelemetryContext.OS = await jsRuntime.GetBrowserPlatform();
        }
    }

    private async void AuthenticationStateChanged(Task<AuthenticationState> task)
    {
        try
        {
            var user = (await task).User;
            TelemetryContext.UserId = user.IsAuthenticated() ? user.GetUserId() : null;
            TelemetryContext.UserSessionId = user.IsAuthenticated() ? user.GetSessionId() : null;


            if (InPrerenderSession is false)
            {
                await pushNotificationService.RegisterDevice(CurrentCancellationToken);
            }
        }
        catch (Exception exp)
        {
            ExceptionHandler.Handle(exp);
        }
    }


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


        await base.DisposeAsync(disposing);
    }
}
