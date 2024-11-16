using Boilerplate.Client.Core.Services.DiagnosticLog;

namespace Boilerplate.Client.Core.Components.Layout;

/// <summary>
/// This modal can be opened by clicking 7 times on the spacer of the header or by pressing Ctrl+Shift+X.
/// Also by calling `App.showDiagnostic` function using the dev-tools console.
/// </summary>
public partial class DiagnosticModal : IDisposable
{
    private bool isOpen;
    private string? searchText;
    private bool isDescendingSort = true;
    private Action unsubscribe = default!;
    private IEnumerable<LogLevel> filterLogLevels = [];
    private IEnumerable<DiagnosticLog> allLogs = default!;
    private IEnumerable<DiagnosticLog> filteredLogs = default!;
    private BitBasicList<(DiagnosticLog, int)> logStackRef = default!;
    private readonly BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();
    private readonly LogLevel[] defaultFilterLogLevels = AppEnvironment.IsDev() ? [LogLevel.Information, LogLevel.Warning, LogLevel.Error, LogLevel.Critical] : [LogLevel.Warning, LogLevel.Error, LogLevel.Critical];


    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private ITelemetryContext telemetryContext = default!;


    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL, async _ =>
        {
            isOpen = true;
            allLogs = [.. DiagnosticLogger.Store];
            HandleOnLogLevelFilter(defaultFilterLogLevels);
            await InvokeAsync(StateHasChanged);
        });

        return base.OnInitAsync();
    }


    private void HandleOnSearchChange(string? text)
    {
        searchText = text;
        FilterLogs();
    }

    private void HandleOnLogLevelFilter(IEnumerable<LogLevel> logLevels)
    {
        filterLogLevels = logLevels;
        FilterLogs();
    }

    private void HandleOnSortClick()
    {
        isDescendingSort = !isDescendingSort;
        FilterLogs();
    }

    private void FilterLogs()
    {
        filteredLogs = allLogs.WhereIf(string.IsNullOrEmpty(searchText) is false, l => l.Message?.Contains(searchText!, StringComparison.InvariantCultureIgnoreCase) is true || l.Category?.Contains(searchText!, StringComparison.InvariantCultureIgnoreCase) is true)
                              .Where(l => filterLogLevels.Contains(l.Level));
        if (isDescendingSort)
        {
            filteredLogs = filteredLogs.OrderByDescending(l => l.CreatedOn);
        }
        else
        {
            filteredLogs = filteredLogs.OrderBy(l => l.CreatedOn);
        }
    }

    private async Task CopyTelemetry()
    {
        await clipboard.WriteText(string.Join(Environment.NewLine, telemetryContext.ToDictionary().Select(c => $"{c.Key}: {c.Value}")));
    }

    private async Task CopyException(DiagnosticLog log)
    {
        var stateToCopy = string.Join(Environment.NewLine, log.State?.Select(i => $"{i.Key}: {i.Value}") ?? []);

        await clipboard.WriteText($"{log.Category}{Environment.NewLine}{log.Message}{Environment.NewLine}{log.Exception?.ToString()}{Environment.NewLine}{stateToCopy}");
    }

    private async Task GoTop()
    {
        await logStackRef.RootElement.Scroll(0, 0);
    }

    private async Task ClearLogs()
    {
        DiagnosticLogger.Store.Clear();
        allLogs = [];
        FilterLogs();
    }


    private static BitColor GetColor(LogLevel level)
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


    public void Dispose()
    {
        unsubscribe?.Invoke();
    }
}
