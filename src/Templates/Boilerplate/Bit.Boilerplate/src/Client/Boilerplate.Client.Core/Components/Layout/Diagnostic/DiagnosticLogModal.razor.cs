using Boilerplate.Shared.Dtos.Diagnostic;

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

public partial class DiagnosticLogModal
{
    [CascadingParameter] public BitDir? CurrentDir { get; set; }

    [Parameter] public BitColor Color { get; set; }
    [Parameter] public string? Content { get; set; }
    [Parameter] public bool IsLogModalOpen { get; set; }
    [Parameter] public EventCallback<bool> IsLogModalOpenChanged { get; set; }
    [Parameter] public EventCallback OnCopy { get; set; }
    [Parameter] public EventCallback<bool> OnNav { get; set; }


    [AutoInject] private Clipboard clipboard = default!;
}