//+:cnd:noEmit
using System.Text;
using Boilerplate.Shared.Features.Diagnostic;

namespace Boilerplate.Client.Core.Components.Pages.Management;

/// <summary>
/// Health checks read from /healthz (See MapAppHealthChecks), and the Hangfire dashboard. Only the title is localized,
/// as the report isn't.
/// </summary>
public partial class OperationsPage
{
    private const int MaxProbesPerCheck = 24;

    [AutoInject] private HttpClient httpClient = default!;
    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private IUserController userController = default!;
    [AutoInject] private IDiagnosticController diagnosticController = default!;
    [AutoInject] private IExternalNavigationService externalNavigationService = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif
    //#if (signalR == true || notification == true)
    [AutoInject] private NotificationPreferenceService notificationPreferenceService = default!;
    //#endif

    private HealthReportDto? report;
    private List<CheckView> checks = [];
    private string? loadError;
    private bool isLoading;
    private DateTimeOffset? loadedAt;

    private bool autoRefresh = true;
    private int refreshSeconds = 30;
    private readonly BitDropdownItem<int>[] refreshIntervals =
    [
        new() { Text = "Every 10 seconds", Value = 10 },
        new() { Text = "Every 30 seconds", Value = 30 },
        new() { Text = "Every minute", Value = 60 },
        new() { Text = "Every 5 minutes", Value = 300 }
    ];

    private string? searchText;
    private StatusFilter statusFilter = StatusFilter.All;

    private readonly Dictionary<string, List<Probe>> probes = [];
    private string? copiedCheck;

    private ManualTest runningTest;
    private readonly Dictionary<ManualTest, (bool Sent, string Message)> testResults = [];

    /// <summary>
    /// A ring with one segment per status, so a mix of healthy and degraded checks reads at a glance.
    /// </summary>
    private readonly BitChartConfig gaugeChart = new()
    {
        Type = BitChartType.Doughnut,
        Options = new()
        {
            MaintainAspectRatio = false,
            CutoutPercentage = 76,
            CircumferenceDegrees = 360,
            // Starts at 12 o'clock; the chart measures its rotation from 3 o'clock.
            RotationDegrees = -90,
            Plugins = new()
            {
                Legend = new() { Display = false },
                Tooltip = new() { LabelFormatter = (_, value) => $"{value:N0} checks" }
            }
        }
    };

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await Load();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        _ = KeepRefreshing();
    }

    /// <summary>
    /// Keeps the "... ago" texts fresh and reloads once the chosen interval has passed.
    /// </summary>
    private async Task KeepRefreshing()
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5));

            while (await timer.WaitForNextTickAsync(CurrentCancellationToken))
            {
                if (autoRefresh && isLoading is false && (loadedAt is null || DateTimeOffset.UtcNow - loadedAt >= TimeSpan.FromSeconds(refreshSeconds)))
                {
                    await Load();
                }

                StateHasChanged();
            }
        }
        catch (OperationCanceledException)
        {
            // The page was left.
        }
    }

    private async Task Load()
    {
        if (isLoading) return;

        isLoading = true;
        StateHasChanged();

        try
        {
            var newReport = await httpClient.GetFromJsonAsync("healthz", JsonSerializerOptions.GetTypeInfo<HealthReportDto>(), CurrentCancellationToken)
                ?? throw new InvalidOperationException("/healthz returned no report.");

            RecordProbes(newReport);

            report = newReport;
            checks = [.. newReport.Entries.Select(entry => new CheckView(entry.Key, entry.Value))
                                          .OrderBy(check => check.Rank)
                                          .ThenBy(check => check.Title)];
            loadError = null;
            loadedAt = DateTimeOffset.UtcNow;

            UpdateGaugeChart();
        }
        catch (Exception exp) when (exp is not OperationCanceledException)
        {
            // The last report stays on screen, so a network hiccup doesn't blank the page.
            loadError = exp.Message;
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }

    private void RecordProbes(HealthReportDto newReport)
    {
        foreach (var (name, entry) in newReport.Entries)
        {
            var history = probes.TryGetValue(name, out var list) ? list : probes[name] = [];
            history.Add(new(newReport.CheckedAt, entry.Status, new CheckView(name, entry).Duration));
            if (history.Count > MaxProbesPerCheck)
            {
                history.RemoveAt(0);
            }
        }
    }

    private void UpdateGaugeChart()
    {
        (Tone Tone, StatusFilter Filter, string Label)[] segments =
        [
            (Tone.Healthy, StatusFilter.Healthy, "Healthy"),
            (Tone.Degraded, StatusFilter.Degraded, "Degraded"),
            (Tone.NotConfigured, StatusFilter.NotConfigured, "Not configured"),
            (Tone.Unhealthy, StatusFilter.Unhealthy, "Unhealthy")
        ];
        var shown = segments.Where(segment => Count(segment.Filter) > 0).ToArray();

        gaugeChart.Data = new()
        {
            Labels = [.. shown.Select(segment => segment.Label)],
            Datasets =
            [
                new()
                {
                    Data = [.. shown.Select(segment => (double?)Count(segment.Filter))],
                    BackgroundColors = [.. shown.Select(segment => $"var({ToneCssVar(segment.Tone)})")],
                    BorderWidth = 0,
                    SpacingArc = shown.Length > 1 ? 3 : 0
                }
            ]
        };
        gaugeChart.Options!.Plugins!.Custom =
        [
            new BitChartCenterTextPlugin($"{HealthyPercent:F0}%", "healthy")
            {
                Color = $"var({BitCss.Var.Color.Foreground.Primary.Main})",
                SubtextColor = $"var({BitCss.Var.Color.Foreground.Secondary.Main})"
            }
        ];
    }

    /// <summary>
    /// Opens Hangfire's dashboard on this app's own origin, already signed in as this user. A plain browser navigation
    /// carries only a cookie, which <see cref="IUserController.UpdateSession"/> writes with the token's own expiry - so
    /// the token is refreshed first to buy a full lifetime rather than whatever is left of the current one.
    /// </summary>
    private async Task OpenHangfireDashboard()
    {
        await AuthManager.RefreshToken(requestedBy: nameof(OpenHangfireDashboard));

        await userController.UpdateSession(new()
        {
            AppVersion = TelemetryContext.AppVersion,
            DeviceInfo = TelemetryContext.Platform,
            CultureName = CultureInfoManager.InvariantGlobalization ? null : CultureInfo.CurrentUICulture.Name,
            //#if (signalR == true || notification == true)
            NotificationStatus = await notificationPreferenceService.GetSessionStatus(), // Left out, it would mute the session.
            //#endif
            PlatformType = AppPlatform.Type
        }, CurrentCancellationToken);

        await externalNavigationService.NavigateTo(NavigationManager.ToAbsoluteUri("hangfire").ToString());
    }

    private async Task CopyDetails(CheckView check)
    {
        var text = new StringBuilder()
            .AppendLine($"{check.Title} ({check.Name}): {check.StatusText}")
            .AppendLine($"Checked at: {report?.CheckedAt:O}")
            .AppendLine($"Duration: {check.Duration}")
            .AppendLine($"Description: {check.Entry.Description}");

        foreach (var (key, value) in check.Entry.Data)
        {
            text.AppendLine($"{key}: {value}");
        }

        text.AppendLine().AppendLine(check.Entry.Exception);

        await clipboard.WriteText(text.ToString());

        copiedCheck = check.Name;
        SnackBarService.Success($"{check.Title} details copied");
    }

    private IEnumerable<CheckView> FilteredChecks => checks.Where(check =>
        (statusFilter switch
        {
            StatusFilter.Attention => check.Tone is not Tone.Healthy,
            StatusFilter.Unhealthy => check.Tone is Tone.Unhealthy,
            StatusFilter.Degraded => check.Tone is Tone.Degraded,
            StatusFilter.NotConfigured => check.Tone is Tone.NotConfigured,
            StatusFilter.Healthy => check.Tone is Tone.Healthy,
            _ => true
        })
        && (string.IsNullOrWhiteSpace(searchText)
            || check.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || check.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || check.Category.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || check.Entry.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) is true
            || check.Entry.Exception?.Contains(searchText, StringComparison.OrdinalIgnoreCase) is true));

    private int Count(StatusFilter filter) => filter switch
    {
        StatusFilter.Attention => checks.Count(c => c.Tone is not Tone.Healthy),
        StatusFilter.Unhealthy => checks.Count(c => c.Tone is Tone.Unhealthy),
        StatusFilter.Degraded => checks.Count(c => c.Tone is Tone.Degraded),
        StatusFilter.NotConfigured => checks.Count(c => c.Tone is Tone.NotConfigured),
        StatusFilter.Healthy => checks.Count(c => c.Tone is Tone.Healthy),
        _ => checks.Count
    };

    private double HealthyPercent => checks.Count is 0 ? 0 : 100.0 * Count(StatusFilter.Healthy) / checks.Count;

    // A not configured check reports Degraded too, but it is a setup gap rather than a failing dependency.
    private Tone OverallTone => report?.Status switch
    {
        HealthCheckStatus.Unhealthy => Tone.Unhealthy,
        _ when Count(StatusFilter.Degraded) > 0 => Tone.Degraded,
        _ when Count(StatusFilter.NotConfigured) > 0 => Tone.NotConfigured,
        _ => Tone.Healthy
    };

    private string OverallHeadline => OverallTone switch
    {
        Tone.Healthy or Tone.NotConfigured => "All systems operational",
        Tone.Degraded => "Running with degraded dependencies",
        _ => "Out of rotation"
    };

    private string OverallExplanation => OverallTone switch
    {
        Tone.Healthy => "Every check passed. /health answers 200, so load balancers keep sending traffic here.",
        Tone.NotConfigured => "Every configured dependency passed. Configure the ones marked Not configured, or remove the code that uses them.",
        Tone.Degraded => "/health still answers 200 and the instance stays in rotation, but the features behind the checks below may fail.",
        _ => $"/health answers 503, so load balancers stop sending traffic here until {string.Join(", ", checks.Where(c => c.Tone is Tone.Unhealthy).Select(c => c.Title))} recover."
    };

    private IReadOnlyList<Probe> ProbesOf(CheckView check) => probes.GetValueOrDefault(check.Name) ?? [];

    private void SetFilter(StatusFilter filter) => statusFilter = filter;

    /// <summary>
    /// A failure needs no handling here: the client shows it like any other api error.
    /// </summary>
    private async Task RunTest(ManualTest test)
    {
        runningTest = test;

        try
        {
            testResults[test] = test switch
            {
                //#if (notification == true)
                ManualTest.Push => await SendTestPushNotification(),
                //#endif
                ManualTest.Email => await diagnosticController.SendTestEmail(CurrentCancellationToken)
                    ? (true, "Sent. Check your inbox.")
                    : (false, "Your account has no email."),
                ManualTest.Sms => await diagnosticController.SendTestSms(CurrentCancellationToken)
                    ? (true, "Sent. Check your phone.")
                    : (false, "Your account has no phone number."),
                _ => throw new NotSupportedException()
            };
        }
        finally
        {
            runningTest = ManualTest.None;
        }
    }

    //#if (notification == true)
    private async Task<(bool Sent, string Message)> SendTestPushNotification()
    {
        const string notSubscribed = "This device isn't subscribed. Turn notifications on from the app menu first.";

        string? deviceId;
        try
        {
            // Capped, as a browser can leave the permission prompt pending forever.
            deviceId = (await pushNotificationService.GetSubscription(CurrentCancellationToken)
                .WaitAsync(TimeSpan.FromSeconds(30), CurrentCancellationToken))?.DeviceId;
        }
        catch (TimeoutException)
        {
            return (false, "The notification permission prompt wasn't answered.");
        }

        if (string.IsNullOrWhiteSpace(deviceId))
            return (false, notSubscribed);

        return await diagnosticController.SendTestPushNotification(deviceId, CurrentCancellationToken)
            ? (true, "Sent. The notification should appear shortly.")
            : (false, notSubscribed);
    }
    //#endif

    private static (string Text, string IconName) TestButton(ManualTest test) => test switch
    {
        ManualTest.Push => ("Send a test notification", BitIconName.Ringer),
        ManualTest.Email => ("Send a test email", BitIconName.Mail),
        _ => ("Send a test SMS", BitIconName.Message)
    };

    private void ClearFilters() => (statusFilter, searchText) = (StatusFilter.All, null);

    private void ShowOnly(StatusFilter filter) => (statusFilter, searchText) = (filter, null);

    private string Ago(DateTimeOffset? at)
    {
        if (at is null) return "never";

        var elapsed = DateTimeOffset.UtcNow - at.Value;
        return elapsed.TotalSeconds switch
        {
            < 5 => "just now",
            < 60 => $"{elapsed.TotalSeconds:N0}s ago",
            < 3600 => $"{elapsed.TotalMinutes:N0} min ago",
            < 86400 => $"{elapsed.TotalHours:N0} h ago",
            _ => TimeZoneService.ToLocalTime(at.Value).ToString("g", CultureInfo.InvariantCulture)
        };
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalMilliseconds switch
    {
        < 1 => "<1 ms",
        < 1000 => $"{duration.TotalMilliseconds:N0} ms",
        _ => $"{duration.TotalSeconds:N1} s"
    };

    /// <summary>
    /// A report is as slow as its slowest check; a timed out check takes 10 to 20 seconds.
    /// </summary>
    private static BitColor DurationColor(TimeSpan duration) => duration.TotalSeconds switch
    {
        < 2 => BitColor.Success,
        < 8 => BitColor.Warning,
        _ => BitColor.Error
    };

    /// <summary>
    /// How close a check came to its timeout; one without a timeout can't be judged.
    /// </summary>
    private static BitColor UsageColor(double? usage) => usage switch
    {
        null => BitColor.SecondaryForeground,
        < 50 => BitColor.Success,
        < 80 => BitColor.Warning,
        _ => BitColor.Error
    };

    private static string ToneIcon(Tone tone) => tone switch
    {
        Tone.Healthy => BitIconName.CompletedSolid,
        Tone.Degraded => BitIconName.WarningSolid,
        Tone.Unhealthy => BitIconName.StatusErrorFull,
        Tone.NotConfigured => BitIconName.Settings,
        _ => BitIconName.Info
    };

    private static BitColor ToneColor(Tone tone) => tone switch
    {
        Tone.Healthy => BitColor.Success,
        Tone.Degraded => BitColor.Warning,
        Tone.Unhealthy => BitColor.Error,
        Tone.NotConfigured => BitColor.Info,
        _ => BitColor.SecondaryForeground
    };

    private static string ToneCssVar(Tone tone) => tone switch
    {
        Tone.Healthy => BitCss.Var.Color.Success.Main,
        Tone.Degraded => BitCss.Var.Color.Warning.Main,
        Tone.Unhealthy => BitCss.Var.Color.Error.Main,
        Tone.NotConfigured => BitCss.Var.Color.Info.Main,
        _ => BitCss.Var.Color.Foreground.Secondary.Main
    };

    private static string ToneClass(Tone tone) => tone.ToString().ToLowerInvariant();

    private enum StatusFilter { All, Attention, Unhealthy, Degraded, NotConfigured, Healthy }

    public enum Tone { Healthy, Degraded, Unhealthy, NotConfigured, Neutral }

    public record Probe(DateTimeOffset At, HealthCheckStatus Status, TimeSpan Duration);

    private string LocalTime(DateTimeOffset at, string format) => TimeZoneService.ToLocalTime(at).ToString(format, CultureInfo.InvariantCulture);
}
