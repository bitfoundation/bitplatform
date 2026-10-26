namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Attachments;

public partial class AiAttachmentsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               AllowAttachments
               AllowDictation
               MaxAttachmentSize="MaxSize"
               OnAttachmentRejected="OnRejected"
               EmptyTitle="Attach something"
               EmptyBody="Drag a file onto the composer, paste a screenshot, or use the paperclip. Files over 2 MB are turned away." />
</div>

<aside aria-label="Turned away">
    <h4>Turned away</h4>
    @if (rejections.Count == 0)
    {
        <p>Nothing yet. Try a file over 2 MB.</p>
    }
    else
    {
        <ul>
            @foreach (var rejection in rejections)
            {
                <li><strong>@rejection.FileName</strong>: @Describe(rejection)</li>
            }
        </ul>
    }
</aside>
""";

    private readonly string example1CsharpCode = """
private const long MaxSize = 2 * 1024 * 1024;

private readonly List<BitAiAttachmentRejection> rejections = [];
private BitAiChatSession? session;

[Inject] private BitAiChatSessionFactory SessionFactory { get; set; } = default!;

protected override void OnInitialized() => session = SessionFactory.Create();

// The composer already tells the user; this is where an application would log or count it.
private void OnRejected(BitAiAttachmentRejection rejection)
{
    rejections.Insert(0, rejection);
    if (rejections.Count > 5) rejections.RemoveAt(rejections.Count - 1);
}

private static string Describe(BitAiAttachmentRejection rejection) => rejection.Reason switch
{
    BitAiAttachmentRejectionReason.TooLarge => $"{rejection.Size / 1024.0 / 1024.0:0.0} MB is over the limit",
    BitAiAttachmentRejectionReason.TypeNotAllowed => "that type is not accepted",
    BitAiAttachmentRejectionReason.TooMany => "more files than can be attached at once",
    _ => "it could not be read",
};

public ValueTask DisposeAsync() => session?.DisposeAsync() ?? ValueTask.CompletedTask;
""";
}
