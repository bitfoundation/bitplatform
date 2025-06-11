namespace Boilerplate.Client.Core.Components.Common;

public partial class AppPageData
{
    private string? _lastPublishedMessage;

    [AutoInject] private PubSubService pubSubService = default!;

    [Parameter] public string? PageTitle { get; set; }
    [Parameter] public string? Title { get; set; }
    [Parameter] public string? SubTitle { get; set; }
    [Parameter] public bool ShowGoBackButton { get; set; }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        var publishMessage = $"{PageTitle}-{Title}-{SubTitle}-{ShowGoBackButton}";

        if (_lastPublishedMessage == publishMessage) return;

        _lastPublishedMessage = publishMessage;

        pubSubService.Publish(ClientPubSubMessages.PAGE_DATA_CHANGED, (Title, SubTitle, ShowGoBackButton), persistent: true);
    }
}
