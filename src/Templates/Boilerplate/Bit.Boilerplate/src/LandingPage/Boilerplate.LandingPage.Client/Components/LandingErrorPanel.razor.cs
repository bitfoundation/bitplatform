using System.Runtime.ExceptionServices;

namespace Boilerplate.LandingPage.Client.Components;

public partial class LandingErrorPanel
{
    [Parameter, EditorRequired] public Exception Exception { get; set; } = default!;

    protected override void OnInitialized()
    {
        // While prerendering the failure goes on to the server's exception handler as a 500, since a page answering 200 with this panel would stay on a CDN
        if (RendererInfo.IsInteractive is false)
        {
            ExceptionDispatchInfo.Throw(Exception);
        }

        base.OnInitialized();
    }
}
