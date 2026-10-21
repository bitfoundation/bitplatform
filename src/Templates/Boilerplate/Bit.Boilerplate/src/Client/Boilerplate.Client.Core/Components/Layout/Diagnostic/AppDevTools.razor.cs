using Bit.BevTools;

namespace Boilerplate.Client.Core.Components.Layout.Diagnostic;

public partial class AppDevTools
{
    [Parameter] public bool Open { get; set; }

    [AutoInject] private IServiceProvider serviceProvider = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        var session = BitBevTools.Attach(serviceProvider);

        if (Open)
        {
            session.Ui.Open();
        }
    }
}
