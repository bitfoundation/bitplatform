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

// A transport that reads the files it is sent, and sends them back.
private readonly BitAiChatSession session = new(AiDemoContentTransports.ReadsAttachments());

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

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
// AiScriptedTransport and AiScript are the demo's helpers - see the Basic chat scenario.
public static IBitAiTransport ReadsAttachments() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    // The files are on the user message, as BitAiAttachments.
    var said = AiScript.LastUserMessage(request);
    IReadOnlyList<BitAiAttachment> files = said is null ? [] : said.Attachments;

    await yielder(new BitAiMessageStartedEvent());

    if (files.Count == 0)
    {
        var nothing = $"You said: {said?.GetText()}\n\nAttach a file as well, and I will read it and send it back.";
        await foreach (var evt in AiScript.Write(nothing, 4, 12, token)) await yielder(evt);
        await yielder(AiScript.Usage(request, nothing));
        await yielder(new BitAiMessageCompletedEvent());
        return;
    }

    // A small file arrives with its bytes; a larger one is read from the browser when something opens it.
    var lines = files.Select(f =>
        $"- **{f.Name}**: {f.ContentType ?? "an unknown type"}, " +
        $"{(f.Size is { } size ? $"{size / 1024.0:0.#} KB" : "size unknown")}, " +
        (f.HasInlineData ? "sent inline" : "read on demand"));

    var reply = (files.Count == 1 ? "I received 1 file:" : $"I received {files.Count} files:") + "\n\n" +
                string.Join("\n", lines) + "\n\n" +
                "Here " + (files.Count == 1 ? "it is" : "they are") + " back, the way a model returns a file it made:";

    await foreach (var evt in AiScript.Write(reply, 4, 12, token)) await yielder(evt);

    foreach (var file in files)
    {
        await AiScript.Pause(250, token);

        // An image is drawn in the answer - from its data URL, or from the browser's own preview of a file too large to
        // hold inline; anything else is offered as a file.
        if (file.Kind is BitAiAttachmentKind.Image && (file.Url ?? file.ThumbnailUrl) is { Length: > 0 } url)
        {
            await yielder(new BitAiImageEvent(url) { Alt = file.Name });
        }
        else
        {
            await yielder(new BitAiFileEvent(file));
        }
    }

    await yielder(AiScript.Usage(request, reply));
    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoContentTransports.cs", example1TransportCode),
    ];
}
