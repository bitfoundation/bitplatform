//+:cnd:noEmit
using System.Text;
using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

/// <summary>
/// The device's own telemetry context, plus what the api says about the request. Routable at <c>/diagnostic</c> - a
/// literal route rather than a <see cref="PageUrls"/> entry, since it is not part of the app's navigation.
/// </summary>
public partial class DiagnosticReport
{
    /// <summary>
    /// Off in <see cref="AppDiagnosticModal"/>: the round trip sends a test push and a test SignalR message, so it
    /// only runs when asked for.
    /// </summary>
    [Parameter] public bool AutoRun { get; set; } = true;

    //#if (notification == true)
    /// <summary>Long enough for a subscription that is going to arrive, short enough not to hold up the report.</summary>
    private static readonly TimeSpan PushSubscriptionPatience = TimeSpan.FromSeconds(5);
    //#endif

    [AutoInject] private Clipboard clipboard = default!;
    //#if (signalR == true)
    [AutoInject] private HubConnection hubConnection = default!;
    //#endif
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private IDiagnosticController diagnosticController = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif
    [AutoInject] private ILogger<DiagnosticReport> logger = default!;

    private readonly List<string> serverLines = [];
    private readonly List<string> clientLines = [];

    private Dictionary<string, object?> context = [];

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        BuildContext();

        // On init rather than after first render, so a prerendered visit carries the answer in its html. The typed
        // controller caches it through the prerender state, so the interactive pass does not ask again.
        if (AutoRun)
        {
            await Run();
        }
    }

    private void BuildContext()
    {
        context = telemetryContext.ToDictionary();

        if (InPrerenderSession)
        {
            // Prerendering runs in the web server's process, so the reported platform would be the host's os - on an
            // anonymous page.
            context[nameof(ITelemetryContext.Platform)] = "Generic Server";
        }
    }

    private async Task Run()
    {
        serverLines.Clear();
        clientLines.Clear();
        BuildContext();

        AddClientLines();
        StateHasChanged();

        string? signalRConnectionId = null;
        string? pushNotificationSubscriptionDeviceId = null;

        //#if (signalR == true)
        try
        {
            signalRConnectionId = hubConnection.State == HubConnectionState.Connected ? hubConnection.ConnectionId : null;
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to get SignalR ConnectionId for diagnostic.");
        }
        //#endif

        //#if (notification == true)
        try
        {
            // Capped: subscribing raises the notification prompt, and safari leaves that pending until someone
            // answers it - taking the whole report below with it, for one line of it.
            pushNotificationSubscriptionDeviceId = (await pushNotificationService.GetSubscription(CurrentCancellationToken)
                .WaitAsync(PushSubscriptionPatience, CurrentCancellationToken))!.DeviceId;
        }
        catch (Exception exp)
        {
            logger.LogWarning(exp, "Failed to get Push Notification Subscription DeviceId for diagnostic.");
        }
        //#endif

        serverLines.AddRange(await diagnosticController.PerformDiagnostic(signalRConnectionId, pushNotificationSubscriptionDeviceId, CurrentCancellationToken));
    }

    //#if (signalR == true)
    /// <summary>
    /// Asks the same thing over the websocket instead of http, replacing the report. A websocket upgrade is its own
    /// path through a proxy, so any difference between the two is the finding; <c>Via:</c> says which is on screen.
    /// </summary>
    private async Task RunOverSignalR()
    {
        serverLines.Clear();
        clientLines.Clear();
        BuildContext();

        AddClientLines();
        StateHasChanged();

        // Throws with its own message when the connection is not up, which is what the user needs to be told anyway.
        serverLines.AddRange(await hubConnection.InvokeAsync<string[]>(SharedAppMessages.GetDiagnosticReport, CurrentCancellationToken));
    }
    //#endif

    private void AddClientLines()
    {
        StringBuilder runtime = new();

        try
        {
            runtime.AppendLine($"IsDynamicCodeCompiled: {RuntimeFeature.IsDynamicCodeCompiled}");
            runtime.AppendLine($"IsDynamicCodeSupported: {RuntimeFeature.IsDynamicCodeSupported}");
            runtime.AppendLine($"Is Aot: {new StackTrace(false).GetFrame(0)?.GetMethod() is null}"); // No 100% Guaranteed way to detect AOT.
            runtime.AppendLine($"Env version: {Environment.Version}");
            runtime.AppendLine($"64 bit process: {Environment.Is64BitProcess}");
            runtime.AppendLine($"Privilaged process: {Environment.IsPrivilegedProcess}");

            if (GC.GetConfigurationVariables().TryGetValue("ServerGC", out var serverGC))
                runtime.AppendLine($"ServerGC: {serverGC}");

            if (GC.GetConfigurationVariables().TryGetValue("ConcurrentGC", out var concurrentGC))
                runtime.AppendLine($"ConcurrentGC: {concurrentGC}");
        }
        catch (Exception exp)
        {
            runtime.AppendLine($"Error while getting diagnostic data: {exp.Message}");
        }

        clientLines.Add(runtime.ToString().TrimEnd());
    }

    private async Task Copy()
    {
        StringBuilder all = new();

        foreach (var (key, value) in context)
        {
            all.AppendLine($"{key}: {value}");
        }

        foreach (var line in serverLines.Concat(clientLines))
        {
            all.AppendLine();
            all.AppendLine(line);
        }

        await clipboard.WriteText(all.ToString());
    }
}
