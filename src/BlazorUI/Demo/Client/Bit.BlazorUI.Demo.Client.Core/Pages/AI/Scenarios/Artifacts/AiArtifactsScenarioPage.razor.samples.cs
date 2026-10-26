namespace Bit.BlazorUI.Demo.Client.Core.Pages.AI.Scenarios.Artifacts;

public partial class AiArtifactsScenarioPage
{
    private readonly string example1RazorCode = """
@implements IAsyncDisposable

<div style="height: 560px">
    <BitAiChat Session="session"
               EmptyTitle="Ask for a document"
               EmptyBody="Send once for a draft, then again to see it revised into version two."
               Suggestions="suggestions" />
</div>
""";

    private readonly string example1CsharpCode = """
private readonly BitAiChatSession session = new(AiDemoContentTransports.Artifacts());

private readonly string[] suggestions = ["Write me a README", "Tighten it and add an install step"];

public ValueTask DisposeAsync() => session.DisposeAsync();
""";

    private const string example1TransportCode = """
public static IBitAiTransport Artifacts() => new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
{
    // The same id on every turn: a later turn revises the artifact rather than creating another.
    const string artifactId = "art-readme";

    // Every turn after the first is a revision, numbered in the body, so no version comes out the same as the one before it.
    var revision = request.Messages.Count(m => m.Role.IsUser) - 1;
    var isRevision = revision > 0;

    await yielder(new BitAiMessageStartedEvent());
    await foreach (var evt in AiScript.Write(isRevision ? $"Revision {revision}." : "Here is a first draft.", 4, 12, token)) await yielder(evt);

    await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
    {
        Language = "markdown",
    });

    var body = isRevision ? $"{RevisedReadme}\n\n_Revision {revision}._" : FirstReadme;

    foreach (var slice in AiScript.Slice(body, 12))
    {
        await AiScript.Pause(60, token);
        await yielder(new BitAiArtifactDeltaEvent(artifactId, slice));
    }

    await yielder(new BitAiArtifactCompletedEvent(artifactId)
    {
        Summary = revision switch
        {
            0 => "First draft",
            1 => "Tightened the opening, added install",
            _ => $"Revision {revision}",
        },
    });

    await yielder(new BitAiMessageCompletedEvent());
}));
""";

    private readonly DemoCodeFile[] example1CodeFiles =
    [
        new("AiDemoContentTransports.cs", example1TransportCode),
    ];
}
