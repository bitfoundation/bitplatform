//+:cnd:noEmit
using Velopack;
using Microsoft.Web.WebView2.Core;
using Boilerplate.Client.Core.Components;
using Boilerplate.Client.Windows.Services;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace Boilerplate.Client.Windows;

public partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        //#if (appCenter == true)
        string? appCenterSecret = null;
        if (appCenterSecret is not null)
        {
            Microsoft.AppCenter.AppCenter.Start(appCenterSecret, typeof(Microsoft.AppCenter.Crashes.Crashes), typeof(Microsoft.AppCenter.Analytics.Analytics));
        }
        //#endif

        Application.ThreadException += (_, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject);

        ApplicationConfiguration.Initialize();

        AppPlatform.IsBlazorHybrid = true;
        ITelemetryContext.Current = new WindowsTelemetryContext();

        //#if (framework == 'net9.0')
        Application.SetColorMode(SystemColorMode.System);
        //#endif

        var services = new ServiceCollection();
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Windows");
        var configuration = configurationBuilder.Build();
        services.AddClientWindowsProjectServices(configuration);
        Services = services.BuildServiceProvider();

        if (CultureInfoManager.MultilingualEnabled)
        {
            Services.GetRequiredService<CultureInfoManager>().SetCurrentCulture(
                Application.UserAppDataRegistry.GetValue("Culture") as string ?? // 1- User settings
                CultureInfo.CurrentUICulture.Name); // 2- OS Settings
        }
        Services.GetRequiredService<PubSubService>().Subscribe(ClientPubSubMessages.CULTURE_CHANGED, async culture =>
        {
            Application.Restart();
        });

        // https://github.com/velopack/velopack
        VelopackApp.Build().Run();
        _ = Task.Run(async () =>
        {
            try
            {
                var windowsUpdateSettings = Services.GetRequiredService<ClientWindowsSettings>().WindowsUpdate;
                if (string.IsNullOrEmpty(windowsUpdateSettings?.FilesUrl))
                {
                    return;
                }
                var updateManager = new UpdateManager(windowsUpdateSettings.FilesUrl);
                var updateInfo = await updateManager.CheckForUpdatesAsync();
                if (updateInfo is not null)
                {
                    await updateManager.DownloadUpdatesAsync(updateInfo);
                    if (windowsUpdateSettings.AutoReload)
                    {
                        updateManager.ApplyUpdatesAndRestart(updateInfo, args);
                    }
                }
            }
            catch (Exception exp)
            {
                Services.GetRequiredService<IExceptionHandler>().Handle(exp);
            }
        });

        var form = new Form()
        {
            Text = "Boilerplate",
            WindowState = FormWindowState.Maximized,
            BackColor = ColorTranslator.FromHtml("#0D2960"),
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        };

        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--unsafely-treat-insecure-origin-as-secure=https://0.0.0.1 --enable-notifications");

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            Services = Services,
            HostPage = @"wwwroot\index.html",
            BackColor = ColorTranslator.FromHtml("#0D2960")
        };

        blazorWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml("#0D2960");

        //#if (appInsights == true)
        blazorWebView.RootComponents.Add(new RootComponent("head::after", typeof(BlazorApplicationInsights.ApplicationInsightsInit), null));
        //#endif

        blazorWebView.RootComponents.Add(new RootComponent("#app-container", typeof(Routes), null));

        blazorWebView.BlazorWebViewInitialized += delegate
        {
            blazorWebView.WebView.CoreWebView2.PermissionRequested += async (sender, args) =>
            {
                args.Handled = true;
                args.State = CoreWebView2PermissionState.Allow;
            };
            var settings = blazorWebView.WebView.CoreWebView2.Settings;
            if (AppEnvironment.IsDev() is false)
            {
                settings.IsZoomControlEnabled = false;
                settings.AreBrowserAcceleratorKeysEnabled = false;
            }
            blazorWebView.WebView.NavigationCompleted += async delegate
            {
                await blazorWebView.WebView.ExecuteScriptAsync("Blazor.start()");
            };
        };

        form.Controls.Add(blazorWebView);

        Application.Run(form);
    }

    private static void LogException(object? error)
    {
        var errorMessage = error?.ToString() ?? "Unknown error";
        if (Services is not null && error is Exception exp)
        {
            Services.GetRequiredService<IExceptionHandler>().Handle(exp);
        }
        else
        {
            Clipboard.SetText(errorMessage);
            System.Windows.Forms.MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static IServiceProvider? Services { get; private set; }
}
