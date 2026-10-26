namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Attachments;

public partial class AiAttachmentsScenarioPage
{
    private const long MaxSize = 2 * 1024 * 1024;

    private readonly List<BitAiAttachmentRejection> _rejections = [];
    private BitAiChatSession? _session;

    [AutoInject] private BitAiChatSessionFactory _sessionFactory = default!;

    protected override Task OnInitAsync()
    {
        _session = _sessionFactory.Create();

        return base.OnInitAsync();
    }

    // The composer already tells the user; this is where an application would log or count it.
    private void OnRejected(BitAiAttachmentRejection rejection)
    {
        _rejections.Insert(0, rejection);
        if (_rejections.Count > 5) _rejections.RemoveAt(_rejections.Count - 1);
    }

    private static string Describe(BitAiAttachmentRejection rejection) => rejection.Reason switch
    {
        BitAiAttachmentRejectionReason.TooLarge => $"{rejection.Size / 1024.0 / 1024.0:0.0} MB is over the limit",
        BitAiAttachmentRejectionReason.TypeNotAllowed => "that type is not accepted",
        BitAiAttachmentRejectionReason.TooMany => "more files than can be attached at once",
        _ => "it could not be read",
    };

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        if (_session is not null) await _session.DisposeAsync();

        await base.DisposeAsync(disposing);
    }
}
