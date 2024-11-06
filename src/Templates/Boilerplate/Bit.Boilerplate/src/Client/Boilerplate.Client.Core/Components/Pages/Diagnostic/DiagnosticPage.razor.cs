using Microsoft.Extensions.Logging;

namespace Boilerplate.Client.Core.Components.Pages.Diagnostic;

public partial class DiagnosticPage
{
    private DiagnosticLog[] allLogs = [];
    private BitDropdownItem<LogLevel>[] logLevelItems = Enum.GetValues<LogLevel>().Select(v => new BitDropdownItem<LogLevel>() { Value = v, Text = v.ToString() }).ToArray();

    protected override Task OnInitAsync()
    {
        allLogs = Enumerable.Range(1, 10).Select(i => new DiagnosticLog() { Level = LogLevel.Error, Message = $"This is an error({i})" }).ToArray();
        return base.OnInitAsync();
    }
}
