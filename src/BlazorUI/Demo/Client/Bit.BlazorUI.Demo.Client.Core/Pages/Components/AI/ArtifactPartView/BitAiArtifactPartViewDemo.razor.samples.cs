namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.AI.ArtifactPartView;

public partial class BitAiArtifactPartViewDemo
{
    private const string ConversationSampleCode = """"
// A conversation holding one artifact, the way a session leaves it after a turn that wrote one.
private readonly BitAiConversation conversation = new();
private readonly BitAiArtifactPart notesPart = new("art-notes");

protected override void OnInitialized()
{
    var notes = conversation.GetOrAddArtifact("art-notes", BitAiArtifactKind.Document, "Meeting notes");
    notes.SetContent("""
        ## Meeting notes

        - Ship the AI components in the next release.
        - **Owner**: the UI team.
        """);
    notes.Complete("First draft");
}
"""";

    private readonly string example1RazorCode = """
<BitAiArtifactPartView Part="notesPart" Conversation="conversation" />
""";
    private readonly string example1CsharpCode = ConversationSampleCode;

    private readonly string example2RazorCode = """
@implements IAsyncDisposable

<style>
    .turn {
        gap: 0.5rem;
        display: flex;
        flex-direction: column;
        margin-block-end: 1rem;
    }
</style>

<BitButton OnClick="SendAsync" Disabled="readmeSession.IsGenerating">@(readmeSession.Conversation.Messages.Count == 0 ? "Write me a README" : "Tighten it and add an install step")</BitButton>

@foreach (var message in readmeSession.Conversation.Messages)
{
    <div class="turn">
        <b>@(message.Role.IsUser ? "You" : "Assistant"):</b> @message.GetText()
        @foreach (var part in message.PartsOf<BitAiArtifactPart>())
        {
            <BitAiArtifactPartView Part="part" Conversation="readmeSession.Conversation" />
        }
    </div>
}
""";
    private readonly string example2CsharpCode = """
// A scripted transport that drafts a README into the artifact art-readme, and revises it on the next turn (see the next tab).
private readonly BitAiChatSession readmeSession = new(AiDemoContentTransports.Artifacts());
private BitAiRenderScheduler? renderScheduler;

protected override void OnInitialized()
{
    // The session says so on every streamed delta; the scheduler turns any number of those into one render per 50 ms.
    renderScheduler = new(() => InvokeAsync(StateHasChanged));
    readmeSession.StateChanged += renderScheduler.Request;
}

private Task SendAsync() => readmeSession.SubmitAsync(readmeSession.Conversation.Messages.Count == 0 ? "Write me a README" : "Tighten it and add an install step");

public async ValueTask DisposeAsync()
{
    if (renderScheduler is not null) await renderScheduler.DisposeAsync();

    await readmeSession.DisposeAsync();
}
""";
    private const string example2TransportCode = """"
public static IBitAiTransport Artifacts() =>
    new AiScriptedTransport((request, token) => AiScript.Stream(token, async yielder =>
    {
        const string artifactId = "art-readme";

        // Every turn after the first is a revision, numbered in the body, so no version comes out the same as the one
        // before it.
        var revision = request.Messages.Count(m => m.Role.IsUser) - 1;
        var isRevision = revision > 0;

        await yielder(new BitAiMessageStartedEvent());

        var intro = revision switch
        {
            0 => "Here is a first draft. Tell me what to change, and I will revise it.",
            1 => "Revised. I tightened the opening and added the install step.",
            _ => $"Revised again, into revision {revision}.",
        };

        await foreach (var evt in AiScript.Write(intro, 4, 12, token)) await yielder(evt);

        // The session adds a BitAiArtifactPart to this message, stamped with the version this opens.
        await yielder(new BitAiArtifactCreatedEvent(artifactId, BitAiArtifactKind.Document, "Project README")
        {
            Language = "markdown",
        });

        var body = isRevision
            ? $"""
              # Bit.BlazorUI.AI

              The AI UI layer of bit BlazorUI: conversations, streaming, tools, agents, and artifacts.

              ## Install

              ```bash
              dotnet add package Bit.BlazorUI.AI
              ```

              ## Why

              Chat is the easy part. What takes the time is everything around it: partial JSON,
              tool approval, citation anchoring, and not re-rendering the world on every token.

              _Revision {revision}._
              """
            : """
              # Bit.BlazorUI.AI

              The AI components of bit BlazorUI.

              ## Why

              Chat is the easy part. Everything around it is what takes the time.
              """;

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
"""";
    private readonly DemoCodeFile[] example2CodeFiles =
    [
        new("AiDemoContentTransports.cs", example2TransportCode),
    ];

    private readonly string example3RazorCode = """
<BitAiArtifactPartView Part="missingPart" Conversation="conversation" />

<BitAiArtifactPartView Part="notesPart" />
""";
    private readonly string example3CsharpCode = ConversationSampleCode + """

// An artifact the conversation does not hold.
private readonly BitAiArtifactPart missingPart = new("art-deleted");
""";

    private readonly string example4RazorCode = """
<BitParams Parameters="artifactPartViewParams">
    <BitAiArtifactPartView Part="notesPart" Conversation="conversation" />

    <BitAiArtifactPartView Part="missingPart" Conversation="conversation" />

    <BitAiArtifactPartView Part="notesPart" Conversation="conversation" Styles="@(new() { ArtifactView = new() { Title = "font-style: italic;" } })" />
</BitParams>
""";
    private readonly string example4CsharpCode = ConversationSampleCode + """

private readonly BitAiArtifactPart missingPart = new("art-deleted");

private readonly BitAiArtifactPartViewParams[] artifactPartViewParams =
[
    new()
    {
        Styles = new()
        {
            ArtifactView = new() { Header = "background-color: var(--bit-clr-pri-light);" },
            Placeholder = new() { Root = "border-style: dashed;" },
        },
    },
];
""";

    private readonly string example5RazorCode = """
<style>
    .custom-placeholder {
        font-style: italic;
        border-style: dashed;
    }
</style>

<BitAiArtifactPartView Part="notesPart"
                       Conversation="conversation"
                       Styles="@(new() { ArtifactView = new() { Header = "background-color: #8a2be2; color: white" } })" />

<BitAiArtifactPartView Part="missingPart"
                       Conversation="conversation"
                       Classes="@(new() { Placeholder = new() { Root = "custom-placeholder" } })" />

<BitAiArtifactPartView Part="notesPart"
                       Conversation="conversation"
                       Style="--bit-AiArtifactView-border-color: #d2691e; --bit-AiArtifactView-radius: 0; --bit-AiArtifactView-header-background: #d2691e1a" />
""";
    private readonly string example5CsharpCode = ConversationSampleCode + """

private readonly BitAiArtifactPart missingPart = new("art-deleted");
""";

    private readonly string example6RazorCode = """
<BitAiArtifactPartView Dir="BitDir.Rtl" Part="persianPart" Conversation="conversation" />

<div dir="rtl">
    <BitAiArtifactPartView Part="missingPart" Conversation="conversation" />
</div>
""";
    private readonly string example6CsharpCode = """"
private readonly BitAiConversation conversation = new();
private readonly BitAiArtifactPart persianPart = new("art-persian");
private readonly BitAiArtifactPart missingPart = new("art-deleted");

protected override void OnInitialized()
{
    var persian = conversation.GetOrAddArtifact("art-persian", BitAiArtifactKind.Document, "صورت جلسه");
    persian.SetContent("""
        ## صورت جلسه

        - انتشار اجزای هوش مصنوعی در نسخه بعدی.
        - **مسئول**: تیم رابط کاربری.
        """);
    persian.Complete();
}
"""";
}
