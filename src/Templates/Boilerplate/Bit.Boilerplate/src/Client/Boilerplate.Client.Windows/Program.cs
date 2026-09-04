//+:cnd:noEmit
// [mirror] the WebView2 permission allow-list - keep in sync with:
// - src/Client/Boilerplate.Client.Maui/MauiProgram.cs (HandlePermissionRequested, inside the Windows target)
// Only that handler mirrors: the culture bootstrap, LogException and the PAGE_DATA_CHANGED subscription below
// deliberately differ from their MAUI counterparts, because the APIs available to each host differ.
using Velopack;

using System.Diagnostics.CodeAnalysis;

using Boilerplate.Client.Core.Components;
using Boilerplate.Client.Windows.Infrastructure.Services;

using Microsoft.Extensions.Options;
using Microsoft.Web.WebView2.Core;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;

namespace Boilerplate.Client.Windows;

public partial class Program
{
    [STAThread]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(HeadOutlet))]
    public static void Main(string[] args)
    {
        VelopackApp.Build().Run();

        Application.ThreadException += (_, e) => LogException(e.Exception, reportedBy: nameof(Application.ThreadException));
        AppDomain.CurrentDomain.UnhandledException += (_, e) => LogException(e.ExceptionObject, reportedBy: nameof(AppDomain.UnhandledException));
        TaskScheduler.UnobservedTaskException += (_, e) => { LogException(e.Exception, reportedBy: nameof(TaskScheduler.UnobservedTaskException)); e.SetObserved(); };

        ApplicationConfiguration.Initialize();

        AppPlatform.IsBlazorHybrid = true;
        ITelemetryContext.Current = new WindowsTelemetryContext();

        Application.SetColorMode(SystemColorMode.System);

        var configuration = new ConfigurationBuilder()
            .AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Windows")
            .AddEnvironmentVariables()
            .Build();

        var services = new ServiceCollection();
        services.AddClientWindowsProjectServices(configuration);
        Services = services.BuildServiceProvider();

        Services.GetService<IStartupValidator>()?.Validate();

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            var culture = Services.GetRequiredService<IStorageService>()
                .GetItem("Culture")
                .GetAwaiter()
                .GetResult();
            CultureInfoManager.SetCurrentCulture(
                culture ?? // 1- User settings
                CultureInfo.CurrentUICulture.Name); // 2- OS Settings
        }

        var form = new Form()
        {
            Text = "Boilerplate",
            Height = 768,
            Width = 1024,
            MinimumSize = new Size(375, 667),
            WindowState = FormWindowState.Maximized,
            BackColor = ColorTranslator.FromHtml("#0D2960"),
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        };
        var pubSubService = Services.GetRequiredService<PubSubService>();
        pubSubHandlerReferenceToKeepAlive = pubSubService.Subscribe(ClientAppMessages.PAGE_DATA_CHANGED, async args =>
        {
            var (title, _, __) = ((string? title, string?, bool))args!;
            await form.InvokeAsync(() =>
            {
                form.Text = title ?? "Boilerplate";
            });
        });

        _ = Task.Run(async () =>
        {
            try
            {
                await ((WindowsAppUpdateService)Services.GetRequiredService<IAppUpdateService>()).Update();
            }
            catch (Exception exp)
            {
                Services.GetRequiredService<ClientExceptionHandlerBase>().Handle(exp);
            }
        });

        Environment.SetEnvironmentVariable("WEBVIEW2_ADDITIONAL_BROWSER_ARGUMENTS", "--unsafely-treat-insecure-origin-as-secure=https://0.0.0.1 --enable-notifications --remote-debugging-port=9222");

        var blazorWebView = new BlazorWebView
        {
            Dock = DockStyle.Fill,
            Services = Services,
            HostPage = @"wwwroot\index.html",
            BackColor = ColorTranslator.FromHtml("#0D2960")
        };

        blazorWebView.WebView.DefaultBackgroundColor = ColorTranslator.FromHtml("#0D2960");

        blazorWebView.RootComponents.Add(new RootComponent("head::after", typeof(HeadOutlet), null));
        blazorWebView.RootComponents.Add(new RootComponent("#app-container", typeof(Routes), null));

        blazorWebView.BlazorWebViewInitialized += delegate
        {
            blazorWebView.WebView.CoreWebView2.PermissionRequested += (sender, args) =>
            {
                if (args.PermissionKind is not (CoreWebView2PermissionKind.Microphone
                             or CoreWebView2PermissionKind.ClipboardRead
                             or CoreWebView2PermissionKind.Notifications)) return;

                args.Handled = true;
                args.State = CoreWebView2PermissionState.Allow;
            };
            _ = StartBlazor(blazorWebView);
        };

        form.Controls.Add(blazorWebView);

        Application.Run(form);
    }

    static async Task StartBlazor(BlazorWebView blazorWebView)
    {
        while (await blazorWebView.WebView.ExecuteScriptAsync("Blazor.start()") is "null")
        {
            await Task.Yield();
        }
    }

    private static void LogException(object? error, string reportedBy)
    {
        if (Services is not null && error is Exception exp)
        {
            Services.GetRequiredService<ClientExceptionHandlerBase>().Handle(exp, parameters: new()
            {
                { nameof(reportedBy), reportedBy }
            }, displayKind: AppEnvironment.IsDevelopment() ? ExceptionDisplayKind.NonInterrupting : ExceptionDisplayKind.None);
        }
        else
        {
            var errorMessage = error?.ToString() ?? "Unknown error";
            // The dialog first: this branch runs before the DI container exists, so it is the only report a WinForms
            // process launched from Explorer can make. Clipboard.SetText throws when another process is holding the
            // clipboard (and off an STA thread), which would otherwise swallow the dialog with it.
            System.Windows.Forms.MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            try
            {
                Clipboard.SetText(errorMessage); // so the user can paste it into a bug report
            }
            catch { }
        }
    }

    public static IServiceProvider? Services { get; private set; }

    /// <summary>
    /// Strong root for the PAGE_DATA_CHANGED subscription, which PubSubService itself only holds weakly.
    /// </summary>
    private static Action? pubSubHandlerReferenceToKeepAlive;
}
