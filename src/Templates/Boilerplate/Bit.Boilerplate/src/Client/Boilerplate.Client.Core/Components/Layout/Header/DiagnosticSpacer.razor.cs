namespace Boilerplate.Client.Core.Components.Layout.Header;

public partial class DiagnosticSpacer
{
    private int clickCount = 0;
    private async Task HandleOnClick()
    {
        if (++clickCount == 7)
        {
            clickCount = 0;
            PubSubService.Publish(ClientPubSubMessages.SHOW_DIAGNOSTIC_MODAL);
        }
    }
}
