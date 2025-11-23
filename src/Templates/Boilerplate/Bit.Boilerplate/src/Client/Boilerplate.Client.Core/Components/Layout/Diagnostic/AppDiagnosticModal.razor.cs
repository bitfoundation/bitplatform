//#if (signalR == true)
using Microsoft.AspNetCore.SignalR.Client;
//#endif
using Boilerplate.Shared.Controllers.Diagnostics;
using Boilerplate.Client.Core.Services.DiagnosticLog;
using System.Text.RegularExpressions;
using Boilerplate.Shared.Dtos.Diagnostic;

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

/// <summary>
/// This modal can be opened by clicking 7 times on the spacer of the header or by pressing Ctrl+Shift+X.
/// Also by calling `App.showDiagnostic` function using the dev-tools console.
/// </summary>
public partial class AppDiagnosticModal
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }


    [AutoInject] private Clipboard clipboard = default!;
    //#if (signalR == true)
    [AutoInject] private HubConnection hubConnection = default!;
    //#endif
    [AutoInject] private ITelemetryContext telemetryContext = default!;
    [AutoInject] private BitMessageBoxService messageBoxService = default!;
    [AutoInject] private IDiagnosticsController diagnosticsController = default!;
    //#if (notification == true)
    [AutoInject] private IPushNotificationService pushNotificationService = default!;
    //#endif


    private static bool showKnownException = true;

    private bool isOpen;
    private bool enableRegExp;
    private string? searchText;
    private bool isLogModalOpen;
    private int selectedLogIndex;
    private DiagnosticLogDto? selectedLog;
    private bool isDescendingSort = true;
    private Action unsubscribe = default!;
    private IEnumerable<string>? filterCategoryValues;
    private DiagnosticLogDto[] allLogs = default!;
    private BitDropdownItem<string>[] allCategoryItems = [];
    private DiagnosticLogDto[] filteredLogs = default!;
    private BitBasicList<(DiagnosticLogDto, int)> logStackRef = default!;
    private readonly BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();
    private IEnumerable<LogLevel> filterLogLevelValues = AppEnvironment.IsDevelopment()
                                                            ? [LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical]
                                                            : [LogLevel.Warning, LogLevel.Error, LogLevel.Critical];


    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        unsubscribe = PubSubService.Subscribe(ClientAppMessages.SHOW_DIAGNOSTIC_MODAL, async _ =>
        {
            isOpen = true;
            ReloadLogs();
            await InvokeAsync(StateHasChanged);
        });
    }


    private void HandleOnSortClick()
    {
        isDescendingSort = !isDescendingSort;
        FilterLogs();
    }

    private async Task CopyTelemetry()
    {
        await clipboard.WriteText(string.Join(Environment.NewLine, telemetryContext.ToDictionary().Select(c => $"{c.Key}: {c.Value}")));
    }

    private async Task CopyLog(DiagnosticLogDto? log)
    {
        if (log is null) return;

        await clipboard.WriteText(GetContent(log));
    }

    private async Task OpenLog(DiagnosticLogDto log, int index)
    {
        selectedLog = log;
        selectedLogIndex = index;

        isLogModalOpen = true;
    }

    private static string GetContent(DiagnosticLogDto? log)
    {
        if (log is null) return string.Empty;

        var stateToCopy = string.Join(Environment.NewLine, log.State?.Select(i => $"{i.Key}: {i.Value}") ?? []);
        return $"{log.Category}{Environment.NewLine}{log.Message}{Environment.NewLine}{log.ExceptionString}{Environment.NewLine}{stateToCopy}";
    }

    private async Task GoTop()
    {
        await logStackRef.RootElement.Scroll(0, 0);
    }

    private static BitColor GetColor(LogLevel? level)
    {
        return level switch
        {
            LogLevel.Trace => BitColor.PrimaryForeground,
            LogLevel.Debug => BitColor.PrimaryForeground,
            LogLevel.Information => BitColor.Primary,
            LogLevel.Warning => BitColor.Warning,
            LogLevel.Error => BitColor.Error,
            LogLevel.Critical => BitColor.Error,
            LogLevel.None => BitColor.SecondaryForeground,
            _ => BitColor.TertiaryForeground
        };
    }

    private async Task ClearLogs()
    {
        DiagnosticLogger.Store.Clear();
        ReloadLogs();
    }

    private void LoadLogs(DiagnosticLogDto[] logs)
    {
        allLogs = logs;

        var allCategories = allLogs.Where(c => string.IsNullOrEmpty(c.Category) is false)
                                   .Select(l => l.Category!)
                                   .Distinct()
                                   .Order()
                                   .ToArray();

        filterCategoryValues ??= [.. allCategories];

        allCategoryItems = [.. allCategories.Select(c => new BitDropdownItem<string>() { Text = c, Value = c })];

        FilterLogs();
    }

    private void ReloadLogs()
    {
        LoadLogs([.. DiagnosticLogger.Store]);
    }

    private void FilterLogs()
    {
        var query = FilterSearchText(allLogs);

        query = query.Where(l => filterLogLevelValues.Contains(l.Level))
                                   .Where(l => filterCategoryValues?.Contains(l.Category) is true)
                                   .OrderByDescendingIf(isDescendingSort, l => l.CreatedOn)
                                   .OrderByIf(isDescendingSort is false, l => l.CreatedOn);

        filteredLogs = [.. query];

        IEnumerable<DiagnosticLogDto> FilterSearchText(DiagnosticLogDto[] logs)
        {
            if (string.IsNullOrEmpty(searchText)) return logs;

            if (enableRegExp)
            {
                try
                {
                    var regExp = new Regex(searchText, RegexOptions.IgnoreCase);

                    return [.. logs.Where(l => regExp.IsMatch(l.Message ?? string.Empty) ||
                                               regExp.IsMatch(l.Category ?? string.Empty) ||
                                               l.State?.Any(s => regExp.IsMatch(s.Key) || regExp.IsMatch(s.Value ?? string.Empty)) is true)];
                }
                catch
                {
                    return [];
                }
            }

            return [.. logs.Where(l => l.Message?.Contains(searchText, StringComparison.InvariantCultureIgnoreCase) is true ||
                                       l.Category?.Contains(searchText, StringComparison.InvariantCultureIgnoreCase) is true ||
                                       l.State?.Any(s => s.Key.Contains(searchText) ||
                                                         s.Value?.Contains(searchText, StringComparison.InvariantCultureIgnoreCase) is true) is true)];
        }
    }

    private async Task NavLog(bool isNext)
    {
        if (filteredLogs.Length == 0) return;

        selectedLogIndex = Math.Clamp(
            selectedLogIndex + (isNext ? 1 : -1),
            0,
            filteredLogs.Length - 1);

        await OpenLog(filteredLogs[selectedLogIndex], selectedLogIndex);
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        unsubscribe?.Invoke();
        await base.DisposeAsyncCore();
    }
}
