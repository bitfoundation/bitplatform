using Bit.BlazorUI;
using Boilerplate.Client.Core.Services.DiagnosticLog;
using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Layout;

public partial class DiagnosticModal : IDisposable
{
    private bool isOpen;
    private Action unsubscribe = default!;
    private IEnumerable<DiagnosticLog> allLogs = default!;
    private IEnumerable<DiagnosticLog> filteredLogs = default!;
    private BitBasicList<(DiagnosticLog, int)> logStackRef = default!;
    private BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();

    protected override Task OnInitAsync()
    {
        unsubscribe = PubSubService.Subscribe(PubSubMessages.SHOW_DIAGNOSTIC_MODAL, async _ => 
        {
            isOpen = true;
            await InvokeAsync(StateHasChanged);
        });

        allLogs = Enumerable.Range(1, 100000).Select(i => new DiagnosticLog() { Level = LogLevel.Error, Message = $"This is an error({i})" });
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

    private async Task GoTop()
    {
        await logStackRef.RootElement.Scroll(0, 0);
    }


    public void Dispose()
    {
        unsubscribe?.Invoke();
    }
}
