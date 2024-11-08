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


    [AutoInject] private Clipboard clipboard = default!;


    protected override Task OnInitAsync()   
    {
        unsubscribe = PubSubService.Subscribe(PubSubMessages.SHOW_DIAGNOSTIC_MODAL, async _ =>
        {
            isOpen = true;
            allLogs = [.. DiagnosticLogger.Store];
            await HandleOnLogLevelFilter(LogLevel.Information);
            await InvokeAsync(StateHasChanged);
        });

        return base.OnInitAsync();
    }

    private async Task HandleOnSearchChange(string? searchText)
    {
        filteredLogs = allLogs.Where(l => l.Message?.Contains(searchText ?? "") ?? false);
    }

    private async Task HandleOnLogLevelFilter(LogLevel logLevel)
    {
        filteredLogs = allLogs.Where(l => l.Level >= logLevel);
    }

    private static BitColor GetColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => BitColor.Info,
            LogLevel.Debug => BitColor.Secondary,
            LogLevel.Information => BitColor.Primary,
            LogLevel.Warning => BitColor.Warning,
            LogLevel.Error => BitColor.SevereWarning,
            LogLevel.Critical => BitColor.Error,
            LogLevel.None => BitColor.SecondaryForeground,
            _ => BitColor.TertiaryForeground
        };
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


    public void Dispose()
    {
        unsubscribe?.Invoke();
    }
}
