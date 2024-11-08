using Boilerplate.Client.Core.Services.DiagnosticLog;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class DiagnosticModal : IDisposable
{
    private bool isOpen;
    private string? searchText;
    private bool isDescendingSort;
    private Action unsubscribe = default!;
    private IEnumerable<LogLevel> filterLogLevels = [];
    private IEnumerable<DiagnosticLog> allLogs = default!;
    private IEnumerable<DiagnosticLog> filteredLogs = default!;
    private BitBasicList<(DiagnosticLog, int)> logStackRef = default!;
    private BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();


    [AutoInject] private Clipboard clipboard = default!;
    [AutoInject] private ConcurrentBag<DiagnosticLog> store = default!;


    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(PubSubMessages.SHOW_DIAGNOSTIC_MODAL, async _ =>
        {
            isOpen = true;
            allLogs = [.. store];
            HandleOnLogLevelFilter([LogLevel.Information]);
            await InvokeAsync(StateHasChanged);
        });

        return base.OnInitAsync();
    }


    private void HandleOnSearchChange(string? text)
    {
        searchText = text;
        FilterLogs();
    }

    private void HandleOnLogLevelFilter(BitDropdownItem<LogLevel>[] items)
    {
        HandleOnLogLevelFilter(items.Select(i => i.Value));
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
        filteredLogs = allLogs.Where(l => l.Message?.Contains(searchText ?? "", StringComparison.InvariantCultureIgnoreCase) ?? false)
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

    private async Task CopyException(DiagnosticLog log)
    {
        var stateToCopy = string.Join(Environment.NewLine, log.State?.Select(i => $"{i.Key}: {i.Value}") ?? []);

        await clipboard.WriteText($"{log.Message}{Environment.NewLine}{log.Exception?.ToString()}{Environment.NewLine}{stateToCopy}");
    }

    private async Task GoTop()
    {
        await logStackRef.RootElement.Scroll(0, 0);
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
