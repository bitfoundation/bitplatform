using Velopack;
using Bit.BlazorUI.Demo.Client.Core;
using Bit.BlazorUI.Demo.Client.Windows.Configuration;
using Microsoft.Web.WebView2.Core;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace Bit.BlazorUI.Demo.Client.Windows;

public partial class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        Application.ThreadException += (_, e) => LogException(e.Exception);
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject);

        ApplicationConfiguration.Initialize();

        Application.SetColorMode(SystemColorMode.System);

        AppRenderMode.IsBlazorHybrid = true;
        var services = new ServiceCollection();
        ConfigurationBuilder configurationBuilder = new();
        configurationBuilder.AddClientConfigurations();
        var configuration = configurationBuilder.Build();
        services.AddTransient<IConfiguration>(sp => configuration);
        Uri.TryCreate(configuration.GetApiServerAddress(), UriKind.Absolute, out var apiServerAddress);
        services.AddTransient(sp =>
        {
            HttpClient httpClient = new()
            {
                BaseAddress = apiServerAddress
            };
            return httpClient;
        });
        services.AddWindowsFormsBlazorWebView();
        if (BuildConfiguration.IsDebug())
        {
            services.AddBlazorWebViewDeveloperTools();
        }
        services.AddWindowsServices();
        Services = services.BuildServiceProvider();

        // https://github.com/velopack/velopack
        VelopackApp.Build().Run();
        _ = Task.Run(async () =>
        {
            try
            {
                var windowsUpdateSettings = Services.GetRequiredService<IConfiguration>().GetSection("WindowsUpdateSettings")?.Get<WindowsUpdateSettings>();
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
            Text = "bit BlazorUI",
            WindowState = FormWindowState.Maximized,
            BackColor = ColorTranslator.FromHtml("#0D2960"),
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        };

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            Services = Services,
            HostPage = @"wwwroot\index.html",
            BackColor = ColorTranslator.FromHtml("#0D2960")
        };

        blazorWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml("#0D2960");

        blazorWebView.RootComponents.Add(new RootComponent("#app-container", typeof(Routes), null));

        blazorWebView.BlazorWebViewInitialized += delegate
        {
            blazorWebView.WebView.CoreWebView2.PermissionRequested += async (sender, args) =>
            {
                args.Handled = true;
                args.State = CoreWebView2PermissionState.Allow;
            };
            var settings = blazorWebView.WebView.CoreWebView2.Settings;
#if DEBUG

            settings.IsZoomControlEnabled = false;
            settings.AreBrowserAcceleratorKeysEnabled = false;
#endif
            bool hasBlazorStarted = false;
            blazorWebView.WebView.NavigationCompleted += async delegate
            {
                if (hasBlazorStarted)
                    return;
                hasBlazorStarted = true;
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
            MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public static IServiceProvider? Services { get; private set; }
}
