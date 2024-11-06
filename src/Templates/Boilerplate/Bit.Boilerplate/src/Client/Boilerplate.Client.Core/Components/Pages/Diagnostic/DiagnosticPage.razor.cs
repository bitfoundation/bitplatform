using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Pages.Diagnostic;

public partial class DiagnosticPage
{
    private IEnumerable<DiagnosticLog> allLogs = default!;
    private IEnumerable<DiagnosticLog> filteredLogs = default!;
    private BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();

    protected override Task OnInitAsync()
    {
        allLogs = Enumerable.Range(1, 100).Select(i => new DiagnosticLog() { Level = LogLevel.Error, Message = $"This is an error({i})" });
        filteredLogs = allLogs;

        return base.OnInitAsync();
    }

    private async Task HandleOnSearchChange(string? searchText)
    {
        filteredLogs = allLogs.Where(l => l.Message?.Contains(searchText ?? "") ?? false);
    }

    private async Task HandleOnLogLevelFilter(LogLevel logLevel)
    {
        filteredLogs = allLogs.Where(l => l.Level == logLevel);
    }
}
